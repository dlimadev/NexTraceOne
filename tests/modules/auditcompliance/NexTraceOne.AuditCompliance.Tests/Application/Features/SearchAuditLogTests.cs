using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.SearchAuditLog;
using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para SearchAuditLog.
/// Valida pesquisa com filtros, paginação e validações do query.
/// </summary>
public sealed class SearchAuditLogTests
{
    private readonly IAuditEventRepository _eventRepository = Substitute.For<IAuditEventRepository>();

    private SearchAuditLog.Handler CreateHandler() => new(_eventRepository);

    [Fact]
    public async Task Handle_WithFilters_ShouldReturnResults()
    {
        var now = DateTimeOffset.UtcNow;
        var events = new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "ServiceCreated", "s1", "Service", "user@org.com", now, Guid.NewGuid()),
        };

        _eventRepository.SearchAsync("Catalog", null, null, null, null, 1, 25, Arg.Any<CancellationToken>())
            .Returns(events);

        var handler = CreateHandler();
        var query = new SearchAuditLog.Query("Catalog", null, null, null, null, 1, 25);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].SourceModule.Should().Be("Catalog");
    }

    [Fact]
    public async Task Handle_NoResults_ShouldReturnEmptyList()
    {
        _eventRepository.SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), 1, 25, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());

        var handler = CreateHandler();
        var result = await handler.Handle(new SearchAuditLog.Query(null, null, null, null, null, 1, 25), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = AuditEvent.Record("Governance", "PackPublished", "pack-1", "GovernancePack", "admin@org.com", now, Guid.NewGuid());

        _eventRepository.SearchAsync("Governance", "PackPublished", null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent> { evt });

        var handler = CreateHandler();
        var result = await handler.Handle(new SearchAuditLog.Query("Governance", "PackPublished", null, null, null, 1, 10), CancellationToken.None);

        var item = result.Value.Items[0];
        item.SourceModule.Should().Be("Governance");
        item.ActionType.Should().Be("PackPublished");
        item.ResourceType.Should().Be("GovernancePack");
        item.ResourceId.Should().Be("pack-1");
        item.PerformedBy.Should().Be("admin@org.com");
        item.OccurredAt.Should().Be(now);
    }

    // ── Validator Tests ──

    [Fact]
    public void Validator_ValidQuery_ShouldPass()
    {
        var validator = new SearchAuditLog.Validator();
        validator.Validate(new SearchAuditLog.Query(null, null, null, null, null, 1, 25)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_PageZero_ShouldFail()
    {
        var validator = new SearchAuditLog.Validator();
        validator.Validate(new SearchAuditLog.Query(null, null, null, null, null, 0, 25)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_NegativePage_ShouldFail()
    {
        var validator = new SearchAuditLog.Validator();
        validator.Validate(new SearchAuditLog.Query(null, null, null, null, null, -1, 25)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_PageSizeZero_ShouldFail()
    {
        var validator = new SearchAuditLog.Validator();
        validator.Validate(new SearchAuditLog.Query(null, null, null, null, null, 1, 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_PageSizeTooLarge_ShouldFail()
    {
        var validator = new SearchAuditLog.Validator();
        validator.Validate(new SearchAuditLog.Query(null, null, null, null, null, 1, 101)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_MaxPageSize_ShouldPass()
    {
        var validator = new SearchAuditLog.Validator();
        validator.Validate(new SearchAuditLog.Query(null, null, null, null, null, 1, 100)).IsValid.Should().BeTrue();
    }
}
