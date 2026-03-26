using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade <see cref="EventContractDetail"/>.
/// Valida criação, métodos de domínio e invariantes da entidade AsyncAPI-específica.
/// </summary>
public sealed class EventContractDetailTests
{
    private static readonly ContractVersionId ValidVersionId = ContractVersionId.From(Guid.NewGuid());

    [Fact]
    public void Create_Should_Return_Valid_EventContractDetail()
    {
        var result = EventContractDetail.Create(
            ValidVersionId,
            title: "UserEventService",
            asyncApiVersion: "2.6.0",
            channelsJson: """{"user/signedup":["PUBLISH"]}""",
            messagesJson: """{"UserSignedUp":["userId","email"]}""",
            serversJson: """{"production":"kafka.example.com:9092"}""");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("UserEventService");
        result.Value.AsyncApiVersion.Should().Be("2.6.0");
        result.Value.DefaultContentType.Should().Be("application/json");
        result.Value.ChannelsJson.Should().Be("""{"user/signedup":["PUBLISH"]}""");
        result.Value.MessagesJson.Should().Be("""{"UserSignedUp":["userId","email"]}""");
        result.Value.ServersJson.Should().Be("""{"production":"kafka.example.com:9092"}""");
    }

    [Fact]
    public void Create_Should_Accept_AsyncApi3()
    {
        var result = EventContractDetail.Create(
            ValidVersionId,
            title: "OrderEvents",
            asyncApiVersion: "3.0.0",
            channelsJson: "{}",
            messagesJson: "{}",
            serversJson: "{}");

        result.IsSuccess.Should().BeTrue();
        result.Value.AsyncApiVersion.Should().Be("3.0.0");
    }

    [Fact]
    public void Create_Should_Accept_Custom_DefaultContentType()
    {
        var result = EventContractDetail.Create(
            ValidVersionId,
            title: "AvroEvents",
            asyncApiVersion: "2.6.0",
            channelsJson: "{}",
            messagesJson: "{}",
            serversJson: "{}",
            defaultContentType: "application/avro");

        result.IsSuccess.Should().BeTrue();
        result.Value.DefaultContentType.Should().Be("application/avro");
    }

    [Fact]
    public void UpdateFromParsing_Should_Update_AllFields()
    {
        var detail = EventContractDetail.Create(
            ValidVersionId,
            title: "OldService",
            asyncApiVersion: "2.6.0",
            channelsJson: "{}",
            messagesJson: "{}",
            serversJson: "{}").Value;

        detail.UpdateFromParsing(
            "NewService",
            "3.0.0",
            """{"new/channel":["PUBLISH"]}""",
            """{"NewMessage":["id"]}""",
            """{"prod":"kafka.new.com"}""",
            "application/protobuf");

        detail.Title.Should().Be("NewService");
        detail.AsyncApiVersion.Should().Be("3.0.0");
        detail.ChannelsJson.Should().Be("""{"new/channel":["PUBLISH"]}""");
        detail.MessagesJson.Should().Be("""{"NewMessage":["id"]}""");
        detail.ServersJson.Should().Be("""{"prod":"kafka.new.com"}""");
        detail.DefaultContentType.Should().Be("application/protobuf");
    }

    [Fact]
    public void EventContractDetailId_New_Should_Return_UniqueId()
    {
        var id1 = EventContractDetailId.New();
        var id2 = EventContractDetailId.New();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void EventContractDetailId_From_Should_Roundtrip()
    {
        var guid = Guid.NewGuid();
        var id = EventContractDetailId.From(guid);

        id.Value.Should().Be(guid);
    }
}
