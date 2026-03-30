using System.Linq;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetUnifiedTimeline;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.ListIncidents;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Tests.Incidents.LegacyCorrelation;

/// <summary>
/// Testes unitários para GetUnifiedTimeline (query + validator + handler).
/// Valida validação de parâmetros e construção correcta de timeline a partir de incidentes.
/// </summary>
public sealed class GetUnifiedTimelineTests
{
    private readonly GetUnifiedTimeline.Validator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var query = new GetUnifiedTimeline.Query(
            ServiceName: "PaymentService",
            SystemName: "SYS1",
            Environment: "production",
            From: DateTimeOffset.UtcNow.AddDays(-7),
            To: DateTimeOffset.UtcNow,
            PageSize: 50,
            Page: 1);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidPageSize_Fails()
    {
        var query = new GetUnifiedTimeline.Query(null, null, null, null, null, PageSize: 0);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_PageSizeTooLarge_Fails()
    {
        var query = new GetUnifiedTimeline.Query(null, null, null, null, null, PageSize: 201);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_InvalidPage_Fails()
    {
        var query = new GetUnifiedTimeline.Query(null, null, null, null, null, Page: 0);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public async Task Handle_ReturnsTimelineFromIncidents()
    {
        var store = Substitute.For<IIncidentStore>();
        var obsProvider = Substitute.For<IObservabilityProvider>();
        var logger = Substitute.For<ILogger<GetUnifiedTimeline.Handler>>();
        var handler = new GetUnifiedTimeline.Handler(store, obsProvider, logger);

        var incidents = new List<ListIncidents.IncidentListItem>
        {
            new(Guid.NewGuid(), "INC-001", "Server timeout", IncidentType.ServiceDegradation,
                IncidentSeverity.Major, IncidentStatus.Open, "svc-1", "Payment Service",
                "team-payments", "production", DateTimeOffset.UtcNow.AddHours(-2),
                false, CorrelationConfidence.NotAssessed, MitigationStatus.NotStarted),
            new(Guid.NewGuid(), "INC-002", "Queue overflow", IncidentType.MessagingIssue,
                IncidentSeverity.Critical, IncidentStatus.Investigating, "svc-2", "Order Service",
                "team-orders", "production", DateTimeOffset.UtcNow.AddHours(-1),
                true, CorrelationConfidence.High, MitigationStatus.InProgress)
        };

        store.GetIncidentListItems().Returns(incidents);

        var query = new GetUnifiedTimeline.Query(null, null, "production", null, null, 50, 1);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Entries.Should().HaveCount(2);
        result.Value.Entries.First().Source.Should().Be("incident");
    }

    [Fact]
    public async Task Handle_FiltersIncidentsByServiceName()
    {
        var store = Substitute.For<IIncidentStore>();
        var obsProvider = Substitute.For<IObservabilityProvider>();
        var logger = Substitute.For<ILogger<GetUnifiedTimeline.Handler>>();
        var handler = new GetUnifiedTimeline.Handler(store, obsProvider, logger);

        var incidents = new List<ListIncidents.IncidentListItem>
        {
            new(Guid.NewGuid(), "INC-001", "Server timeout", IncidentType.ServiceDegradation,
                IncidentSeverity.Major, IncidentStatus.Open, "svc-1", "Payment Service",
                "team-payments", "production", DateTimeOffset.UtcNow,
                false, CorrelationConfidence.NotAssessed, MitigationStatus.NotStarted),
            new(Guid.NewGuid(), "INC-002", "Queue overflow", IncidentType.MessagingIssue,
                IncidentSeverity.Critical, IncidentStatus.Investigating, "svc-2", "Order Service",
                "team-orders", "production", DateTimeOffset.UtcNow,
                true, CorrelationConfidence.High, MitigationStatus.InProgress)
        };

        store.GetIncidentListItems().Returns(incidents);

        var query = new GetUnifiedTimeline.Query("Payment Service", null, null, null, null, 50, 1);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Entries.Single().ServiceName.Should().Be("Payment Service");
    }
}
