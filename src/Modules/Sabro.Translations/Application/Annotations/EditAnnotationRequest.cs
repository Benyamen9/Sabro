namespace Sabro.Translations.Application.Annotations;

public sealed record EditAnnotationRequest(
    Guid AnnotationId,
    string NewBody);
