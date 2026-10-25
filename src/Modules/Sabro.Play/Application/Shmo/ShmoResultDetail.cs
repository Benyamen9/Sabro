using System.Text.Json;

namespace Sabro.Play.Application.Shmo;

/// <summary>
/// Reads the guesses out of a Shmo result's <c>DetailJson</c>, which the Shmo client
/// writes as <c>{ "guessIds": [...] }</c>, oldest guess first. A win ends on the
/// answer, so the last id of a solved round is that day's figure.
/// </summary>
internal static class ShmoResultDetail
{
    /// <summary>
    /// The guessed figure ids in the order they were played. Anything malformed
    /// yields what could be read, never an exception: a damaged detail costs the
    /// collection a few cards, not the whole request.
    /// </summary>
    public static IReadOnlyList<Guid> GuessIds(string? detailJson)
    {
        if (string.IsNullOrWhiteSpace(detailJson))
        {
            return Array.Empty<Guid>();
        }

        try
        {
            using var document = JsonDocument.Parse(detailJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("guessIds", out var guessIds)
                || guessIds.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<Guid>();
            }

            var ids = new List<Guid>();
            foreach (var element in guessIds.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }
}
