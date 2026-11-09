namespace Sabro.API.Controllers.V1;

/// <summary>
/// How the hub names a place or a mark without fetching the item: a word's gloss and Syriac, a figure's
/// name, a chant's melody and mode with its incipit. Null on a grammar page, whose title lives in the hub.
/// </summary>
public sealed record ReaderItemLabelDto(string Title, string? Syriac, string? Subtitle);
