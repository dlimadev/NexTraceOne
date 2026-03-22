using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.GetComplianceReport;
using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para GetComplianceReport.
/// Valida agregação por módulo, contagem de links e integridade.
/// </summary>
public sealed class GetComplianceReportTests
{
    private readonly IAuditEventRepository _eventRepository = Substitute.For<IAuditEventRepository>();
    private readonly IAuditChainRepository _chainRepository = Substitute.For<IAuditChainRepository>();

    private GetComplianceReport.Handler CreateHandler() => new(_eventRepository, _chainRepository);

    [Fact]
    public async Task Handle_WithEventsAndLinks_ShouldReturnComplianceReport()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-30);
        var to = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();

        var events = new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "ServiceCreated", "s1", "Service", "u1", from.AddDays(1), tenantId),
            AuditEvent.Record("Catalog", "ServiceUpdated", "s1", "Service", "u2", from.AddDays(2), tenantId),
            AuditEvent.Record("IdentityAccess", "UserCreated", "u1", "User", "admin", from.AddDays(3), tenantId),
        };

        var chainEvent = AuditEvent.Record("Temp", "Init", "x", "X", "sys", from, tenantId);
        var links = new List<AuditChainLink>
        {
            AuditChainLink.Create(chainEvent, 1, string.Empty, from.AddDays(1)),
            AuditChainLink.Create(chainEvent, 2, "prev", from.AddDays(2)),
            AuditChainLink.Create(chainEvent, 3, "prev2", from.AddDays(3)),
        };

        _eventRepository.SearchAsync(null, null, from, to, 1, 10000, Arg.Any<CancellationToken>())
            .Returns(events);
        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(links);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetComplianceReport.Query(from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(3);
        result.Value.TotalChainLinks.Should().Be(3);
        result.Value.ChainIntact.Should().BeTrue();
        result.Value.From.Should().Be(from);
        result.Value.To.Should().Be(to);
    }

    [Fact]
    public async Task Handle_ShouldGroupByModule()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();

        var events = new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "A", "r1", "T", "u", from.AddDays(1), tenantId),
            AuditEvent.Record("Catalog", "B", "r2", "T", "u", from.AddDays(2), tenantId),
            AuditEvent.Record("IdentityAccess", "C", "r3", "T", "u", from.AddDays(3), tenantId),
            AuditEvent.Record("Governance", "D", "r4", "T", "u", from.AddDays(4), tenantId),
        };

        _eventRepository.SearchAsync(null, null, from, to, 1, 10000, Arg.Any<CancellationToken>())
            .Returns(events);
        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetComplianceReport.Query(from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ModuleBreakdown.Should().HaveCount(3);

        var catalogModule = result.Value.ModuleBreakdown.FirstOrDefault(m => m.SourceModule == "Catalog");
        catalogModule.Should().NotBeNull();
        catalogModule!.EventCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoEvents_ShouldReturnEmptyReport()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        _eventRepository.SearchAsync(null, null, from, to, 1, 10000, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());
        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetComplianceReport.Query(from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(0);
        result.Value.TotalChainLinks.Should().Be(0);
        result.Value.ModuleBreakdown.Should().BeEmpty();
    }
}
