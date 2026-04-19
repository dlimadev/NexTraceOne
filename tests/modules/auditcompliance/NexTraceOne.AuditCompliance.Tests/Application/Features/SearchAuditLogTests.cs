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
        _eventRepository.CountAsync("Catalog", null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = CreateHandler();
        var query = new SearchAuditLog.Query("Catalog", null, null, null, null, 1, 25);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].SourceModule.Should().Be("Catalog");
    }

    [Fact]
    public async Task Handle_NoResults_ShouldReturnEmptyListWithZeroPagination()
    {
        _eventRepository.SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), 1, 25, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());
        _eventRepository.CountAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = CreateHandler();
        var result = await handler.Handle(new SearchAuditLog.Query(null, null, null, null, null, 1, 25), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPaginationMetadata()
    {
        var now = DateTimeOffset.UtcNow;
        var events = Enumerable.Range(1, 20)
            .Select(i => AuditEvent.Record("Mod", "Act", $"r{i}", "T", "u@o.com", now, Guid.NewGuid()))
            .ToList();

        _eventRepository.SearchAsync(null, null, null, null, null, 2, 20, Arg.Any<CancellationToken>())
            .Returns(events);
        _eventRepository.CountAsync(null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(45);

        var handler = CreateHandler();
        var result = await handler.Handle(new SearchAuditLog.Query(null, null, null, null, null, 2, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(45);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(3); // ceil(45/20) = 3
    }

    [Fact]
    public async Task Handle_ShouldMapAllFields()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = AuditEvent.Record("Governance", "PackPublished", "pack-1", "GovernancePack", "admin@org.com", now, Guid.NewGuid());

        _eventRepository.SearchAsync("Governance", "PackPublished", null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent> { evt });
        _eventRepository.CountAsync("Governance", "PackPublished", null, null, null, Arg.Any<CancellationToken>())
            .Returns(1);

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

    [Fact]
    public async Task Handle_WithResourceFilters_ShouldUseSearchWithResourceAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var events = new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "ServiceUpdated", "svc-1", "Service", "dev@org.com", now, Guid.NewGuid()),
        };

        _eventRepository.SearchWithResourceAsync(
            null, null, null,
            "Service", "svc-1",
            null, null,
            1, 20, Arg.Any<CancellationToken>())
            .Returns(events);
        _eventRepository.CountWithResourceAsync(
            null, null, null,
            "Service", "svc-1",
            null, null, Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = CreateHandler();
        var query = new SearchAuditLog.Query(null, null, null, null, null, 1, 20, "Service", "svc-1");
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);

        // Ensure the resource-specific search was used, not the generic one
        await _eventRepository.DidNotReceive()
            .SearchAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithResourceFilters_ShouldReturnCorrectPagination()
    {
        var now = DateTimeOffset.UtcNow;
        _eventRepository.SearchWithResourceAsync(
            null, null, null,
            "Release", null,
            null, null,
            1, 10, Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());
        _eventRepository.CountWithResourceAsync(
            null, null, null,
            "Release", null,
            null, null, Arg.Any<CancellationToken>())
            .Returns(0);

        var handler = CreateHandler();
        var result = await handler.Handle(new SearchAuditLog.Query(null, null, null, null, null, 1, 10, "Release", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
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
