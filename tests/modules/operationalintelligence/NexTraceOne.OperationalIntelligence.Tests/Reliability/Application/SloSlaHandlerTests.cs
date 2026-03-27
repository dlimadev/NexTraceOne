using FluentAssertions;
using NSubstitute;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetBurnRate;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetErrorBudget;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSlaDefinition;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSloDefinition;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;
using Xunit;

namespace NexTraceOne.OperationalIntelligence.Tests.Reliability.Application;

/// <summary>
/// Testes unitários para os handlers de SLO/SLA/ErrorBudget/BurnRate introduzidos na P6.1.
/// </summary>
public sealed class SloSlaHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ICurrentTenant MockTenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns(TenantId);
        return tenant;
    }

    // ── RegisterSloDefinition ────────────────────────────────────────────────

    [Fact]
    public async Task RegisterSloDefinition_ShouldPersistAndReturnResponse()
    {
        var repository = Substitute.For<ISloDefinitionRepository>();
        var handler = new RegisterSloDefinition.Handler(repository, MockTenant());
        var command = new RegisterSloDefinition.Command("svc-api", "production", "API Availability", SloType.Availability, 99.9m, 30);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be("svc-api");
        result.Value.Environment.Should().Be("production");
        result.Value.TargetPercent.Should().Be(99.9m);
        result.Value.WindowDays.Should().Be(30);
        await repository.Received(1).AddAsync(Arg.Any<SloDefinition>(), Arg.Any<CancellationToken>());
    }

    // ── RegisterSlaDefinition ────────────────────────────────────────────────

    [Fact]
    public async Task RegisterSlaDefinition_WhenSloExists_ShouldPersistAndReturnResponse()
    {
        var sloId = SloDefinitionId.New();
        var existingSlo = SloDefinition.Create(TenantId, "svc-api", "production", "API SLO", SloType.Availability, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(existingSlo);

        var slaRepository = Substitute.For<ISlaDefinitionRepository>();
        var handler = new RegisterSlaDefinition.Handler(sloRepository, slaRepository, MockTenant());

        var command = new RegisterSlaDefinition.Command(sloId.Value, "API SLA", 99.5m, DateTimeOffset.UtcNow);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("API SLA");
        result.Value.ContractualTargetPercent.Should().Be(99.5m);
        result.Value.Status.Should().Be(SlaStatus.Active);
        await slaRepository.Received(1).AddAsync(Arg.Any<SlaDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterSlaDefinition_WhenSloNotFound_ShouldReturnNotFound()
    {
        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((SloDefinition?)null);

        var slaRepository = Substitute.For<ISlaDefinitionRepository>();
        var handler = new RegisterSlaDefinition.Handler(sloRepository, slaRepository, MockTenant());

        var command = new RegisterSlaDefinition.Command(Guid.NewGuid(), "SLA Test", 99m, DateTimeOffset.UtcNow);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await slaRepository.DidNotReceive().AddAsync(Arg.Any<SlaDefinition>(), Arg.Any<CancellationToken>());
    }

    // ── GetErrorBudget ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetErrorBudget_WhenSnapshotExists_ShouldReturnBudgetData()
    {
        var sloId = SloDefinitionId.New();
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);
        var snapshot = ErrorBudgetSnapshot.Create(TenantId, sloId, "svc-api", "production", 1440m, 144m, DateTimeOffset.UtcNow);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var budgetRepository = Substitute.For<IErrorBudgetSnapshotRepository>();
        budgetRepository.GetLatestAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var handler = new GetErrorBudget.Handler(sloRepository, budgetRepository, MockTenant());
        var result = await handler.Handle(new GetErrorBudget.Query(sloId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalBudgetMinutes.Should().Be(1440m);
        result.Value.ConsumedBudgetMinutes.Should().Be(144m);
        result.Value.RemainingBudgetMinutes.Should().Be(1296m);
        result.Value.Status.Should().Be(SloStatus.Healthy);
    }

    [Fact]
    public async Task GetErrorBudget_WhenNoSnapshot_ShouldReturnHealthyWithNullValues()
    {
        var sloId = SloDefinitionId.New();
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Availability SLO", SloType.Availability, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var budgetRepository = Substitute.For<IErrorBudgetSnapshotRepository>();
        budgetRepository.GetLatestAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((ErrorBudgetSnapshot?)null);

        var handler = new GetErrorBudget.Handler(sloRepository, budgetRepository, MockTenant());
        var result = await handler.Handle(new GetErrorBudget.Query(sloId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalBudgetMinutes.Should().BeNull();
        result.Value.Status.Should().Be(SloStatus.Healthy);
    }

    [Fact]
    public async Task GetErrorBudget_WhenSloNotFound_ShouldReturnNotFound()
    {
        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns((SloDefinition?)null);
        var budgetRepository = Substitute.For<IErrorBudgetSnapshotRepository>();

        var handler = new GetErrorBudget.Handler(sloRepository, budgetRepository, MockTenant());
        var result = await handler.Handle(new GetErrorBudget.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    // ── GetBurnRate ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBurnRate_WhenSnapshotExists_ShouldReturnBurnRateData()
    {
        var sloId = SloDefinitionId.New();
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Error Rate SLO", SloType.ErrorRate, 99.9m, 30);
        var snapshot = BurnRateSnapshot.Create(TenantId, sloId, "svc-api", "production",
            BurnRateWindow.OneHour, 0.005m, 0.001m, DateTimeOffset.UtcNow);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var burnRateRepository = Substitute.For<IBurnRateSnapshotRepository>();
        burnRateRepository.GetLatestAsync(Arg.Any<SloDefinitionId>(), BurnRateWindow.OneHour, TenantId, Arg.Any<CancellationToken>())
            .Returns(snapshot);

        var handler = new GetBurnRate.Handler(sloRepository, burnRateRepository, MockTenant());
        var result = await handler.Handle(new GetBurnRate.Query(sloId.Value, BurnRateWindow.OneHour), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BurnRate.Should().Be(5m);
        result.Value.Window.Should().Be(BurnRateWindow.OneHour);
    }

    [Fact]
    public async Task GetBurnRate_WhenNoSnapshot_ShouldReturnHealthyWithNullValues()
    {
        var sloId = SloDefinitionId.New();
        var slo = SloDefinition.Create(TenantId, "svc-api", "production", "Error Rate SLO", SloType.ErrorRate, 99.9m, 30);

        var sloRepository = Substitute.For<ISloDefinitionRepository>();
        sloRepository.GetByIdAsync(Arg.Any<SloDefinitionId>(), TenantId, Arg.Any<CancellationToken>())
            .Returns(slo);

        var burnRateRepository = Substitute.For<IBurnRateSnapshotRepository>();
        burnRateRepository.GetLatestAsync(Arg.Any<SloDefinitionId>(), BurnRateWindow.SevenDays, TenantId, Arg.Any<CancellationToken>())
            .Returns((BurnRateSnapshot?)null);

        var handler = new GetBurnRate.Handler(sloRepository, burnRateRepository, MockTenant());
        var result = await handler.Handle(new GetBurnRate.Query(sloId.Value, BurnRateWindow.SevenDays), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BurnRate.Should().BeNull();
        result.Value.Status.Should().Be(SloStatus.Healthy);
    }
}
