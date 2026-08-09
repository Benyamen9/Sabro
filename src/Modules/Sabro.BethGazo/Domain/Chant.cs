using Sabro.Shared.Domain;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.BethGazo.Domain;

/// <summary>
/// One melody of the Beth Gazo, and the unit Nahlo serves as a daily puzzle.
/// </summary>
/// <remarks>
/// <para>
/// <b>A chant is identified by four things, not one:</b> its melody name, its
/// section, its mode where the section has one, and — where it has one — its
/// <i>shuḥlofo</i>. A melody name recurs across modes, so "Maryam yoldath Aloho"
/// names a family rather than a chant; only "Maryam yoldath Aloho, Tlithoyo" picks
/// one out. That is the whole reason the game works: were the mode derivable from
/// the name, naming the melody would hand the player the mode for free.
/// </para>
/// <para>
/// <b>The section joined the identity on 2026-08-08</b>, when the owner gave two
/// rules that only a section can express: the madroshe have no mode at all, and the
/// mshaḥelfotho belongs to the farde alone. Both are carried by
/// <see cref="BethGazoSection.AllowedModes"/> — a section that admits no mode
/// cannot be asked one. <see cref="ModeId"/> is therefore nullable, and its null
/// means "this section has no modes", never "nobody has filled it in yet".
/// </para>
/// <para>
/// <b>On the identity columns.</b> The plan called for wrapping them in a
/// <c>ChantIdentity</c> owned value object, so that a fourth component could be
/// added in one place. That was written while the <i>solqin</i> were undefined and
/// might have been a fourth part of the answer. They are not — a solqin inherits
/// another chant's melody, which is <see cref="InheritsMelodyFromId"/>, a plain
/// link. With the identity settled at three parts the wrapper buys indirection and
/// nothing else, so these are plain columns.
/// </para>
/// </remarks>
public sealed class Chant : Entity<Guid>, IAggregateRoot
{
    public const int MaxTransliterationLength = 256;
    public const int MaxAudioUrlLength = 512;

