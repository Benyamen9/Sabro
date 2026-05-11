namespace Sabro.Identity.Domain;

/// <summary>
/// The three traditional Syriac scripts. The underlying Unicode content is
/// identical across variants — the choice only affects rendering. Defaults
/// to Estrangela, which is the convention for patristic texts.
/// </summary>
public enum ScriptVariant
{
    Estrangela,
    Serto,
    Madnhaya,
}
