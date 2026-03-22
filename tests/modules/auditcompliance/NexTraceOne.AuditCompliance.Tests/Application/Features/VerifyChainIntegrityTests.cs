using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Application.Features.VerifyChainIntegrity;
using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Application.Features;

/// <summary>
/// Testes de unidade para VerifyChainIntegrity.
/// Valida verificação de cadeia íntegra, violações e cadeia vazia.
/// </summary>
public sealed class VerifyChainIntegrityTests
{
    private readonly IAuditEventRepository _eventRepository = Substitute.For<IAuditEventRepository>();
    private readonly IAuditChainRepository _chainRepository = Substitute.For<IAuditChainRepository>();

    private VerifyChainIntegrity.Handler CreateHandler() => new(_eventRepository, _chainRepository);

    [Fact]
    public async Task Handle_EmptyChain_ShouldReturnIntact()
    {
        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink>());

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyChainIntegrity.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeTrue();
        result.Value.TotalLinks.Should().Be(0);
        result.Value.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidChain_ShouldReturnIntact()
    {
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();

        var evt1 = AuditEvent.Record("M", "A1", "r1", "T", "u", now, tenantId);
        var link1 = AuditChainLink.Create(evt1, 1, string.Empty, now);

        var evt2 = AuditEvent.Record("M", "A2", "r2", "T", "u", now.AddSeconds(1), tenantId);
        var link2 = AuditChainLink.Create(evt2, 2, link1.CurrentHash, now.AddSeconds(1));

        var evt3 = AuditEvent.Record("M", "A3", "r3", "T", "u", now.AddSeconds(2), tenantId);
        var link3 = AuditChainLink.Create(evt3, 3, link2.CurrentHash, now.AddSeconds(2));

        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink> { link1, link2, link3 });

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyChainIntegrity.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeTrue();
        result.Value.TotalLinks.Should().Be(3);
        result.Value.Violations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_BrokenChain_ShouldReturnViolations()
    {
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();

        var evt1 = AuditEvent.Record("M", "A1", "r1", "T", "u", now, tenantId);
        var link1 = AuditChainLink.Create(evt1, 1, string.Empty, now);

        // Create link2 with wrong previous hash (simulates tampering)
        var evt2 = AuditEvent.Record("M", "A2", "r2", "T", "u", now.AddSeconds(1), tenantId);
        var link2 = AuditChainLink.Create(evt2, 2, "TAMPERED-HASH", now.AddSeconds(1));

        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink> { link1, link2 });

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyChainIntegrity.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeFalse();
        result.Value.Violations.Should().NotBeEmpty();
        result.Value.Violations.Should().Contain(v => v.SequenceNumber == 2);
    }

    [Fact]
    public async Task Handle_SingleLink_ShouldReturnIntactIfValid()
    {
        var now = DateTimeOffset.UtcNow;
        var evt = AuditEvent.Record("M", "A", "r", "T", "u", now, Guid.NewGuid());
        var link = AuditChainLink.Create(evt, 1, string.Empty, now);

        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink> { link });

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyChainIntegrity.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeTrue();
        result.Value.TotalLinks.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReportAllViolations()
    {
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();

        // All links have wrong previous hashes
        var evt1 = AuditEvent.Record("M", "A1", "r1", "T", "u", now, tenantId);
        var link1 = AuditChainLink.Create(evt1, 1, "wrong-genesis", now);

        var evt2 = AuditEvent.Record("M", "A2", "r2", "T", "u", now.AddSeconds(1), tenantId);
        var link2 = AuditChainLink.Create(evt2, 2, "wrong-prev", now.AddSeconds(1));

        _chainRepository.GetAllLinksAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditChainLink> { link1, link2 });

        var handler = CreateHandler();
        var result = await handler.Handle(new VerifyChainIntegrity.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeFalse();
        // link1 has PreviousHash "wrong-genesis" but expected "" → violation at seq 1
        // link2 has PreviousHash "wrong-prev" but expected link1.CurrentHash → violation at seq 2
        result.Value.Violations.Should().HaveCount(2);
    }
}
