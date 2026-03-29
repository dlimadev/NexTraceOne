using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade CicsTransaction do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes, uppercase e limites de transactionId.
/// </summary>
public sealed class CicsTransactionTests
{
    private static MainframeSystemId CreateSystemId() => MainframeSystemId.New();
    private static CicsRegion CreateRegion() => CicsRegion.Create("CICSPRD1", "5.6", 1490);

    private static CicsTransaction CreateTransaction() =>
        CicsTransaction.Create("TXN1", CreateSystemId(), "PROG01", CreateRegion());

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var systemId = CreateSystemId();
        var region = CreateRegion();

        var txn = CicsTransaction.Create("TXN1", systemId, "PROG01", region);

        txn.TransactionId.Should().Be("TXN1");
        txn.SystemId.Should().Be(systemId);
        txn.ProgramName.Should().Be("PROG01");
        txn.Region.Should().Be(region);
        txn.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithNullTransactionId_ShouldThrow()
    {
        var act = () => CicsTransaction.Create(null!, CreateSystemId(), "PROG01", CreateRegion());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldUppercaseTransactionId()
    {
        var txn = CicsTransaction.Create("txn1", CreateSystemId(), "PROG01", CreateRegion());

        txn.TransactionId.Should().Be("TXN1");
    }

    [Fact]
    public void Create_WithTooLongTransactionId_ShouldThrow()
    {
        var act = () => CicsTransaction.Create("TXNAB", CreateSystemId(), "PROG01", CreateRegion());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullSystemId_ShouldThrow()
    {
        var act = () => CicsTransaction.Create("TXN1", null!, "PROG01", CreateRegion());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullProgramName_ShouldThrow()
    {
        var act = () => CicsTransaction.Create("TXN1", CreateSystemId(), null!, CreateRegion());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullRegion_ShouldThrow()
    {
        var act = () => CicsTransaction.Create("TXN1", CreateSystemId(), "PROG01", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var txn = CreateTransaction();

        txn.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
