namespace Sabro.Biblical.Application.Passages;

/// <summary>
/// Returned by <c>IBiblicalPassageService.GetOrCreateAsync</c>. <see cref="WasCreated"/>
/// lets the controller decide whether to return 200 (existing) or 201 (newly created).
/// </summary>
public sealed record BiblicalPassageLookupResult(BiblicalPassageDto Passage, bool WasCreated);
