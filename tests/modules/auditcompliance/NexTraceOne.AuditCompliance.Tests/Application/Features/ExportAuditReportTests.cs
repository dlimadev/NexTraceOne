using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.ExportAuditReport;
using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para ExportAuditReport.
/// Valida exportação por período, mapeamento e validações do query.
/// </summary>
public sealed class ExportAuditReportTests
{
    private readonly IAuditEventRepository _eventRepository = Substitute.For<IAuditEventRepository>();

    private ExportAuditReport.Handler CreateHandler() => new(_eventRepository);

    [Fact]
    public async Task Handle_WithEvents_ShouldReturnReport()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var events = new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "ServiceCreated", "svc-1", "Service", "user1@org.com", from.AddDays(1), Guid.NewGuid()),
            AuditEvent.Record("IdentityAccess", "UserCreated", "usr-1", "User", "admin@org.com", from.AddDays(2), Guid.NewGuid()),
            AuditEvent.Record("Catalog", "ServiceUpdated", "svc-1", "Service", "user2@org.com", from.AddDays(3), Guid.NewGuid()),
        };

        _eventRepository.SearchAsync(null, null, from, to, 1, 10000, Arg.Any<CancellationToken>())
            .Returns(events);

        var handler = CreateHandler();
        var result = await handler.Handle(new ExportAuditReport.Query(from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(3);
        result.Value.From.Should().Be(from);
        result.Value.To.Should().Be(to);
        result.Value.Entries.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_NoEvents_ShouldReturnEmptyReport()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        _eventRepository.SearchAsync(null, null, from, to, 1, 10000, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());

        var handler = CreateHandler();
        var result = await handler.Handle(new ExportAuditReport.Query(from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(0);
        result.Value.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapEntryFields()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var evt = AuditEvent.Record("Governance", "PackPublished", "pack-1", "GovernancePack", "admin@org.com", from.AddHours(1), Guid.NewGuid());

        _eventRepository.SearchAsync(null, null, from, to, 1, 10000, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent> { evt });

        var handler = CreateHandler();
        var result = await handler.Handle(new ExportAuditReport.Query(from, to), CancellationToken.None);

        var entry = result.Value.Entries[0];
        entry.SourceModule.Should().Be("Governance");
        entry.ActionType.Should().Be("PackPublished");
        entry.ResourceType.Should().Be("GovernancePack");
        entry.ResourceId.Should().Be("pack-1");
        entry.PerformedBy.Should().Be("admin@org.com");
    }

    // ── Validator Tests ──

    [Fact]
    public void Validator_ValidRange_ShouldPass()
    {
        var validator = new ExportAuditReport.Validator();
        var query = new ExportAuditReport.Query(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);
        validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_FromAfterTo_ShouldFail()
    {
        var validator = new ExportAuditReport.Validator();
        var query = new ExportAuditReport.Query(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-7));
        validator.Validate(query).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_SameFromAndTo_ShouldFail()
    {
        var now = DateTimeOffset.UtcNow;
        var validator = new ExportAuditReport.Validator();
        var query = new ExportAuditReport.Query(now, now);
        validator.Validate(query).IsValid.Should().BeFalse();
    }
}
