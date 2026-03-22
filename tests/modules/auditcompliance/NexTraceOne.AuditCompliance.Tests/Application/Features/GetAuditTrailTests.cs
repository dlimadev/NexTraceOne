using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.GetAuditTrail;
using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para GetAuditTrail.
/// Valida filtragem por recurso, mapeamento de DTOs e validações.
/// </summary>
public sealed class GetAuditTrailTests
{
    private readonly IAuditEventRepository _eventRepository = Substitute.For<IAuditEventRepository>();

    private GetAuditTrail.Handler CreateHandler() => new(_eventRepository);

    [Fact]
    public async Task Handle_WithEvents_ShouldReturnTrailItems()
    {
        var now = DateTimeOffset.UtcNow;
        var events = new List<AuditEvent>
        {
            AuditEvent.Record("Catalog", "ServiceCreated", "svc-1", "Service", "user1@org.com", now.AddHours(-2), Guid.NewGuid()),
            AuditEvent.Record("Catalog", "ServiceUpdated", "svc-1", "Service", "user2@org.com", now.AddHours(-1), Guid.NewGuid()),
        };

        _eventRepository.GetTrailByResourceAsync("Service", "svc-1", Arg.Any<CancellationToken>())
            .Returns(events);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAuditTrail.Query("Service", "svc-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].SourceModule.Should().Be("Catalog");
        result.Value.Items[0].ActionType.Should().Be("ServiceCreated");
        result.Value.Items[0].PerformedBy.Should().Be("user1@org.com");
        result.Value.Items[1].ActionType.Should().Be("ServiceUpdated");
    }

    [Fact]
    public async Task Handle_NoEvents_ShouldReturnEmptyList()
    {
        _eventRepository.GetTrailByResourceAsync("Service", "svc-unknown", Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent>());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAuditTrail.Query("Service", "svc-unknown"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithChainLink_ShouldMapChainHash()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = AuditEvent.Record("Mod", "Act", "r1", "Type", "u", now, Guid.NewGuid());
        var link = AuditChainLink.Create(evt, 1, string.Empty, now);
        evt.LinkToChain(link);

        _eventRepository.GetTrailByResourceAsync("Type", "r1", Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent> { evt });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAuditTrail.Query("Type", "r1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].ChainHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithoutChainLink_ShouldMapNullHash()
    {
        var evt = AuditEvent.Record("Mod", "Act", "r1", "Type", "u", DateTimeOffset.UtcNow, Guid.NewGuid());

        _eventRepository.GetTrailByResourceAsync("Type", "r1", Arg.Any<CancellationToken>())
            .Returns(new List<AuditEvent> { evt });

        var handler = CreateHandler();
        var result = await handler.Handle(new GetAuditTrail.Query("Type", "r1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].ChainHash.Should().BeNull();
    }

    // ── Validator Tests ──

    [Fact]
    public void Validator_ValidQuery_ShouldPass()
    {
        var validator = new GetAuditTrail.Validator();
        validator.Validate(new GetAuditTrail.Query("Service", "svc-1")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptyResourceType_ShouldFail()
    {
        var validator = new GetAuditTrail.Validator();
        validator.Validate(new GetAuditTrail.Query("", "svc-1")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_EmptyResourceId_ShouldFail()
    {
        var validator = new GetAuditTrail.Validator();
        validator.Validate(new GetAuditTrail.Query("Service", "")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ResourceTypeTooLong_ShouldFail()
    {
        var validator = new GetAuditTrail.Validator();
        validator.Validate(new GetAuditTrail.Query(new string('A', 201), "svc-1")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_ResourceIdTooLong_ShouldFail()
    {
        var validator = new GetAuditTrail.Validator();
        validator.Validate(new GetAuditTrail.Query("Service", new string('R', 501))).IsValid.Should().BeFalse();
    }
}
