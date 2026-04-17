using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.AssessServiceMaturity;
using NexTraceOne.Governance.Application.Features.GetServiceMaturity;
using NexTraceOne.Governance.Application.Features.ListServicesByMaturityLevel;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Domain.Errors;

namespace NexTraceOne.Governance.Tests.Application;

/// <summary>
/// Testes dos handlers de maturidade de serviços.
/// Cobre AssessServiceMaturity, GetServiceMaturity e ListServicesByMaturityLevel.
/// </summary>
public sealed class ServiceMaturityHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IServiceMaturityAssessmentRepository _repository =
        Substitute.For<IServiceMaturityAssessmentRepository>();
    private readonly IGovernanceUnitOfWork _unitOfWork = Substitute.For<IGovernanceUnitOfWork>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public ServiceMaturityHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
    }

    // ── AssessServiceMaturity ──

    [Fact]
    public async Task Assess_NewService_ShouldCreateAssessment()
    {
        _repository.GetByServiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(null));

        var handler = new AssessServiceMaturity.Handler(_repository, _unitOfWork, _clock);
        var command = new AssessServiceMaturity.Command(
            ServiceId: Guid.NewGuid(),
            ServiceName: "order-service",
            OwnershipDefined: true,
            ContractsPublished: true,
            DocumentationExists: true,
            PoliciesApplied: false,
            ApprovalWorkflowActive: false,
            TelemetryActive: false,
            BaselinesEstablished: false,
            AlertsConfigured: false,
            RunbooksAvailable: false,
            RollbackTested: false,
            ChaosValidated: false,
            AssessedBy: "auto",
            TenantId: "tenant1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReassessment.Should().BeFalse();
        result.Value.CurrentLevel.Should().Be(ServiceMaturityLevel.Documented);
        result.Value.ServiceName.Should().Be("order-service");

        await _repository.Received(1).AddAsync(Arg.Any<ServiceMaturityAssessment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assess_ExistingService_ShouldReassess()
    {
        var serviceId = Guid.NewGuid();
        var existing = ServiceMaturityAssessment.Assess(
            serviceId: serviceId,
            serviceName: "order-service",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: "tenant1",
            now: FixedNow.AddDays(-30));

        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(existing));

        var handler = new AssessServiceMaturity.Handler(_repository, _unitOfWork, _clock);
        var command = new AssessServiceMaturity.Command(
            ServiceId: serviceId,
            ServiceName: "order-service",
            OwnershipDefined: true,
            ContractsPublished: true,
            DocumentationExists: true,
            PoliciesApplied: true,
            ApprovalWorkflowActive: true,
            TelemetryActive: false,
            BaselinesEstablished: false,
            AlertsConfigured: false,
            RunbooksAvailable: false,
            RollbackTested: false,
            ChaosValidated: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsReassessment.Should().BeTrue();
        result.Value.CurrentLevel.Should().Be(ServiceMaturityLevel.Governed);
        result.Value.ReassessmentCount.Should().Be(1);

        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Assess_AllCriteriaTrue_ShouldReturnResilient()
    {
        _repository.GetByServiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(null));

        var handler = new AssessServiceMaturity.Handler(_repository, _unitOfWork, _clock);
        var command = new AssessServiceMaturity.Command(
            ServiceId: Guid.NewGuid(),
            ServiceName: "mature-svc",
            OwnershipDefined: true,
            ContractsPublished: true,
            DocumentationExists: true,
            PoliciesApplied: true,
            ApprovalWorkflowActive: true,
            TelemetryActive: true,
            BaselinesEstablished: true,
            AlertsConfigured: true,
            RunbooksAvailable: true,
            RollbackTested: true,
            ChaosValidated: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentLevel.Should().Be(ServiceMaturityLevel.Resilient);
    }

    [Fact]
    public async Task Assess_NewWithNoCriteria_ShouldReturnBasic()
    {
        _repository.GetByServiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(null));

        var handler = new AssessServiceMaturity.Handler(_repository, _unitOfWork, _clock);
        var command = new AssessServiceMaturity.Command(
            ServiceId: Guid.NewGuid(),
            ServiceName: "bare-svc",
            OwnershipDefined: false,
            ContractsPublished: false,
            DocumentationExists: false,
            PoliciesApplied: false,
            ApprovalWorkflowActive: false,
            TelemetryActive: false,
            BaselinesEstablished: false,
            AlertsConfigured: false,
            RunbooksAvailable: false,
            RollbackTested: false,
            ChaosValidated: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentLevel.Should().Be(ServiceMaturityLevel.Basic);
    }

    [Fact]
    public async Task Assess_ReassessMultipleTimes_ShouldIncrementCount()
    {
        var serviceId = Guid.NewGuid();
        var existing = ServiceMaturityAssessment.Assess(
            serviceId: serviceId,
            serviceName: "svc",
            ownershipDefined: true,
            contractsPublished: false,
            documentationExists: false,
            policiesApplied: false,
            approvalWorkflowActive: false,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow.AddDays(-60));

        // Pre-reassess once
        existing.Reassess(true, true, false, false, false, false, false, false, false, false, false,
            FixedNow.AddDays(-30));

        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(existing));

        var handler = new AssessServiceMaturity.Handler(_repository, _unitOfWork, _clock);
        var command = new AssessServiceMaturity.Command(
            ServiceId: serviceId, ServiceName: "svc",
            OwnershipDefined: true, ContractsPublished: true, DocumentationExists: true,
            PoliciesApplied: false, ApprovalWorkflowActive: false,
            TelemetryActive: false, BaselinesEstablished: false, AlertsConfigured: false,
            RunbooksAvailable: false, RollbackTested: false, ChaosValidated: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReassessmentCount.Should().Be(2);
    }

    // ── GetServiceMaturity ──

    [Fact]
    public async Task Get_ExistingService_ShouldReturnAssessment()
    {
        var serviceId = Guid.NewGuid();
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: serviceId,
            serviceName: "api-svc",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: true,
            approvalWorkflowActive: true,
            telemetryActive: false,
            baselinesEstablished: false,
            alertsConfigured: false,
            runbooksAvailable: false,
            rollbackTested: false,
            chaosValidated: false,
            assessedBy: "engineer",
            tenantId: "t1",
            now: FixedNow);

        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(assessment));

        var handler = new GetServiceMaturity.Handler(_repository);
        var query = new GetServiceMaturity.Query(serviceId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ServiceId.Should().Be(serviceId);
        result.Value.CurrentLevel.Should().Be(ServiceMaturityLevel.Governed);
        result.Value.OwnershipDefined.Should().BeTrue();
        result.Value.TelemetryActive.Should().BeFalse();
        result.Value.AssessedBy.Should().Be("engineer");
    }

    [Fact]
    public async Task Get_NonExistentService_ShouldReturnNotFoundError()
    {
        var serviceId = Guid.NewGuid();
        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(null));

        var handler = new GetServiceMaturity.Handler(_repository);
        var query = new GetServiceMaturity.Query(serviceId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Governance.ServiceMaturityAssessment.ServiceNotFound");
    }

    [Fact]
    public async Task Get_ShouldReturnAllCriteriaFields()
    {
        var serviceId = Guid.NewGuid();
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId: serviceId,
            serviceName: "full-svc",
            ownershipDefined: true,
            contractsPublished: true,
            documentationExists: true,
            policiesApplied: true,
            approvalWorkflowActive: true,
            telemetryActive: true,
            baselinesEstablished: true,
            alertsConfigured: true,
            runbooksAvailable: true,
            rollbackTested: true,
            chaosValidated: true,
            assessedBy: "auto",
            tenantId: null,
            now: FixedNow);

        _repository.GetByServiceIdAsync(serviceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ServiceMaturityAssessment?>(assessment));

        var handler = new GetServiceMaturity.Handler(_repository);
        var result = await handler.Handle(new GetServiceMaturity.Query(serviceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var r = result.Value;
        r.OwnershipDefined.Should().BeTrue();
        r.ContractsPublished.Should().BeTrue();
        r.DocumentationExists.Should().BeTrue();
        r.PoliciesApplied.Should().BeTrue();
        r.ApprovalWorkflowActive.Should().BeTrue();
        r.TelemetryActive.Should().BeTrue();
        r.BaselinesEstablished.Should().BeTrue();
        r.AlertsConfigured.Should().BeTrue();
        r.RunbooksAvailable.Should().BeTrue();
        r.RollbackTested.Should().BeTrue();
        r.ChaosValidated.Should().BeTrue();
        r.CurrentLevel.Should().Be(ServiceMaturityLevel.Resilient);
    }

    // ── ListServicesByMaturityLevel ──

    [Fact]
    public async Task List_NoFilter_ShouldReturnAll()
    {
        var assessments = new List<ServiceMaturityAssessment>
        {
            ServiceMaturityAssessment.Assess(
                Guid.NewGuid(), "svc-a", true, false, false, false, false,
                false, false, false, false, false, false, "auto", null, FixedNow),
            ServiceMaturityAssessment.Assess(
                Guid.NewGuid(), "svc-b", true, true, true, true, true,
                true, true, true, true, true, true, "auto", null, FixedNow)
        };

        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceMaturityAssessment>>(assessments));

        var handler = new ListServicesByMaturityLevel.Handler(_repository);
        var result = await handler.Handle(new ListServicesByMaturityLevel.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.FilteredLevel.Should().BeNull();
    }

    [Fact]
    public async Task List_WithFilter_ShouldPassFilterToRepository()
    {
        _repository.ListAsync(ServiceMaturityLevel.Governed, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceMaturityAssessment>>([]));

        var handler = new ListServicesByMaturityLevel.Handler(_repository);
        var result = await handler.Handle(
            new ListServicesByMaturityLevel.Query(ServiceMaturityLevel.Governed), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.FilteredLevel.Should().Be(ServiceMaturityLevel.Governed);

        await _repository.Received(1).ListAsync(ServiceMaturityLevel.Governed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_EmptyResult_ShouldReturnEmptyList()
    {
        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceMaturityAssessment>>([]));

        var handler = new ListServicesByMaturityLevel.Handler(_repository);
        var result = await handler.Handle(new ListServicesByMaturityLevel.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task List_ShouldMapDtoFieldsCorrectly()
    {
        var serviceId = Guid.NewGuid();
        var assessment = ServiceMaturityAssessment.Assess(
            serviceId, "mapped-svc", true, true, true, false, false,
            false, false, false, false, false, false, "tester", "t1", FixedNow);

        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ServiceMaturityAssessment>>(new[] { assessment }));

        var handler = new ListServicesByMaturityLevel.Handler(_repository);
        var result = await handler.Handle(new ListServicesByMaturityLevel.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items[0];
        item.ServiceId.Should().Be(serviceId);
        item.ServiceName.Should().Be("mapped-svc");
        item.CurrentLevel.Should().Be(ServiceMaturityLevel.Documented);
        item.AssessedBy.Should().Be("tester");
        item.ReassessmentCount.Should().Be(0);
    }
}
