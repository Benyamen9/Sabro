using FluentValidation;

namespace Sabro.Historical.Application.Figures;

public sealed class CreateHistoricalFigureRequestValidator : AbstractValidator<CreateHistoricalFigureRequest>
{
    /// <summary>
    /// Deliberately generous: the ecosystem supports five languages today, and this
    /// is a sanity bound on request size, not a statement about which languages are
    /// allowed. The configured set lives in SupportedLanguagesOptions and must not
    /// be duplicated here.
    /// </summary>
    private const int MaxDescriptions = 20;

    public CreateHistoricalFigureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Region).IsInEnum();
        RuleFor(x => x.Gender).IsInEnum();

        RuleFor(x => x.Tradition!.Value)
            .IsInEnum()
            .When(x => x.Tradition.HasValue);

        RuleFor(x => x.Era)
            .NotEqual(0)
            .WithMessage("Era must not be zero — there is no century zero.");

        // Bounded so a caller cannot post an unbounded collection, and deduplicated
        // here as well as in the domain so the failure comes back as a field error
        // on Descriptions rather than as a bare domain message.
        RuleFor(x => x.Descriptions)
            .Must(d => d is null || d.Count <= MaxDescriptions)
            .WithMessage($"At most {MaxDescriptions} descriptions are allowed (one per supported language).")
            .Must(HaveDistinctLanguages)
            .WithMessage("Only one description per language is allowed.");

        RuleForEach(x => x.Descriptions).SetValidator(new HistoricalFigureDescriptionRequestValidator());
    }

    private static bool HaveDistinctLanguages(IReadOnlyList<HistoricalFigureDescriptionRequest>? descriptions)
    {
        if (descriptions is null)
        {
            return true;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return descriptions.All(d => d.Language is null || seen.Add(d.Language.Trim()));
    }
}
