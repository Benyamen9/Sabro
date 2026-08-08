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
    public const int MaxShuhlofoLength = 128;
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

    /// <summary>The opening words in Syriac, unvocalized and NFC-normalized. Required.</summary>
    public string SyriacIncipit { get; private set; } = string.Empty;

    /// <summary>Vocalized form. Optional enrichment, as in the Lexicon — it never gates publication.</summary>
    public string? SyriacIncipitVocalized { get; private set; }

    /// <summary>
    /// The melody name in SBL transliteration, e.g. "Maryam yoldath Aloho". This is
    /// what a player types, so it is required rather than optional enrichment —
    /// unlike the Lexicon, where transliteration is a search aid.
    /// </summary>
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
    /// The variation, where this chant is one. Null for a melody that has none —
    /// only some do, which is why this is nullable rather than defaulted.
    /// </summary>
    public string? Shuhlofo { get; private set; }

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

    /// <param name="section">
    /// The section entity rather than its id, because it is what knows whether
    /// <paramref name="modeId"/> is allowed. Handing the domain the id alone would
    /// push that rule up into the application layer, where it would have to be
    /// repeated for create and for update.
    /// </param>
    public static Result<Chant> Create(
        string syriacIncipit,
        string transliteration,
        BethGazoSection section,
        Guid? modeId,
        string? syriacIncipitVocalized = null,
        string? shuhlofo = null,
        Guid? inheritsMelodyFromId = null)
    {
        var normalized = Normalize(
            syriacIncipit,
            transliteration,
            section,
            modeId,
            syriacIncipitVocalized,
            shuhlofo,
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
        string? shuhlofo = null,
        Guid? inheritsMelodyFromId = null)
    {
        var normalized = Normalize(
            syriacIncipit,
            transliteration,
            section,
            modeId,
            syriacIncipitVocalized,
            shuhlofo,
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
        string? shuhlofo,
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

        string? trimmedShuhlofo = null;
        if (!string.IsNullOrWhiteSpace(shuhlofo))
        {
            trimmedShuhlofo = shuhlofo.Trim();
            if (trimmedShuhlofo.Length > MaxShuhlofoLength)
            {
                return Result<NormalizedFields>.Failure(
                    Error.Validation($"Shuhlofo must be at most {MaxShuhlofoLength} characters."));
            }
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
            trimmedShuhlofo,
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
        Shuhlofo = fields.Shuhlofo;
        InheritsMelodyFromId = fields.InheritsMelodyFromId;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private readonly record struct NormalizedFields(
        string SyriacIncipit,
        string? SyriacIncipitVocalized,
        string Transliteration,
        Guid SectionId,
        Guid? ModeId,
        string? Shuhlofo,
        Guid? InheritsMelodyFromId);
}
