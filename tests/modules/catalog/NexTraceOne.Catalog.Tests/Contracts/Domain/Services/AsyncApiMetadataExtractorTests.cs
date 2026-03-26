using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="AsyncApiMetadataExtractor"/>.
/// Valida a extração correta de metadados AsyncAPI: título, versão, defaultContentType,
/// channels com operações, mensagens/schemas e servidores/brokers.
/// </summary>
public sealed class AsyncApiMetadataExtractorTests
{
    private const string ValidAsyncApi26 = """
        {
          "asyncapi": "2.6.0",
          "info": {
            "title": "UserEventService",
            "version": "1.0.0"
          },
          "defaultContentType": "application/json",
          "servers": {
            "production": {
              "url": "kafka.example.com:9092",
              "protocol": "kafka"
            }
          },
          "channels": {
            "user/signedup": {
              "publish": {
                "message": {
                  "payload": {
                    "type": "object",
                    "properties": {
                      "userId": { "type": "string" },
                      "email": { "type": "string" }
                    }
                  }
                }
              }
            },
            "user/deleted": {
              "subscribe": {}
            }
          },
          "components": {
            "messages": {
              "UserSignedUp": {
                "payload": {
                  "type": "object",
                  "properties": {
                    "userId": { "type": "string" },
                    "email": { "type": "string" },
                    "signedUpAt": { "type": "string" }
                  }
                }
              }
            }
          }
        }
        """;

    [Fact]
    public void Extract_Should_Return_Title_From_Info()
    {
        var result = AsyncApiMetadataExtractor.Extract(ValidAsyncApi26);

        result.Title.Should().Be("UserEventService");
    }

    [Fact]
    public void Extract_Should_Return_AsyncApiVersion()
    {
        var result = AsyncApiMetadataExtractor.Extract(ValidAsyncApi26);

        result.AsyncApiVersion.Should().Be("2.6.0");
    }

    [Fact]
    public void Extract_Should_Return_DefaultContentType()
    {
        var result = AsyncApiMetadataExtractor.Extract(ValidAsyncApi26);

        result.DefaultContentType.Should().Be("application/json");
    }

    [Fact]
    public void Extract_Should_Serialize_Channels_With_Operations()
    {
        var result = AsyncApiMetadataExtractor.Extract(ValidAsyncApi26);

        result.ChannelsJson.Should().Contain("user/signedup");
        result.ChannelsJson.Should().Contain("user/deleted");
        result.ChannelsJson.Should().Contain("PUBLISH");
        result.ChannelsJson.Should().Contain("SUBSCRIBE");
    }

    [Fact]
    public void Extract_Should_Serialize_Messages_From_Components()
    {
        var result = AsyncApiMetadataExtractor.Extract(ValidAsyncApi26);

        result.MessagesJson.Should().Contain("UserSignedUp");
        result.MessagesJson.Should().Contain("userId");
        result.MessagesJson.Should().Contain("email");
    }

    [Fact]
    public void Extract_Should_Serialize_Servers()
    {
        var result = AsyncApiMetadataExtractor.Extract(ValidAsyncApi26);

        result.ServersJson.Should().Contain("production");
        result.ServersJson.Should().Contain("kafka.example.com:9092");
    }

    [Fact]
    public void Extract_Should_Return_Defaults_For_Empty_Content()
    {
        var result = AsyncApiMetadataExtractor.Extract(string.Empty, "FallbackTitle");

        result.Title.Should().Be("FallbackTitle");
        result.AsyncApiVersion.Should().Be("2.6.0");
        result.DefaultContentType.Should().Be("application/json");
        result.ChannelsJson.Should().Be("{}");
        result.MessagesJson.Should().Be("{}");
        result.ServersJson.Should().Be("{}");
    }

    [Fact]
    public void Extract_Should_Return_Defaults_For_Invalid_Json()
    {
        var result = AsyncApiMetadataExtractor.Extract("not-json-at-all", "MyService");

        result.Title.Should().Be("MyService");
        result.AsyncApiVersion.Should().Be("2.6.0");
        result.ChannelsJson.Should().Be("{}");
    }

    [Fact]
    public void Extract_Should_Return_Fallback_Title_When_Info_Missing()
    {
        const string noInfo = """{"asyncapi": "2.6.0"}""";

        var result = AsyncApiMetadataExtractor.Extract(noInfo, "Fallback");

        result.Title.Should().Be("Fallback");
        result.AsyncApiVersion.Should().Be("2.6.0");
    }

    [Fact]
    public void Extract_Should_Use_Default_AsyncApiVersion_When_Field_Missing()
    {
        const string noVersion = """{"info":{"title":"Svc"}}""";

        var result = AsyncApiMetadataExtractor.Extract(noVersion, "Svc");

        result.AsyncApiVersion.Should().Be("2.6.0");
    }

    [Fact]
    public void Extract_Should_Return_Empty_Servers_When_Missing()
    {
        const string noServers = """{"asyncapi":"2.6.0","info":{"title":"Svc"}}""";

        var result = AsyncApiMetadataExtractor.Extract(noServers);

        result.ServersJson.Should().Be("{}");
    }
}
