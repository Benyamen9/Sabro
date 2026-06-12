using System.Threading.RateLimiting;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Sabro.API.Configuration;
using Sabro.Biblical.Public;
using Sabro.Identity.Public;
using Sabro.Lexicon.Public;
using Sabro.Play.Public;
using Sabro.Reviews.Public;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Translations.Public;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName());

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddOpenApi("v1", options =>
    {
        // Force enum schemas to be emitted as JSON strings with their member
        // names, matching the runtime JsonStringEnumConverter configured on the
        // MVC pipeline. Without this Microsoft.AspNetCore.OpenApi defaults to
        // the enum's underlying numeric type.
        options.AddSchemaTransformer<StringEnumSchemaTransformer>();
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var authority = builder.Configuration["Logto:Authority"]
                ?? throw new InvalidOperationException("Logto:Authority is not configured.");
            var audience = builder.Configuration["Logto:Audience"]
                ?? throw new InvalidOperationException("Logto:Audience is not configured.");

            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthPolicies.Read, policy => policy.RequireAssertion(c => AuthPolicies.HasScope(c, AuthPolicies.Read)));
        options.AddPolicy(AuthPolicies.Write, policy => policy.RequireAssertion(c => AuthPolicies.HasScope(c, AuthPolicies.Write)));
        options.AddPolicy(AuthPolicies.Admin, policy => policy.RequireAssertion(c => AuthPolicies.HasScope(c, AuthPolicies.Admin)));
    });

    // CORS so browser clients (the Sabro hub frontend, Meltho, future apps) on
    // other origins can call the API directly — the ecosystem's intended shape
    // (clients call /api/v1 directly). Origins come from config in production;
    // in Development we default to the local frontend ports so a fresh checkout
    // works without extra config. Bearer-token auth means we do not need
    // AllowCredentials (no cookies are sent to the API).
    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if ((corsOrigins is null || corsOrigins.Length == 0) && builder.Environment.IsDevelopment())
    {
        corsOrigins = ["http://localhost:3000", "http://localhost:3100"];
    }

    corsOrigins ??= [];

    builder.Services.AddCors(options =>
        options.AddPolicy("frontend", policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }));
    });

    builder.Services.AddHealthChecks();

    builder.Services.AddSabroSearch(builder.Configuration);

    var modules = new IModuleRegistration[]
    {
        new LexiconModule(),
        new TranslationsModule(),
        new ReviewsModule(),
        new BiblicalModule(),
        new IdentityModule(),
        new PlayModule(),
    };

    foreach (var module in modules)
    {
        module.RegisterServices(builder.Services, builder.Configuration);
    }

    builder.Services.AddValidatorsFromAssemblies(modules
        .Select(m => m.GetType().Assembly)
        .Distinct());

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Sabro API terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}
