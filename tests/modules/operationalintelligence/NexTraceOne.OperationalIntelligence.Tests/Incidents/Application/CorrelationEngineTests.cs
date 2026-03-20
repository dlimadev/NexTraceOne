using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListChanges;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateIncident;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RefreshIncidentCorrelation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Services;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NSubstitute;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.Application;

/// <summary>
/// Testes para a correlation engine e fluxo de criação de incident.
/// Verifica critérios reais: proximidade temporal, interseção de serviço e blast radius.
/// </summary>
public sealed class CorrelationEngineTests
{
    // ── ISender stub para testes sem DI completa ─────────────────────

    /// <summary>
    /// Stub de ISender que permite configurar respostas por tipo de request.
    /// Evita problemas com mocking de métodos genéricos via NSubstitute.
    /// </summary>
    private sealed class StubSender : ISender
    {
        private ListChanges.Response? _listChangesResult;
        private GetBlastRadiusReport.Response? _blastRadiusResult;
        private bool _listChangesFails;

        public void SetupListChanges(ListChanges.Response response) => _listChangesResult = response;
        public void SetupListChangesFails() => _listChangesFails = true;
        public void SetupBlastRadius(GetBlastRadiusReport.Response? response) => _blastRadiusResult = response;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListChanges.Query)
            {
                if (_listChangesFails)
                {
                    var failure = (Result<ListChanges.Response>)Error.NotFound("Infrastructure.Unavailable", "Database unavailable");
                    return Task.FromResult((TResponse)(object)failure);
                }
                var result = Result<ListChanges.Response>.Success(
                    _listChangesResult ?? new ListChanges.Response([], 0, 1, 20));
                return Task.FromResult((TResponse)(object)result);
            }

            if (request is GetBlastRadiusReport.Query blastQuery)
            {
                if (_blastRadiusResult is null)
                {
                    var failure = (Result<GetBlastRadiusReport.Response>)Error.NotFound("BlastRadius.NotFound", "Report not found");
                    return Task.FromResult((TResponse)(object)failure);
                }
                var result = Result<GetBlastRadiusReport.Response>.Success(_blastRadiusResult);
                return Task.FromResult((TResponse)(object)result);
            }

