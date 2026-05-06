using FluentValidation.Results;

namespace Sabro.Lexicon.Application;

internal static class ValidationErrorMap
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FromFluentValidation(
        IEnumerable<ValidationFailure> failures) =>
        failures
            .GroupBy(e => CamelCase(e.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(e => e.ErrorMessage).ToArray());

    private static string CamelCase(string propertyName) =>
        propertyName.Length == 0 ? propertyName : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
