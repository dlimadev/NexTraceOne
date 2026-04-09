using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para <see cref="ContractNegotiation"/>.
/// Valida criação, transições de estado, adição de comentários e validações de entrada.
/// </summary>
public sealed class ContractNegotiationTests
{
    private static readonly Guid ValidTeamId = Guid.NewGuid();
    private static readonly Guid ValidContractId = Guid.NewGuid();
    private static readonly DateTimeOffset FixedDate = new(2025, 06, 15, 10, 0, 0, TimeSpan.Zero);

    // ── Create com valores válidos ───────────────────────────────────

    [Fact]
    public void Create_Should_SetAllProperties_When_ValidValues()
    {
        var negotiation = CreateValid();

        negotiation.Id.Value.Should().NotBeEmpty();
        negotiation.ContractId.Should().Be(ValidContractId);
        negotiation.ProposedByTeamId.Should().Be(ValidTeamId);
        negotiation.ProposedByTeamName.Should().Be("Platform Team");
        negotiation.Title.Should().Be("Update Orders API v2");
        negotiation.Description.Should().Be("Proposing breaking changes to Orders API");
        negotiation.Status.Should().Be(NegotiationStatus.Draft);
        negotiation.Deadline.Should().Be(FixedDate.AddDays(30));
        negotiation.Participants.Should().Be("""["team-1","team-2"]""");
        negotiation.ParticipantCount.Should().Be(2);
        negotiation.CommentCount.Should().Be(0);
        negotiation.ProposedContractSpec.Should().Be("""{"openapi":"3.0"}""");
        negotiation.CreatedAt.Should().Be(FixedDate);
        negotiation.LastActivityAt.Should().Be(FixedDate);
        negotiation.ResolvedAt.Should().BeNull();
        negotiation.ResolvedByUserId.Should().BeNull();
        negotiation.InitiatedByUserId.Should().Be("user-123");
    }

    [Fact]
    public void Create_Should_AllowNullContractId_ForNewContracts()
    {
        var negotiation = ContractNegotiation.Create(
            contractId: null,
            proposedByTeamId: ValidTeamId,
            proposedByTeamName: "Platform Team",
            title: "New API Proposal",
            description: "Proposing a new API",
            deadline: null,
            participants: """["team-1"]""",
            participantCount: 1,
            proposedContractSpec: null,
            initiatedByUserId: "user-123",
            createdAt: FixedDate);

        negotiation.ContractId.Should().BeNull();
        negotiation.ProposedContractSpec.Should().BeNull();
        negotiation.Deadline.Should().BeNull();
    }

    // ── Transições de estado ─────────────────────────────────────────

    [Fact]
    public void SubmitForReview_Should_TransitionFromDraftToInReview()
    {
        var negotiation = CreateValid();
        var submittedAt = FixedDate.AddHours(1);

        negotiation.SubmitForReview(submittedAt);

        negotiation.Status.Should().Be(NegotiationStatus.InReview);
        negotiation.LastActivityAt.Should().Be(submittedAt);
    }

    [Fact]
    public void StartNegotiating_Should_TransitionFromInReviewToNegotiating()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        var startedAt = FixedDate.AddHours(2);

        negotiation.StartNegotiating(startedAt);

