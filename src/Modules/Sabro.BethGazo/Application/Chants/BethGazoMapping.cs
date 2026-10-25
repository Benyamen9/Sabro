using Sabro.BethGazo.Domain;
using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>Turns sections and modes into DTOs, and requested descriptions into domain ones.</summary>
internal static class BethGazoMapping
{
    public static BethGazoModeDto ToDto(BethGazoMode mode) =>
        new(mode.Id, mode.Name, mode.Position, mode.SyriacName, DescriptionsOf(mode.Descriptions));

    /// <summary>Needs the section loaded with its allowed modes.</summary>
    public static BethGazoSectionDto ToDto(BethGazoSection section) =>
        new(
            section.Id,
            section.Name,
            section.Position,
            section.AllowedModeIds.ToList(),
            section.SyriacName,
            DescriptionsOf(section.Descriptions));

    /// <summary>
    /// Builds domain descriptions, failing on the first invalid one. The domain owns
    /// the rules (code shape, length, non-empty); repeating them here is how the two
    /// drift apart.
    /// </summary>
    public static Result<IReadOnlyList<BethGazoDescription>> ToDescriptions(
        IReadOnlyList<BethGazoDescriptionRequest>? requested)
    {
        var built = new List<BethGazoDescription>();
        foreach (var item in requested ?? [])
        {
            var result = BethGazoDescription.Create(item.Language, item.Text);
            if (!result.IsSuccess)
            {
                return Result<IReadOnlyList<BethGazoDescription>>.Failure(result.Error!);
            }

            built.Add(result.Value!);
        }

        return Result<IReadOnlyList<BethGazoDescription>>.Success(built);
    }

    private static BethGazoDescriptionDto[] DescriptionsOf(IEnumerable<BethGazoDescription> descriptions) =>
        descriptions
            .OrderBy(d => d.Language, StringComparer.Ordinal)
            .Select(d => new BethGazoDescriptionDto(d.Language, d.Text))
            .ToArray();
}
