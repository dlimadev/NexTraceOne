using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade <see cref="EventDraftMetadata"/>.
/// Valida criação e atualização dos metadados AsyncAPI específicos de drafts de contrato.
/// </summary>
public sealed class EventDraftMetadataTests
{
    private static readonly ContractDraftId ValidDraftId = ContractDraftId.From(Guid.NewGuid());

    [Fact]
    public void Create_Should_Return_Valid_EventDraftMetadata()
    {
        var metadata = EventDraftMetadata.Create(
            ValidDraftId,
            title: "PaymentEvents");

        metadata.Title.Should().Be("PaymentEvents");
        metadata.AsyncApiVersion.Should().Be("2.6.0");
        metadata.DefaultContentType.Should().Be("application/json");
        metadata.ChannelsJson.Should().Be("{}");
        metadata.MessagesJson.Should().Be("{}");
    }

    [Fact]
    public void Create_Should_Accept_Custom_AsyncApiVersion()
    {
        var metadata = EventDraftMetadata.Create(
            ValidDraftId,
            title: "OrderEvents",
            asyncApiVersion: "3.0.0");

        metadata.AsyncApiVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void Create_Should_Accept_Custom_DefaultContentType()
    {
        var metadata = EventDraftMetadata.Create(
            ValidDraftId,
            title: "AvroService",
            defaultContentType: "application/avro");

        metadata.DefaultContentType.Should().Be("application/avro");
    }

    [Fact]
    public void Create_Should_Accept_Initial_ChannelsAndMessages()
    {
        var metadata = EventDraftMetadata.Create(
            ValidDraftId,
            title: "TestService",
            channelsJson: """{"payments/created":["PUBLISH"]}""",
            messagesJson: """{"PaymentCreated":["paymentId","amount"]}""");

        metadata.ChannelsJson.Should().Be("""{"payments/created":["PUBLISH"]}""");
        metadata.MessagesJson.Should().Be("""{"PaymentCreated":["paymentId","amount"]}""");
    }

    [Fact]
    public void Update_Should_Change_All_Fields()
    {
        var metadata = EventDraftMetadata.Create(
            ValidDraftId,
            title: "OldService");

        metadata.Update(
            title: "NewService",
            asyncApiVersion: "3.0.0",
            defaultContentType: "application/avro",
            channelsJson: """{"orders/placed":["PUBLISH","SUBSCRIBE"]}""",
            messagesJson: """{"OrderPlaced":["orderId"]}""");

        metadata.Title.Should().Be("NewService");
        metadata.AsyncApiVersion.Should().Be("3.0.0");
        metadata.DefaultContentType.Should().Be("application/avro");
        metadata.ChannelsJson.Should().Be("""{"orders/placed":["PUBLISH","SUBSCRIBE"]}""");
        metadata.MessagesJson.Should().Be("""{"OrderPlaced":["orderId"]}""");
    }

    [Fact]
    public void EventDraftMetadataId_New_Should_Return_UniqueId()
    {
        var id1 = EventDraftMetadataId.New();
        var id2 = EventDraftMetadataId.New();

        id1.Should().NotBe(id2);
    }
}
