using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Tests.Domain.Entities;

/// <summary>
/// Testes de unidade para a entidade AuditChainLink.
/// Valida criação de link, hash SHA-256, verificação e encadeamento.
/// </summary>
public sealed class AuditChainLinkTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private AuditEvent CreateTestEvent(string module = "TestModule", string action = "TestAction") =>
        AuditEvent.Record(module, action, "r1", "Resource", "user@test.com", _now, Guid.NewGuid());

    [Fact]
    public void Create_ValidInput_ShouldCreateLink()
    {
        var evt = CreateTestEvent();
        var link = AuditChainLink.Create(evt, 1, string.Empty, _now);

        link.Should().NotBeNull();
        link.Id.Value.Should().NotBeEmpty();
        link.SequenceNumber.Should().Be(1);
        link.CurrentHash.Should().NotBeNullOrEmpty();
        link.PreviousHash.Should().BeEmpty();
        link.CreatedAt.Should().Be(_now);
    }

    [Fact]
    public void Create_ShouldGenerateDeterministicHash()
    {
        var evt = CreateTestEvent();
        var link1 = AuditChainLink.Create(evt, 1, string.Empty, _now);
        var link2 = AuditChainLink.Create(evt, 1, string.Empty, _now);

        link1.CurrentHash.Should().Be(link2.CurrentHash);
    }

    [Fact]
    public void Create_DifferentEvents_ShouldProduceDifferentHashes()
    {
        var evt1 = CreateTestEvent("Module1", "Action1");
        var evt2 = CreateTestEvent("Module2", "Action2");

        var link1 = AuditChainLink.Create(evt1, 1, string.Empty, _now);
        var link2 = AuditChainLink.Create(evt2, 1, string.Empty, _now);

        link1.CurrentHash.Should().NotBe(link2.CurrentHash);
    }

    [Fact]
    public void Create_DifferentSequenceNumbers_ShouldProduceDifferentHashes()
    {
        var evt = CreateTestEvent();

        var link1 = AuditChainLink.Create(evt, 1, string.Empty, _now);
        var link2 = AuditChainLink.Create(evt, 2, string.Empty, _now);

        link1.CurrentHash.Should().NotBe(link2.CurrentHash);
    }

    [Fact]
    public void Create_DifferentPreviousHash_ShouldProduceDifferentHashes()
    {
        var evt = CreateTestEvent();

        var link1 = AuditChainLink.Create(evt, 1, "hash-A", _now);
        var link2 = AuditChainLink.Create(evt, 1, "hash-B", _now);

        link1.CurrentHash.Should().NotBe(link2.CurrentHash);
    }

    [Fact]
    public void Create_NullEvent_ShouldThrow()
    {
        var act = () => AuditChainLink.Create(null!, 1, string.Empty, _now);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NegativeSequence_ShouldThrow()
    {
        var evt = CreateTestEvent();
        var act = () => AuditChainLink.Create(evt, -1, string.Empty, _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Verify_ValidChain_ShouldReturnTrue()
    {
        var evt = CreateTestEvent();
        var previousHash = string.Empty;
        var link = AuditChainLink.Create(evt, 1, previousHash, _now);

        link.Verify(evt, previousHash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPreviousHash_ShouldReturnFalse()
    {
        var evt = CreateTestEvent();
        var link = AuditChainLink.Create(evt, 1, "correct-hash", _now);

        link.Verify(evt, "wrong-hash").Should().BeFalse();
    }

    [Fact]
    public void Verify_DifferentEvent_ShouldReturnFalse()
    {
        var evt1 = CreateTestEvent("Module1");
        var evt2 = CreateTestEvent("Module2");
        var link = AuditChainLink.Create(evt1, 1, string.Empty, _now);

        link.Verify(evt2, string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Create_ChainedLinks_ShouldMaintainHashContinuity()
    {
        var evt1 = CreateTestEvent("Mod", "Act1");
        var link1 = AuditChainLink.Create(evt1, 1, string.Empty, _now);

        var evt2 = CreateTestEvent("Mod", "Act2");
        var link2 = AuditChainLink.Create(evt2, 2, link1.CurrentHash, _now);

        link2.PreviousHash.Should().Be(link1.CurrentHash);
        link2.SequenceNumber.Should().Be(2);
    }

    [Fact]
    public void CurrentHash_ShouldBeHexString()
    {
        var evt = CreateTestEvent();
        var link = AuditChainLink.Create(evt, 1, string.Empty, _now);

        link.CurrentHash.Should().MatchRegex("^[0-9A-F]+$");
        link.CurrentHash.Length.Should().Be(64); // SHA-256 = 32 bytes = 64 hex chars
    }
}
