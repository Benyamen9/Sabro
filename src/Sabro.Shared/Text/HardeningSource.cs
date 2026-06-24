namespace Sabro.Shared.Text;

/// <summary>
/// Where a begadkephat letter's hardening came from. <see cref="Marked"/> is authoritative
/// (read from an explicit qushshaya/rukkakha point in the vocalized form); <see cref="Computed"/>
/// is a tentative first-pass from the post-vocalic spirantization heuristic that the Owner can
/// override by adding the point.
/// </summary>
public enum HardeningSource
{
    /// <summary>The letter is not begadkephat, so hardening does not apply.</summary>
    None,

    /// <summary>Read from an explicit qushshaya (U+0741) or rukkakha (U+0742) mark.</summary>
    Marked,

    /// <summary>Derived by the spirantization heuristic; unverified.</summary>
    Computed,
}
