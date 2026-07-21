using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;
using Sabro.Reviews.Application.SuggestedEdits;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Localization;

namespace Sabro.IntegrationTests.Reviews.Application;

[Collection(IntegrationCollection.Name)]
public class SuggestedEditServiceTests
{
    private readonly PostgresFixture postgres;

    public SuggestedEditServiceTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    [Fact]
    public async Task Propose_AsExpertReviewer_PersistsPendingEdit()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.ProposeAsync(
            new CreateSuggestedEditRequest(
                SuggestedEditTargetType.Segment,
                Guid.NewGuid(),
                TargetVersion: 1,
                ProposedContent: "A clearer rendering.",
                Rationale: "Better lexical choice."),
            reviewer,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Pending);
        result.Value.SubmittedByLogtoUserId.Should().Be(reviewer);
        result.Value.Rationale.Should().Be("Better lexical choice.");

        await using var verify = postgres.CreateReviewsContext();
        (await verify.SuggestedEdits.CountAsync(e => e.Id == result.Value.Id, ct)).Should().Be(1);
    }

    [Fact]
    public async Task Propose_AsReader_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.ProposeAsync(
            ValidProposal(),
            reader,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Propose_AsOwner_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.ProposeAsync(
            ValidProposal(),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Propose_WithBlankContent_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.ProposeAsync(
            new CreateSuggestedEditRequest(
                SuggestedEditTargetType.Segment,
                Guid.NewGuid(),
                TargetVersion: 1,
                ProposedContent: "   "),
            reviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task GetById_OnMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.GetByIdAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task List_FiltersByTargetIdAndStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var targetId = Guid.NewGuid();
        var otherTargetId = Guid.NewGuid();

        await using (var seed = postgres.CreateReviewsContext())
        {
            var service = NewService(seed);
            await service.ProposeAsync(ValidProposal(targetId), reviewer, ct);
            await service.ProposeAsync(ValidProposal(targetId), reviewer, ct);
            await service.ProposeAsync(ValidProposal(otherTargetId), reviewer, ct);
        }

        await using var ctx = postgres.CreateReviewsContext();
        var queryService = NewService(ctx);

        var result = await queryService.ListAsync(
            new SuggestedEditListFilters(
                Status: SuggestedEditStatus.Pending,
                TargetId: targetId),
            page: 1,
            pageSize: 20,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().OnlyContain(e => e.TargetId == targetId && e.Status == SuggestedEditStatus.Pending);
    }

    [Fact]
    public async Task Accept_AsOwner_TransitionsToAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var seedCtx = postgres.CreateReviewsContext();
        var seedService = NewService(seedCtx);
        var proposed = await seedService.ProposeAsync(ValidProposal(), reviewer, ct);
        proposed.IsSuccess.Should().BeTrue();

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest("Looks right."),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Accepted);
        result.Value.DecisionByLogtoUserId.Should().Be(owner);
        result.Value.DecisionNote.Should().Be("Looks right.");
        result.Value.DecisionAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Accept_AsExpertReviewer_ReturnsForbidden()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        await using var seedCtx = postgres.CreateReviewsContext();
        var seedService = NewService(seedCtx);
        var proposed = await seedService.ProposeAsync(ValidProposal(), reviewer, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.AcceptAsync(
            proposed.Value!.Id,
            new DecisionRequest(),
            reviewer,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Accept_AlreadyAccepted_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var seedCtx = postgres.CreateReviewsContext();
        var seedService = NewService(seedCtx);
        var proposed = await seedService.ProposeAsync(ValidProposal(), reviewer, ct);
        await seedService.AcceptAsync(proposed.Value!.Id, new DecisionRequest(), owner, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.AcceptAsync(
            proposed.Value.Id,
            new DecisionRequest(),
            owner,
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Reject_AsOwner_TransitionsToRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var owner = await SeedProfileAsync(Role.Owner, ct);

        await using var seedCtx = postgres.CreateReviewsContext();
        var seedService = NewService(seedCtx);
        var proposed = await seedService.ProposeAsync(ValidProposal(), reviewer, ct);

        await using var ctx = postgres.CreateReviewsContext();
        var service = NewService(ctx);

        var result = await service.RejectAsync(
            proposed.Value!.Id,
            new DecisionRequest("Inconsistent with the Syriac."),
            owner,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(SuggestedEditStatus.Rejected);
        result.Value.DecisionNote.Should().Be("Inconsistent with the Syriac.");
    }

    private static CreateSuggestedEditRequest ValidProposal(Guid? targetId = null) =>
        new(
            SuggestedEditTargetType.Segment,
            targetId ?? Guid.NewGuid(),
            TargetVersion: 1,
            ProposedContent: "Proposed revised content.");

    private static SuggestedEditService NewService(ReviewsDbContext ctx) =>
        new(
            ctx,
            new CreateSuggestedEditRequestValidator(),
            new UserProfileService(
                NewIdentityContext(ctx),
                new UpdateUserProfileRequestValidator(Options.Create(new SupportedLanguagesOptions())),
                NullLogger<UserProfileService>.Instance),
            NullLogger<SuggestedEditService>.Instance);

    private static IdentityDbContext NewIdentityContext(ReviewsDbContext reviewsContext)
    {
        var connectionString = reviewsContext.Database.GetConnectionString()!;
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new IdentityDbContext(options);
    }

    private async Task<string> SeedProfileAsync(Role role, CancellationToken ct)
    {
        var logtoUserId = $"logto|{Guid.NewGuid():N}";
        await using var ctx = postgres.CreateIdentityContext();
        var profile = UserProfile.Create(logtoUserId).Value!;
        profile.AssignRole(role);
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync(ct);
        return logtoUserId;
    }
}
