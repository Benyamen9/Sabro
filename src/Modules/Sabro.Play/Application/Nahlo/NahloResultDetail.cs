using System.Text.Json;

namespace Sabro.Play.Application.Nahlo;

/// <summary>
/// Reads the guesses out of a Nahlo result's <c>DetailJson</c>, which the Nahlo
/// client writes as <c>{ "guesses": [{ "melody", "mode", "isShuhlofo" }], ... }</c>,
/// oldest guess first. A win ends on the right answer, so the last guess of a
/// solved round is the day's chant.
/// </summary>
internal static class NahloResultDetail
{
    /// <summary>
    /// The guesses in the order they were played. Anything malformed yields what
    /// could be read, never an exception; an entry without a melody is dropped.
    /// </summary>
    public static IReadOnlyList<NahloGuess> Guesses(string? detailJson)
    {
        if (string.IsNullOrWhiteSpace(detailJson))
        {
            return Array.Empty<NahloGuess>();
        }

        try
        {
            using var document = JsonDocument.Parse(detailJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("guesses", out var guesses)
                || guesses.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<NahloGuess>();
            }

            var read = new List<NahloGuess>();
            foreach (var element in guesses.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object
                    || !element.TryGetProperty("melody", out var melody)
                    || melody.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var mode = element.TryGetProperty("mode", out var m) && m.ValueKind == JsonValueKind.String
                    ? m.GetString()
                    : null;
                var isShuhlofo = element.TryGetProperty("isShuhlofo", out var s) && s.ValueKind == JsonValueKind.True;
                read.Add(new NahloGuess(melody.GetString()!, mode, isShuhlofo));
            }

            return read;
        }
        catch (JsonException)
        {
            return Array.Empty<NahloGuess>();
        }
    }
}
