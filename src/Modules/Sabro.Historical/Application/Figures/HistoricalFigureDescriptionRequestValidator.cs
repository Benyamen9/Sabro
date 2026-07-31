using FluentValidation;
using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

/// <summary>
/// Shape checks for a single description. The domain re-checks all of this when
/// it builds the value — this exists so a bad request comes back as a field-level
/// validation problem rather than a bare domain error.
/// </summary>
public sealed class HistoricalFigureDescriptionRequestValidator
    : AbstractValidator<HistoricalFigureDescriptionRequest>
{
    public HistoricalFigureDescriptionRequestValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .Matches("^[a-zA-Z]{2,3}$")
            .WithMessage("Language must be a 2- or 3-letter ISO code.");

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(HistoricalFigureDescription.MaxTextLength);
    }
}
