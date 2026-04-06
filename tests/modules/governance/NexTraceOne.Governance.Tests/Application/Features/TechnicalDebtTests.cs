using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.RecordTechnicalDebt;
using NexTraceOne.Governance.Application.Features.GetTechnicalDebtSummary;

namespace NexTraceOne.Governance.Tests.Application.Features;

/// <summary>
/// Testes unitários para as features de Technical Debt tracking.
/// </summary>
public sealed class TechnicalDebtTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public TechnicalDebtTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ── RecordTechnicalDebt ───────────────────────────────────────────────

    [Fact]
    public async Task RecordTechnicalDebt_ValidCommand_ReturnsValidResponseWithComputedScore()
    {
        var handler = new RecordTechnicalDebt.Handler(_clock);
        var command = new RecordTechnicalDebt.Command(
            ServiceName: "order-service",
            DebtType: "architecture",
            Title: "Monolith needs decomposition",
            Description: "The service has grown too large and needs bounded context separation.",
            Severity: "high",
            EstimatedEffortDays: 10,
            Tags: "architecture,decomposition");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DebtId.Should().NotBe(Guid.Empty);
        result.Value.ServiceName.Should().Be("order-service");
        result.Value.DebtType.Should().Be("architecture");
        result.Value.Severity.Should().Be("high");
        result.Value.EstimatedEffortDays.Should().Be(10);
        result.Value.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task RecordTechnicalDebt_CriticalSeverity_HasDebtScoreOf40()
    {
        var handler = new RecordTechnicalDebt.Handler(_clock);
        var command = new RecordTechnicalDebt.Command(
            ServiceName: "auth-service",
            DebtType: "security",
            Title: "Critical vulnerability",
            Description: "Unpatched CVE in authentication library.",
            Severity: "critical",
            EstimatedEffortDays: 2,
            Tags: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DebtScore.Should().Be(40);
    }

    [Fact]
    public void RecordTechnicalDebt_ValidatorRejectsEmptyServiceName()
    {
        var validator = new RecordTechnicalDebt.Validator();
        var command = new RecordTechnicalDebt.Command(
            ServiceName: "",
            DebtType: "architecture",
            Title: "Some title",
            Description: "Some description",
            Severity: "medium",
            EstimatedEffortDays: 5,
            Tags: null);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceName");
    }

    [Fact]
    public void RecordTechnicalDebt_ValidatorRejectsEffortDaysOver999()
    {
        var validator = new RecordTechnicalDebt.Validator();
        var command = new RecordTechnicalDebt.Command(
            ServiceName: "svc",
            DebtType: "testing",
            Title: "Title",
            Description: "Description",
            Severity: "low",
            EstimatedEffortDays: 1000,
            Tags: null);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstimatedEffortDays");
    }

    [Fact]
    public void RecordTechnicalDebt_ValidatorRejectsInvalidSeverity()
    {
        var validator = new RecordTechnicalDebt.Validator();
        var command = new RecordTechnicalDebt.Command(
            ServiceName: "svc",
            DebtType: "architecture",
            Title: "Title",
            Description: "Description",
            Severity: "extreme",
            EstimatedEffortDays: 5,
            Tags: null);

        var result = validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Severity");
    }

    // ── GetTechnicalDebtSummary ───────────────────────────────────────────

    [Fact]
    public async Task GetTechnicalDebtSummary_DefaultQuery_ReturnsDemoDataWithPositiveTotalScore()
    {
        var handler = new GetTechnicalDebtSummary.Handler();
        var query = new GetTechnicalDebtSummary.Query();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalDebtScore.Should().BeGreaterThan(0);
        result.Value.DebtItems.Count.Should().BeGreaterThan(0);
        result.Value.ByType.Count.Should().BeGreaterThan(0);
        result.Value.HighestRiskService.Should().NotBeNullOrEmpty();
        result.Value.RecommendedAction.Should().NotBeNullOrEmpty();
    }
}
