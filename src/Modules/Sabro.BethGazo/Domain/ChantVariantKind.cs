namespace Sabro.BethGazo.Domain;

/// <summary>
/// What kind of extra chant this is, where a mode carries more than one.
/// </summary>
/// <remarks>
/// <para>
/// Owner, 2026-08-08: <i>"not all the extra chants of a mode are shuḥlofe, but hrone
/// as well."</i> The two are different things and the schema has to tell them apart,
/// because the chant's identity is
/// (melody, section, mode, variant) — a <i>shuḥlofo 1</i> and a <i>ḥrino 1</i> under
/// one melody and mode would otherwise be the same four values and collide, making
/// the second unsaveable.
/// </para>
/// <para>
/// The difference is real rather than clerical. A <b>shuḥlofo</b> is a variation
/// <i>of the melody itself</i> — the same qolo sung another way, and the sources note
/// these are never written down but passed from <i>malphono</i> to <i>talmidho</i>. A
/// <b>ḥrino</b> (ܐܚܪܢܐ, "another"; pl. <i>ḥrone</i>) is simply <i>another chant</i>
/// standing in the same mode: not a variant of anything, just the next one.
/// </para>
/// <para>
/// A string-converted enum, per the house rule: adding a value later is an ordinary
/// migration, renaming one is a breaking <c>/api/v1/</c> change. Unlike the modes and
/// the sections this is <b>not</b> a reference table — these are two structural
/// markers the book prints, not a set that grows as the owner works through it.
/// </para>
/// </remarks>
public enum ChantVariantKind
{
    /// <summary>
    /// The chant in its own right — the principal entry for this melody in this mode,
    /// neither a variation of it nor an "another".
    /// </summary>
    None = 0,

    /// <summary>A variation of the melody: the same qolo sung another way.</summary>
    Shuhlofo = 1,

    /// <summary>Another chant standing in the same mode, not a variation of the melody.</summary>
    Hrino = 2,
}
