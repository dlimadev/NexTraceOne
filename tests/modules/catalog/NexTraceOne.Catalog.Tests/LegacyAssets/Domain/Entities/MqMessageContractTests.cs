using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Tests.LegacyAssets.Domain.Entities;

/// <summary>
/// Testes de domínio para a entidade MqMessageContract do sub-domínio Legacy Assets.
/// Cobre criação, validação de invariantes, mutações controladas.
/// </summary>
public sealed class MqMessageContractTests
{
    private static MainframeSystemId CreateSystemId() => MainframeSystemId.New();

    [Fact]
    public void Create_WithValidInput_ShouldSucceed()
    {
        var systemId = CreateSystemId();

        var contract = MqMessageContract.Create("QUEUE.CUSTOMER.REQ", "MQFMT_STRING", systemId);

        contract.QueueName.Should().Be("QUEUE.CUSTOMER.REQ");
        contract.MessageFormat.Should().Be("MQFMT_STRING");
        contract.SystemId.Should().Be(systemId);
        contract.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var contract = MqMessageContract.Create("QUEUE.TEST", "MQFMT_STRING", CreateSystemId());

        contract.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldTrimValues()
    {
        var contract = MqMessageContract.Create("  QUEUE.TEST  ", "  MQFMT_STRING  ", CreateSystemId());

        contract.QueueName.Should().Be("QUEUE.TEST");
        contract.MessageFormat.Should().Be("MQFMT_STRING");
    }

    [Fact]
    public void Create_WithNullQueueName_ShouldThrow()
    {
        var act = () => MqMessageContract.Create(null!, "MQFMT_STRING", CreateSystemId());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyMessageFormat_ShouldThrow()
    {
        var act = () => MqMessageContract.Create("QUEUE.TEST", "", CreateSystemId());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullSystemId_ShouldThrow()
    {
        var act = () => MqMessageContract.Create("QUEUE.TEST", "MQFMT_STRING", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetCopybookReference_ShouldSetReferenceAndUpdateDate()
    {
        var contract = MqMessageContract.Create("QUEUE.TEST", "MQFMT_STRING", CreateSystemId());
        var copybookId = CopybookId.New();

        contract.SetCopybookReference(copybookId);

        contract.CopybookReference.Should().Be(copybookId);
        contract.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDetails_ShouldSetDetailsAndUpdateDate()
    {
        var contract = MqMessageContract.Create("QUEUE.TEST", "MQFMT_STRING", CreateSystemId());

        contract.UpdateDetails("schema.json", 4096, "MQHRF2", "EBCDIC");

        contract.PayloadSchema.Should().Be("schema.json");
        contract.MaxMessageLength.Should().Be(4096);
        contract.HeaderFormat.Should().Be("MQHRF2");
        contract.EncodingScheme.Should().Be("EBCDIC");
        contract.UpdatedAt.Should().NotBeNull();
    }
}
