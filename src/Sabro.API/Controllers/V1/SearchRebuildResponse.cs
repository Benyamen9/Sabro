namespace Sabro.API.Controllers.V1;

public sealed record SearchRebuildResponse(string IndexName, int DocumentCount, TimeSpan Elapsed);