            throw new NotImplementedException($"No stub configured for request type: {request.GetType().Name}");
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        Task ISender.Send<TRequest>(TRequest request, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    // ── CreateIncident.Validator ─────────────────────────────────────

    [Fact]
    public void CreateIncident_Validator_ShouldAcceptValidCommand()
    {
        var validator = new CreateIncident.Validator();
        var command = new CreateIncident.Command(
            "Payment gateway degraded",
            "Error rate above 10% for 15 minutes",
            IncidentType.ServiceDegradation,
            IncidentSeverity.Major,
            "svc-payment-gateway",
            "Payment Gateway",
            "payment-squad",
            "Payments",
            "Production",
            DateTimeOffset.UtcNow);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateIncident_Validator_ShouldRejectEmptyTitle()
    {
        var validator = new CreateIncident.Validator();
        var command = new CreateIncident.Command(
            "",
            "Description",
            IncidentType.ServiceDegradation,
            IncidentSeverity.Major,
            "svc-payment-gateway",
            "Payment Gateway",
            "payment-squad",
            null,
            "Production",
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateIncident_Validator_ShouldRejectEmptyServiceId()
    {
        var validator = new CreateIncident.Validator();
        var command = new CreateIncident.Command(
            "Title",
            "Description",
            IncidentType.ServiceDegradation,
            IncidentSeverity.Major,
            "",
            "Payment Gateway",
            "payment-squad",
            null,
            "Production",
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceId");
    }

    [Fact]
    public void CreateIncident_Validator_ShouldRejectEmptyOwnerTeam()
    {
        var validator = new CreateIncident.Validator();
        var command = new CreateIncident.Command(
            "Title",
            "Description",
            IncidentType.ServiceDegradation,
            IncidentSeverity.Major,
            "svc-payment",
            "Payment",
            "",
            null,
            "Production",
            null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerTeam");
    }

    // ── CreateIncident.Handler ───────────────────────────────────────

    [Fact]
    public async Task CreateIncident_Handler_ShouldReturnCreatedIncidentWithCorrelation()
    {
        var store = Substitute.For<IIncidentStore>();
        var correlationService = Substitute.For<IIncidentCorrelationService>();

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.CreateIncident(Arg.Any<CreateIncidentInput>())
            .Returns(new CreateIncidentResult(incidentId, "INC-2026-9999", now));

        correlationService.RecomputeAsync(incidentId.ToString(), Arg.Any<CancellationToken>())
            .Returns(new GetIncidentCorrelation.Response(
                incidentId,
                CorrelationConfidence.High,
                80m,
                "Temporal proximity and service match.",
                [new GetIncidentCorrelation.CorrelatedChange(Guid.NewGuid(), "Deploy v2.14.0", "Deployment", "HighEvidence", now.AddHours(-2))],
                [new GetIncidentCorrelation.CorrelatedService("svc-payment-gateway", "Payment Gateway", "Primary")],
                [],
                []));

        var handler = new CreateIncident.Handler(store, correlationService,
            Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentTenant>(),
            Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentEnvironment>());
        var command = new CreateIncident.Command(
            "Payment gateway degraded",
            "Error rate above 10%",
            IncidentType.ServiceDegradation,
            IncidentSeverity.Major,
            "svc-payment-gateway",
            "Payment Gateway",
            "payment-squad",
            "Payments",
            "Production",
            now);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncidentId.Should().Be(incidentId);
        result.Value.Reference.Should().Be("INC-2026-9999");
        result.Value.HasCorrelatedChanges.Should().BeTrue();
        result.Value.CorrelationScore.Should().Be(80m);
        result.Value.CorrelationConfidence.Should().Be(CorrelationConfidence.High);
        await correlationService.Received(1).RecomputeAsync(incidentId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateIncident_Handler_WhenNoCorrelation_ShouldReturnNotAssessed()
    {
        var store = Substitute.For<IIncidentStore>();
        var correlationService = Substitute.For<IIncidentCorrelationService>();

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.CreateIncident(Arg.Any<CreateIncidentInput>())
            .Returns(new CreateIncidentResult(incidentId, "INC-2026-9998", now));

        correlationService.RecomputeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GetIncidentCorrelation.Response?)null);

        var handler = new CreateIncident.Handler(store, correlationService,
            Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentTenant>(),
            Substitute.For<NexTraceOne.BuildingBlocks.Application.Abstractions.ICurrentEnvironment>());
        var command = new CreateIncident.Command(
            "Catalog sync failure",
            "External API returning 503",
            IncidentType.DependencyFailure,
            IncidentSeverity.Minor,
            "svc-catalog-sync",
            "Catalog Sync",
            "catalog-squad",
            null,
            "Production",
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasCorrelatedChanges.Should().BeFalse();
        result.Value.CorrelationScore.Should().Be(0m);
        result.Value.CorrelationConfidence.Should().Be(CorrelationConfidence.NotAssessed);
    }

    // ── RefreshIncidentCorrelation.Handler ───────────────────────────

    [Fact]
    public async Task RefreshIncidentCorrelation_Handler_ShouldInvokeRecomputeAndReturnResult()
    {
        var correlationService = Substitute.For<IIncidentCorrelationService>();
        var incidentId = "a1b2c3d4-0001-0000-0000-000000000001";

        correlationService.RecomputeAsync(incidentId, Arg.Any<CancellationToken>())
            .Returns(new GetIncidentCorrelation.Response(
                Guid.Parse(incidentId),
                CorrelationConfidence.Medium,
                55m,
                "Partial match based on service name.",
                [],
                [],
                [],
                []));

        var handler = new RefreshIncidentCorrelation.Handler(correlationService);
        var command = new RefreshIncidentCorrelation.Command(incidentId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Confidence.Should().Be(CorrelationConfidence.Medium);
        result.Value.Score.Should().Be(55m);
    }

    [Fact]
    public async Task RefreshIncidentCorrelation_Handler_WhenIncidentNotFound_ShouldReturnFailure()
    {
        var correlationService = Substitute.For<IIncidentCorrelationService>();

        correlationService.RecomputeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((GetIncidentCorrelation.Response?)null);

        var handler = new RefreshIncidentCorrelation.Handler(correlationService);
        var command = new RefreshIncidentCorrelation.Command("nonexistent-id");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    // ── IncidentCorrelationService — real scoring with stub sender ───

    [Fact]
    public async Task CorrelationService_WhenChangeMatchesServiceAndTemporalWindow_ShouldReturnHighScore()
    {
        var store = Substitute.For<IIncidentStore>();
        var sender = new StubSender();
        var logger = NullLogger<IncidentCorrelationService>.Instance;

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var changeId = Guid.NewGuid();

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId,
                "svc-payment-gateway",
                "Payment Gateway",
                "Production",
                now));

        // Mudança próxima temporalmente (30 min antes) e com mesmo nome de serviço
        sender.SetupListChanges(new ListChanges.Response(
            [new ListChanges.ChangeDto(
                changeId,
                Guid.NewGuid(),
                "Payment Gateway",   // mesmo nome → service score alto
                "2.14.0",
                "Production",
                "Deployment",
                "Deployed",
                "Major",
                "Validated",
                "Passed",
                80m,
                "payment-squad",
                "Payments",
                "Deploy v2.14.0 to Payment Gateway",
                null,
                "abc123",
                now.AddMinutes(-30))  // 30 min antes → temporal score alto
            ],
            1, 1, 20));

        sender.SetupBlastRadius(new GetBlastRadiusReport.Response(
            Guid.NewGuid(), changeId, 5,
            ["svc-order-api", "svc-payment-gateway"],
            [],
            now));

        var service = new IncidentCorrelationService(store, sender, logger);

        var response = await service.RecomputeAsync(incidentId.ToString(), CancellationToken.None);

        response.Should().NotBeNull();
        response!.RelatedChanges.Should().HaveCount(1);
        response.RelatedChanges[0].ChangeId.Should().Be(changeId);
        response.Score.Should().BeGreaterThan(50m);
        response.Confidence.Should().NotBe(CorrelationConfidence.NotAssessed);
        store.Received(1).SaveIncidentCorrelation(incidentId.ToString(), Arg.Any<GetIncidentCorrelation.Response>());
    }

    [Fact]
    public async Task CorrelationService_WhenNoChangesFound_ShouldReturnNotAssessed()
    {
        var store = Substitute.For<IIncidentStore>();
        var sender = new StubSender();
        var logger = NullLogger<IncidentCorrelationService>.Instance;

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId,
                "svc-catalog-sync",
                "Catalog Sync",
                "Production",
                now));

        sender.SetupListChanges(new ListChanges.Response([], 0, 1, 20));

        var service = new IncidentCorrelationService(store, sender, logger);

        var response = await service.RecomputeAsync(incidentId.ToString(), CancellationToken.None);

        response.Should().NotBeNull();
        response!.RelatedChanges.Should().BeEmpty();
        response.Score.Should().Be(0m);
        response.Confidence.Should().Be(CorrelationConfidence.NotAssessed);
    }

    [Fact]
    public async Task CorrelationService_WhenListChangesFails_ShouldReturnNotAssessedGracefully()
    {
        var store = Substitute.For<IIncidentStore>();
        var sender = new StubSender();
        var logger = NullLogger<IncidentCorrelationService>.Instance;

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId,
                "svc-payment-gateway",
                "Payment Gateway",
                "Production",
                now));

        // Simula falha no ListChanges (ex: infra indisponível)
        sender.SetupListChangesFails();

        var service = new IncidentCorrelationService(store, sender, logger);

        // Deve executar sem lançar exceção mesmo quando ListChanges falha, retornando correlação vazia.
        var response = await service.RecomputeAsync(incidentId.ToString(), CancellationToken.None);

        response.Should().NotBeNull();
        response!.RelatedChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task CorrelationService_WhenIncidentNotFound_ShouldReturnNull()
    {
        var store = Substitute.For<IIncidentStore>();
        var sender = new StubSender();
        var logger = NullLogger<IncidentCorrelationService>.Instance;

        store.GetIncidentCorrelationContext("nonexistent").Returns((IncidentCorrelationContext?)null);

        var service = new IncidentCorrelationService(store, sender, logger);

        var response = await service.RecomputeAsync("nonexistent", CancellationToken.None);

        response.Should().BeNull();
    }

    [Fact]
    public async Task CorrelationService_WhenChangeOutsideTemporalWindow_ShouldHaveLowScore()
    {
        var store = Substitute.For<IIncidentStore>();
        var sender = new StubSender();
        var logger = NullLogger<IncidentCorrelationService>.Instance;

        var incidentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var changeId = Guid.NewGuid();

        store.GetIncidentCorrelationContext(incidentId.ToString())
            .Returns(new IncidentCorrelationContext(
                incidentId,
                "svc-payment-gateway",
                "Payment Gateway",
                "Production",
                now));

        // Mudança com nome diferente e 2 dias antes → score baixo ou zero
        sender.SetupListChanges(new ListChanges.Response(
            [new ListChanges.ChangeDto(
                changeId,
                Guid.NewGuid(),
                "Completely Unrelated Service",
                "1.0.0",
                "Production",
                "Deployment",
                "Deployed",
                "Minor",
                "Validated",
                "Passed",
                30m,
                "other-squad",
                null,
                null,
                null,
                "def456",
                now.AddDays(-2))  // 2 dias antes → temporal score zero
            ],
            1, 1, 20));

        var service = new IncidentCorrelationService(store, sender, logger);

        var response = await service.RecomputeAsync(incidentId.ToString(), CancellationToken.None);

        response.Should().NotBeNull();
        // Score 0 → candidato filtrado → sem related changes
        response!.RelatedChanges.Should().BeEmpty();
        response.Confidence.Should().Be(CorrelationConfidence.NotAssessed);
    }
}