    private Chant(NormalizedFields fields)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Status = ChantStatus.Draft;
        PlayableInNahlo = false;
        Apply(fields);
    }

    private Chant()
    {
    }

    /// <summary>
    /// <b>This chant's own</b> opening words in Syriac, unvocalized and
    /// NFC-normalized. Required.
    /// </summary>
    /// <remarks>
    /// Its own, not its group's: the chants under one melody name open with
    /// different words in each mode. It may coincide with the melody name, but
    /// nothing guarantees that for any given mode — see
    /// <see cref="Transliteration"/>.
    /// </remarks>
    public string SyriacIncipit { get; private set; } = string.Empty;

    /// <summary>Vocalized form. Optional enrichment, as in the Lexicon — it never gates publication.</summary>
    public string? SyriacIncipitVocalized { get; private set; }

    /// <summary>
    /// The melody name in SBL transliteration, e.g. "Zodeq dnehwe". This is what a
    /// player types, so it is required rather than optional enrichment — unlike the
    /// Lexicon, where transliteration is a search aid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>This is NOT the romanisation of <see cref="SyriacIncipit"/>.</b> The
    /// two are different values and the difference is the whole structure of the
    /// treasury. A melody name heads a <i>group</i> of chants that runs across the
    /// modes, and each mode's chant inside that group has its own, different opening
    /// words. The owner's example, 2026-08-08:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///   <c>Zodeq dnehwe</c> · Qadmoyo — opens <i>"zodeq dnehwe, duchrono dmaryam…"</i>
    ///   </description></item>
    ///   <item><description>
    ///   <c>Zodeq dnehwe</c> · Trayono — opens <i>"To Abrohom, badeq lan…"</i>
    ///   </description></item>
    /// </list>
    /// <para>
    /// Both carry the melody name "Zodeq dnehwe"; the first happens to begin with
    /// those words and the second does not. <b>Do not read a rule into that.</b> The
    /// owner corrected exactly this inference on 2026-08-08: a group does not
    /// necessarily take its name from its qadmoyo member, or from any member. Treat
    /// the melody name as its own datum — never derive it from an incipit, never
    /// derive an incipit from it, and never assume which mode, if any, will match.
    /// </para>
    /// <para>
    /// This is exactly what the identity index encodes — the chant is
    /// (melody, section, mode, shuḥlofo), because the melody name alone names a
    /// group rather than a chant.
    /// </para>
    /// </remarks>
    public string Transliteration { get; private set; } = string.Empty;

    /// <summary>
    /// The section of the treasury this chant belongs to. A row in
    /// <see cref="BethGazoSection"/>, and required: it decides whether the chant has
    /// a mode at all, so a chant without one has an unanswerable shape.
    /// </summary>
    public Guid SectionId { get; private set; }

    /// <summary>
    /// The mode. A row in <see cref="BethGazoMode"/>, never an enum — see that type.
    /// </summary>
    /// <remarks>
    /// <b>Null is meaningful.</b> It means the section has no modes — the madroshe —
    /// and not that the field is unfilled. The domain refuses a null here for a
    /// section that does have modes, and refuses a value for one that does not, so
    /// the two cases can never be confused by looking at the column alone.
    /// </remarks>
    public Guid? ModeId { get; private set; }

    /// <summary>
    /// What kind of extra chant this is — a <i>shuḥlofo</i>, a <i>ḥrino</i>, or neither.
    /// </summary>
    /// <remarks>
    /// See <see cref="ChantVariantKind"/> for why the two are distinguished. Together with
    /// <see cref="VariantNumber"/> this is the last part of the chant's identity.
    /// </remarks>
    public ChantVariantKind VariantKind { get; private set; } = ChantVariantKind.None;

    /// <summary>
    /// Which one it is: 1, 2, 3 … Null exactly when <see cref="VariantKind"/> is
    /// <see cref="ChantVariantKind.None"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <b>number, not a name</b> (owner, 2026-08-08). He asked for the field to be "just yes
    /// or no", which a boolean cannot deliver: he also confirmed that <b>some chants have more
    /// than one</b>, and identity is (melody, section, mode, kind, number) — so a boolean lets a
    /// chant hold exactly one and makes the second collide with it, unenterable rather than
    /// merely unnamed. An ordinal costs the same single click and keeps them apart.
    /// </para>
    /// <para>
    /// <b>No upper bound.</b> The domain accepts any number from 1 up; the backoffice offers a
    /// short list purely as a convenience. Capping it here would be the same mistake as assuming
    /// eight modes.
    /// </para>
    /// <para>
    /// The game never asks <i>which</i> one — only whether the chant is a shuḥlofo at all
    /// (owner, 2026-08-07) — so this travels outward alongside the kind, and a ḥrino answers
    /// "no" to that question exactly as a principal chant does. The reveal can still say
    /// "variation 2", which is what the earlier name field was for.
    /// </para>
    /// </remarks>
    public int? VariantNumber { get; private set; }

    /// <summary>
    /// Set when this chant is a <i>solqin</i>: it inherits the melody of the chant
    /// named here, with variations of its own. Null when the chant carries its own
    /// melody.
    /// </summary>
    /// <remarks>
    /// Deliberately outside the identity key. A solqin is a relationship between
    /// two chants, not a fourth thing the player names — and the melody a listener
    /// actually recognises in a solqin is its parent's, which is why the launch
    /// pool leaves solqin unflagged (see <see cref="SetPlayable"/>).
    /// </remarks>
    public Guid? InheritsMelodyFromId { get; private set; }

    /// <summary>
    /// The recording. Null while drafting; required to publish, since a chant with
    /// no audio is not a puzzle. Served from the same media volume as the Lexicon's
    /// pronunciation clips.
    /// </summary>
    public string? AudioUrl { get; private set; }

    public ChantStatus Status { get; private set; }

    /// <summary>Editorial opt-in to the Nahlo rotation. Only a published chant may carry it.</summary>
    public bool PlayableInNahlo { get; private set; }

    /// <summary>
    /// Creates a draft chant.
    /// </summary>
    /// <remarks>
    /// Takes the section entity rather than its id, because the section is what
    /// knows whether the mode is allowed. Handing the domain the id alone would push
    /// that rule up into the application layer, where it would have to be repeated
    /// for create and for update.
    /// </remarks>
    public static Result<Chant> Create(
        string syriacIncipit,
        string transliteration,
        BethGazoSection section,
        Guid? modeId,
        string? syriacIncipitVocalized = null,
        ChantVariantKind variantKind = ChantVariantKind.None,
        int? variantNumber = null,
        Guid? inheritsMelodyFromId = null)
    {
        var normalized = Normalize(
            syriacIncipit,
            transliteration,
            section,
            modeId,
            syriacIncipitVocalized,
            variantKind,
            variantNumber,
            inheritsMelodyFromId);
        if (!normalized.IsSuccess)
        {
            return Result<Chant>.Failure(normalized.Error!);
        }

        return Result<Chant>.Success(new Chant(normalized.Value!));
    }

    /// <summary>
    /// Replaces the editable fields. Leaves <see cref="Status"/>,
    /// <see cref="PlayableInNahlo"/> and <see cref="AudioUrl"/> alone — the
    /// recording has its own operation, because replacing it is an upload rather
    /// than an edit.
    /// </summary>
    public Error? Update(
        string syriacIncipit,
        string transliteration,
        BethGazoSection section,
        Guid? modeId,
        string? syriacIncipitVocalized = null,
        ChantVariantKind variantKind = ChantVariantKind.None,
        int? variantNumber = null,
        Guid? inheritsMelodyFromId = null)
    {
        var normalized = Normalize(
            syriacIncipit,
            transliteration,
            section,
            modeId,
            syriacIncipitVocalized,
            variantKind,
            variantNumber,
            inheritsMelodyFromId);
        if (!normalized.IsSuccess)
        {
            return normalized.Error;
        }

        if (normalized.Value!.InheritsMelodyFromId == Id)
        {
            return Error.Validation("A chant cannot inherit its own melody.");
        }

        Apply(normalized.Value!);
        Touch();
        return null;
    }

    /// <summary>Attaches or replaces the recording. Passing null clears it, which unpublishes nothing —
    /// a published chant must keep its audio, so clearing one is refused.</summary>
    public Error? SetAudioUrl(string? audioUrl)
    {
        var trimmed = string.IsNullOrWhiteSpace(audioUrl) ? null : audioUrl.Trim();

        if (trimmed is not null && trimmed.Length > MaxAudioUrlLength)
        {
            return Error.Validation($"Audio URL must be at most {MaxAudioUrlLength} characters.");
        }

        if (trimmed is null && Status == ChantStatus.Published)
        {
            return Error.Validation(
                "A published chant must keep its recording. Return it to draft before removing the audio.");
        }

        AudioUrl = trimmed;
        Touch();
        return null;
    }

    /// <summary>
    /// Promotes a draft to published. Requires a recording — the publish gate for a
    /// chant, as a gloss in every language is the Lexicon's. Idempotent when
    /// already published.
    /// </summary>
    public Error? Publish()
    {
        if (Status == ChantStatus.Published)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(AudioUrl))
        {
            return Error.Validation("A recording is required to publish a chant — without audio there is no puzzle.");
        }

        Status = ChantStatus.Published;
        Touch();
        return null;
    }

    /// <summary>Returns the chant to draft and clears the playable flag (a draft can never be playable).</summary>
    public void ReturnToDraft()
    {
        if (Status == ChantStatus.Draft && !PlayableInNahlo)
        {
            return;
        }

        Status = ChantStatus.Draft;
        PlayableInNahlo = false;
        Touch();
    }

    /// <summary>
    /// Sets the editorial playable flag. Marking playable requires the chant to be
    /// published.
    /// </summary>
    /// <remarks>
    /// A solqin is <i>allowed</i> to be flagged — the domain does not forbid it —
    /// but the launch pool deliberately leaves them unflagged. What a listener
    /// recognises in a solqin is the parent's melody, so "correct" is ambiguous
    /// until the owner decides whether the parent should score as a near-miss.
    /// That is an editorial call, which is exactly what this flag is for.
    /// </remarks>
    public Error? SetPlayable(bool playable)
    {
        if (playable && Status != ChantStatus.Published)
        {
            return Error.Conflict("Only published chants can be marked playable.");
        }

        if (PlayableInNahlo == playable)
        {
            return null;
        }

        PlayableInNahlo = playable;
        Touch();
        return null;
    }

    private static Result<NormalizedFields> Normalize(
        string syriacIncipit,
        string transliteration,
        BethGazoSection section,
        Guid? modeId,
        string? syriacIncipitVocalized,
        ChantVariantKind variantKind,
        int? variantNumber,
        Guid? inheritsMelodyFromId)
    {
        if (section is null)
        {
            return Result<NormalizedFields>.Failure(Error.Validation("A section is required."));
        }

        var incipit = NormalizeSyriacRequired(syriacIncipit, "SyriacIncipit");
        if (!incipit.IsSuccess)
        {
            return Result<NormalizedFields>.Failure(incipit.Error!);
        }

        string? vocalized = null;
        if (!string.IsNullOrWhiteSpace(syriacIncipitVocalized))
        {
            var vocalizedResult = NormalizeSyriacRequired(syriacIncipitVocalized, "SyriacIncipitVocalized");
            if (!vocalizedResult.IsSuccess)
            {
                return Result<NormalizedFields>.Failure(vocalizedResult.Error!);
            }

            vocalized = vocalizedResult.Value;
        }

        var trimmedTransliteration = (transliteration ?? string.Empty).Trim();
        if (trimmedTransliteration.Length == 0)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation("Transliteration is required — it is the name a player types."));
        }

        if (trimmedTransliteration.Length > MaxTransliterationLength)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation($"Transliteration must be at most {MaxTransliterationLength} characters."));
        }

        // The section owns this rule, both directions of it: a mode is required
        // where the section has modes, and refused where it has none. Asking the
        // section rather than testing for Guid.Empty is what keeps "the madroshe
        // have no mode" from becoming a special case anywhere else.
        var normalizedModeId = modeId == Guid.Empty ? null : modeId;
        var modeError = section.ValidateMode(normalizedModeId);
        if (modeError is not null)
        {
            return Result<NormalizedFields>.Failure(modeError);
        }

        // The kind and the number stand or fall together: an "extra chant" with no
        // number cannot be told from its siblings, and a number with no kind does not
        // say what it is one of. Both directions are refused so the pair can never be
        // half-filled.
        if (variantKind == ChantVariantKind.None && variantNumber is not null)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation("A chant that is neither a shuḥlofo nor a ḥrino carries no number."));
        }

        if (variantKind != ChantVariantKind.None && variantNumber is null)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation("A shuḥlofo or ḥrino needs a number, so it can be told from the others."));
        }

        if (variantNumber is not null && variantNumber < 1)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation("The number must be 1 or greater."));
        }

        if (inheritsMelodyFromId == Guid.Empty)
        {
            return Result<NormalizedFields>.Failure(
                Error.Validation("InheritsMelodyFromId must be a real chant id, or null."));
        }

        return Result<NormalizedFields>.Success(new NormalizedFields(
            incipit.Value!,
            vocalized,
            trimmedTransliteration,
            section.Id,
            normalizedModeId,
            variantKind,
            variantNumber,
            inheritsMelodyFromId));
    }

    private static Result<string> NormalizeSyriacRequired(string value, string fieldName)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(Error.Validation($"{fieldName} is required."));
        }

        var normalized = SyriacText.Normalize(trimmed);
        if (!SyriacText.IsSyriacOnly(normalized))
        {
            return Result<string>.Failure(
                Error.Validation($"{fieldName} must contain Syriac characters only."));
        }

        return Result<string>.Success(normalized);
    }

    private void Apply(NormalizedFields fields)
    {
        SyriacIncipit = fields.SyriacIncipit;
        SyriacIncipitVocalized = fields.SyriacIncipitVocalized;
        Transliteration = fields.Transliteration;
        SectionId = fields.SectionId;
        ModeId = fields.ModeId;
        VariantKind = fields.VariantKind;
        VariantNumber = fields.VariantNumber;
        InheritsMelodyFromId = fields.InheritsMelodyFromId;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private readonly record struct NormalizedFields(
        string SyriacIncipit,
        string? SyriacIncipitVocalized,
        string Transliteration,
        Guid SectionId,
        Guid? ModeId,
        ChantVariantKind VariantKind,
        int? VariantNumber,
        Guid? InheritsMelodyFromId);
}
