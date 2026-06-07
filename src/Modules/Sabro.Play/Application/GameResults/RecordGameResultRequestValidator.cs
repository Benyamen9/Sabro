using FluentValidation;

namespace Sabro.Play.Application.GameResults;

public sealed class RecordGameResultRequestValidator : AbstractValidator<RecordGameResultRequest>
{
    public RecordGameResultRequestValidator()
    {
        RuleFor(x => x.GameId)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Attempts)
            .GreaterThanOrEqualTo(0);
    }
}
