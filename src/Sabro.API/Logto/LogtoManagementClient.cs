using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sabro.Shared.Results;

namespace Sabro.API.Logto;

/// <summary>
/// Talks to the self-hosted Logto Management API. Obtains a machine-to-machine
/// access token via <c>client_credentials</c> (client_secret_basic) against the
/// management resource, then deletes the user. Endpoints are derived from the
/// existing <c>Logto:Authority</c> (e.g. <c>https://auth.example/oidc</c>): the
/// token endpoint is <c>{authority}/token</c> and the management base is the
/// authority with <c>/oidc</c> stripped plus <c>/api</c>.
/// </summary>
internal sealed class LogtoManagementClient : ILogtoManagementClient
{
    private static readonly Error UnconfiguredError = new(
        "logto_unconfigured",
        "Account deletion is unavailable because the Logto Management API is not configured.");

    private static readonly Error CallFailedError = new(
        "logto_error",
        "Could not delete the account from the identity provider. Please try again.");

    private readonly HttpClient httpClient;
    private readonly LogtoManagementOptions options;
    private readonly string tokenEndpoint;
    private readonly string managementBaseUrl;
    private readonly ILogger<LogtoManagementClient> logger;

    public LogtoManagementClient(
        HttpClient httpClient,
        IOptions<LogtoManagementOptions> options,
        IConfiguration configuration,
        ILogger<LogtoManagementClient> logger)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.logger = logger;

        var authority = (configuration["Logto:Authority"] ?? string.Empty).TrimEnd('/');
        tokenEndpoint = $"{authority}/token";

        var endpoint = authority.EndsWith("/oidc", StringComparison.OrdinalIgnoreCase)
            ? authority[..^"/oidc".Length]
            : authority;
        managementBaseUrl = $"{endpoint}/api";
    }

    public async Task<Result<bool>> DeleteUserAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            logger.LogError("Logto Management API is not configured; cannot delete identity for the account-deletion request.");
            return Result<bool>.Failure(UnconfiguredError);
        }

        try
        {
            var accessToken = await GetManagementTokenAsync(cancellationToken);
            if (accessToken is null)
            {
                return Result<bool>.Failure(CallFailedError);
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"{managementBaseUrl}/users/{Uri.EscapeDataString(logtoUserId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            // 204 = deleted; 404 = already gone — both leave the identity erased.
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Logto identity deleted via Management API. Status={StatusCode}", (int)response.StatusCode);
                return Result<bool>.Success(true);
            }

            logger.LogError("Logto user delete failed. Status={StatusCode}", (int)response.StatusCode);
            return Result<bool>.Failure(CallFailedError);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Logto Management API request failed.");
            return Result<bool>.Failure(CallFailedError);
        }
    }

    private async Task<string?> GetManagementTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["resource"] = options.Resource,
            ["scope"] = "all",
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Logto management token request failed. Status={StatusCode}", (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.TryGetProperty("access_token", out var token)
            ? token.GetString()
            : null;
    }
}
