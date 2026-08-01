using Sabro.Reviews.Domain;

namespace Sabro.UnitTests.Reviews.Domain;

public class SuggestedEditTests
{
    private const string SubmittedBy = "logto|reviewer-1";
    private const string DecidedBy = "logto|owner-1";
    private const string ProposedContent = "A clearer rendering of the verse.";

    private static readonly Guid TargetId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsPendingSuggestedEdit()
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion: 1,
            ProposedContent,
            SubmittedBy);

        result.IsSuccess.Should().BeTrue();
        var edit = result.Value!;
        edit.TargetType.Should().Be(SuggestedEditTargetType.Segment);
        edit.TargetId.Should().Be(TargetId);
        edit.TargetVersion.Should().Be(1);
        edit.ProposedContent.Should().Be(ProposedContent);
        edit.SubmittedByLogtoUserId.Should().Be(SubmittedBy);
        edit.Status.Should().Be(SuggestedEditStatus.Pending);
        edit.Rationale.Should().BeNull();
        edit.DecisionByLogtoUserId.Should().BeNull();
        edit.DecisionAt.Should().BeNull();
        edit.DecisionNote.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsProposedContentAndRationale()
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion: 2,
            $"  {ProposedContent}  ",
            $"  {SubmittedBy}  ",
            rationale: "  Better lexical choice.  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProposedContent.Should().Be(ProposedContent);
        result.Value.SubmittedByLogtoUserId.Should().Be(SubmittedBy);
        result.Value.Rationale.Should().Be("Better lexical choice.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankRationale_StoresNull(string? rationale)
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion: 1,
            ProposedContent,
            SubmittedBy,
            rationale: rationale);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rationale.Should().BeNull();
    }

    [Fact]
    public void Create_WithUndefinedTargetType_ReturnsValidationError()
    {
        var result = SuggestedEdit.Create(
            (SuggestedEditTargetType)999,
            TargetId,
            targetVersion: 1,
            ProposedContent,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithEmptyTargetId_ReturnsValidationError()
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            Guid.Empty,
            targetVersion: 1,
            ProposedContent,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveTargetVersion_ReturnsValidationError(int targetVersion)
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion,
            ProposedContent,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankProposedContent_ReturnsValidationError(string? content)
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion: 1,
            content!,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankSubmittedBy_ReturnsValidationError(string? submittedBy)
    {
        var result = SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion: 1,
            ProposedContent,
            submittedBy!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Accept_OnPendingEdit_MutatesStatusAndDecisionMetadata()
    {
        var edit = NewPending();
        var originalUpdatedAt = edit.UpdatedAt;

        var error = edit.Accept(DecidedBy, note: "Looks right.");

        error.Should().BeNull();
        edit.Status.Should().Be(SuggestedEditStatus.Accepted);
        edit.DecisionByLogtoUserId.Should().Be(DecidedBy);
        edit.DecisionNote.Should().Be("Looks right.");
        edit.DecisionAt.Should().NotBeNull();
        edit.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Reject_OnPendingEdit_MutatesStatusAndDecisionMetadata()
    {
        var edit = NewPending();
        var originalUpdatedAt = edit.UpdatedAt;

        var error = edit.Reject(DecidedBy, note: "Not consistent with the Syriac.");

        error.Should().BeNull();
        edit.Status.Should().Be(SuggestedEditStatus.Rejected);
        edit.DecisionByLogtoUserId.Should().Be(DecidedBy);
        edit.DecisionNote.Should().Be("Not consistent with the Syriac.");
        edit.DecisionAt.Should().NotBeNull();
        edit.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Accept_WithBlankDecidedBy_ReturnsValidationError(string? decidedBy)
    {
        var edit = NewPending();
        var error = edit.Accept(decidedBy!);
        error.Should().NotBeNull();
        error!.Code.Should().Be("validation");
        edit.Status.Should().Be(SuggestedEditStatus.Pending);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Accept_WithBlankNote_StoresNull(string? note)
    {
        var edit = NewPending();
        var error = edit.Accept(DecidedBy, note);
        error.Should().BeNull();
        edit.DecisionNote.Should().BeNull();
    }

    [Fact]
    public void Accept_AfterAlreadyDecided_ReturnsConflictAndDoesNotMutate()
    {
        var edit = NewPending();
        edit.Accept(DecidedBy);
        var firstDecisionAt = edit.DecisionAt;

        var error = edit.Accept(DecidedBy);

        error.Should().NotBeNull();
        error!.Code.Should().Be("conflict");
        edit.Status.Should().Be(SuggestedEditStatus.Accepted);
        edit.DecisionAt.Should().Be(firstDecisionAt);
    }

    [Fact]
    public void Reject_AfterAlreadyAccepted_ReturnsConflict()
    {
        var edit = NewPending();
        edit.Accept(DecidedBy);

        var error = edit.Reject(DecidedBy);

        error.Should().NotBeNull();
        error!.Code.Should().Be("conflict");
        edit.Status.Should().Be(SuggestedEditStatus.Accepted);
    }

    [Fact]
    public void ProposeFieldChange_WithValidInputs_RecordsFieldAndTargetTimestamp()
    {
        var targetUpdatedAt = DateTimeOffset.UtcNow.AddDays(-2);

        var result = SuggestedEdit.ProposeFieldChange(
            SuggestedEditTargetType.LexiconEntry,
            TargetId,
            field: "meaning.fr",
            proposedValue: "parole",
            originalValue: "mot",
            targetUpdatedAt,
            SubmittedBy,
            rationale: "The current gloss renders the Greek, not the Syriac.");

        result.IsSuccess.Should().BeTrue();
        var proposal = result.Value!;
        proposal.Status.Should().Be(SuggestedEditStatus.Pending);
        proposal.Field.Should().Be("meaning.fr");
        proposal.ProposedContent.Should().Be("parole");
        proposal.TargetUpdatedAt.Should().Be(targetUpdatedAt);
        proposal.OriginalValue.Should().Be("mot");
        proposal.AcceptedDespiteChange.Should().BeFalse();

        // Field proposals carry no version: the Lexicon does not version entries, so
        // staleness is judged by TargetUpdatedAt instead.
        proposal.TargetVersion.Should().BeNull();
    }

    [Theory]
    [InlineData(SuggestedEditTargetType.Segment)]
    [InlineData(SuggestedEditTargetType.Annotation)]
    public void ProposeFieldChange_AgainstAProseTarget_Fails(SuggestedEditTargetType targetType)
    {
        // Prose targets replace their whole content and carry a version. Letting one
        // through here would produce a row with neither a version nor a real field.
        var result = SuggestedEdit.ProposeFieldChange(
            targetType,
            TargetId,
            field: "meaning.fr",
            proposedValue: "parole",
            originalValue: "mot",
            DateTimeOffset.UtcNow,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData(SuggestedEditTargetType.LexiconEntry)]
    [InlineData(SuggestedEditTargetType.HistoricalFigure)]
    public void Create_AgainstAFieldTarget_Fails(SuggestedEditTargetType targetType)
    {
        // The mirror image: a field target must not be filed through the prose factory,
        // which would leave Field null and nothing to apply.
        var result = SuggestedEdit.Create(
            targetType,
            TargetId,
            targetVersion: 1,
            ProposedContent,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ProposeFieldChange_WithoutAField_Fails(string field)
    {
        var result = SuggestedEdit.ProposeFieldChange(
            SuggestedEditTargetType.LexiconEntry,
            TargetId,
            field,
            proposedValue: "parole",
            originalValue: "mot",
            DateTimeOffset.UtcNow,
            SubmittedBy);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void ProposeFieldChange_ThenAccept_DoesNotAlterTheProposedValue()
    {
        // Accepting records a decision; it never rewrites what was proposed. The Owner
        // applies the change through the entry's own edit path.
        var result = SuggestedEdit.ProposeFieldChange(
            SuggestedEditTargetType.HistoricalFigure,
            TargetId,
            field: "era",
            proposedValue: "451",
            originalValue: "450",
            DateTimeOffset.UtcNow,
            SubmittedBy);

        var proposal = result.Value!;
        proposal.Accept(DecidedBy, "Agreed, Chalcedon.").Should().BeNull();

        proposal.Status.Should().Be(SuggestedEditStatus.Accepted);
        proposal.ProposedContent.Should().Be("451");
        proposal.Field.Should().Be("era");
    }

    private static SuggestedEdit NewPending() =>
        SuggestedEdit.Create(
            SuggestedEditTargetType.Segment,
            TargetId,
            targetVersion: 1,
            ProposedContent,
            SubmittedBy).Value!;
}