        negotiation.Status.Should().Be(NegotiationStatus.Negotiating);
        negotiation.LastActivityAt.Should().Be(startedAt);
    }

    [Fact]
    public void Approve_Should_TransitionFromInReviewToApproved()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        var resolvedAt = FixedDate.AddHours(2);

        negotiation.Approve("admin-1", resolvedAt);

        negotiation.Status.Should().Be(NegotiationStatus.Approved);
        negotiation.ResolvedByUserId.Should().Be("admin-1");
        negotiation.ResolvedAt.Should().Be(resolvedAt);
        negotiation.LastActivityAt.Should().Be(resolvedAt);
    }

    [Fact]
    public void Approve_Should_TransitionFromNegotiatingToApproved()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        negotiation.StartNegotiating(FixedDate.AddHours(2));
        var resolvedAt = FixedDate.AddHours(3);

        negotiation.Approve("admin-1", resolvedAt);

        negotiation.Status.Should().Be(NegotiationStatus.Approved);
    }

    [Fact]
    public void Reject_Should_TransitionFromInReviewToRejected()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        var resolvedAt = FixedDate.AddHours(2);

        negotiation.Reject("admin-1", resolvedAt);

        negotiation.Status.Should().Be(NegotiationStatus.Rejected);
        negotiation.ResolvedByUserId.Should().Be("admin-1");
        negotiation.ResolvedAt.Should().Be(resolvedAt);
    }

    [Fact]
    public void Reject_Should_TransitionFromNegotiatingToRejected()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        negotiation.StartNegotiating(FixedDate.AddHours(2));
        var resolvedAt = FixedDate.AddHours(3);

        negotiation.Reject("admin-1", resolvedAt);

        negotiation.Status.Should().Be(NegotiationStatus.Rejected);
    }

    // ── Transições inválidas ─────────────────────────────────────────

    [Fact]
    public void SubmitForReview_Should_Throw_When_NotInDraftStatus()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));

        var act = () => negotiation.SubmitForReview(FixedDate.AddHours(2));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void StartNegotiating_Should_Throw_When_NotInReviewStatus()
    {
        var negotiation = CreateValid();

        var act = () => negotiation.StartNegotiating(FixedDate.AddHours(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_Should_Throw_When_InDraftStatus()
    {
        var negotiation = CreateValid();

        var act = () => negotiation.Approve("admin-1", FixedDate.AddHours(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_Should_Throw_When_InDraftStatus()
    {
        var negotiation = CreateValid();

        var act = () => negotiation.Reject("admin-1", FixedDate.AddHours(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_Should_Throw_When_AlreadyApproved()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        negotiation.Approve("admin-1", FixedDate.AddHours(2));

        var act = () => negotiation.Approve("admin-2", FixedDate.AddHours(3));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_Should_Throw_When_AlreadyRejected()
    {
        var negotiation = CreateValid();
        negotiation.SubmitForReview(FixedDate.AddHours(1));
        negotiation.Reject("admin-1", FixedDate.AddHours(2));

        var act = () => negotiation.Reject("admin-2", FixedDate.AddHours(3));

        act.Should().Throw<InvalidOperationException>();
    }

    // ── AddComment ──────────────────────────────────────────────────

    [Fact]
    public void AddComment_Should_IncrementCommentCount()
    {
        var negotiation = CreateValid();

        negotiation.AddComment(FixedDate.AddMinutes(10));
        negotiation.AddComment(FixedDate.AddMinutes(20));

        negotiation.CommentCount.Should().Be(2);
    }

    [Fact]
    public void AddComment_Should_UpdateLastActivityAt()
    {
        var negotiation = CreateValid();
        var commentedAt = FixedDate.AddHours(5);

        negotiation.AddComment(commentedAt);

        negotiation.LastActivityAt.Should().Be(commentedAt);
    }

    // ── Validação de criação ─────────────────────────────────────────

    [Fact]
    public void Create_Should_Throw_When_TeamIdIsDefault()
    {
        var act = () => ContractNegotiation.Create(
            null, Guid.Empty, "Team", "Title", "Desc", null,
            """["t"]""", 1, null, "user", FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_TitleIsEmpty()
    {
        var act = () => ContractNegotiation.Create(
            null, ValidTeamId, "Team", "", "Desc", null,
            """["t"]""", 1, null, "user", FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_DescriptionIsEmpty()
    {
        var act = () => ContractNegotiation.Create(
            null, ValidTeamId, "Team", "Title", "", null,
            """["t"]""", 1, null, "user", FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_ParticipantCountIsZero()
    {
        var act = () => ContractNegotiation.Create(
            null, ValidTeamId, "Team", "Title", "Desc", null,
            """["t"]""", 0, null, "user", FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_InitiatedByUserIdIsEmpty()
    {
        var act = () => ContractNegotiation.Create(
            null, ValidTeamId, "Team", "Title", "Desc", null,
            """["t"]""", 1, null, "", FixedDate);

        act.Should().Throw<ArgumentException>();
    }

    // ── IDs únicos ──────────────────────────────────────────────────

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        var n1 = CreateValid();
        var n2 = CreateValid();

        n1.Id.Should().NotBe(n2.Id);
    }

    [Fact]
    public void ContractNegotiationId_New_Should_CreateUniqueId()
    {
        var id1 = ContractNegotiationId.New();
        var id2 = ContractNegotiationId.New();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ContractNegotiationId_From_Should_PreserveGuid()
    {
        var guid = Guid.NewGuid();
        var id = ContractNegotiationId.From(guid);

        id.Value.Should().Be(guid);
    }

    // ── Helper ──────────────────────────────────────────────────────

    private static ContractNegotiation CreateValid() =>
        ContractNegotiation.Create(
            contractId: ValidContractId,
            proposedByTeamId: ValidTeamId,
            proposedByTeamName: "Platform Team",
            title: "Update Orders API v2",
            description: "Proposing breaking changes to Orders API",
            deadline: FixedDate.AddDays(30),
            participants: """["team-1","team-2"]""",
            participantCount: 2,
            proposedContractSpec: """{"openapi":"3.0"}""",
            initiatedByUserId: "user-123",
            createdAt: FixedDate);
}
