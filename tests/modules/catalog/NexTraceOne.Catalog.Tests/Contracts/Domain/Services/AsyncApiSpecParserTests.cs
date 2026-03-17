using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Tests.Contracts.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="AsyncApiSpecParser"/>.
/// Valida a extração de canais, operações e schemas de mensagem de especificações AsyncAPI,
/// garantindo que o parsing JSON está correto e resiliente a specs malformadas.
/// </summary>
public sealed class AsyncApiSpecParserTests
{
    private const string ValidSpec = """
        {
          "asyncapi": "2.6.0",
          "channels": {
            "user/signedup": {
              "publish": {
                "message": {
                  "payload": {
                    "type": "object",
                    "required": ["userId", "email"],
                    "properties": {
                      "userId": { "type": "string" },
                      "email": { "type": "string" },
                      "name": { "type": "string" }
                    }
                  }
                }
              },
              "subscribe": {
                "message": {
                  "payload": {
                    "type": "object",
                    "properties": {
                      "status": { "type": "string" }
                    }
                  }
                }
              }
            },
            "order/created": {
              "publish": {
                "message": {
                  "payload": {
                    "type": "object",
                    "required": ["orderId"],
                    "properties": {
                      "orderId": { "type": "string" },
                      "total": { "type": "number" }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    #region ExtractChannelsAndOperations

    [Fact]
    public void ExtractChannelsAndOperations_Should_ReturnAllChannels_When_ValidSpec()
    {
        // Act
        var channels = AsyncApiSpecParser.ExtractChannelsAndOperations(ValidSpec);

        // Assert
        channels.Should().HaveCount(2);
        channels.Should().ContainKey("user/signedup");
        channels.Should().ContainKey("order/created");
    }

    [Fact]
    public void ExtractChannelsAndOperations_Should_ReturnCorrectOperations_When_MultipleOperations()
    {
        // Act
        var channels = AsyncApiSpecParser.ExtractChannelsAndOperations(ValidSpec);

        // Assert — user/signedup tem publish e subscribe
        channels["user/signedup"].Should().HaveCount(2);
        channels["user/signedup"].Should().Contain("PUBLISH").And.Contain("SUBSCRIBE");
        // order/created tem apenas publish
        channels["order/created"].Should().ContainSingle().Which.Should().Be("PUBLISH");
    }

    [Fact]
    public void ExtractChannelsAndOperations_Should_ReturnEmptyDictionary_When_JsonMalformed()
    {
        // Act
        var channels = AsyncApiSpecParser.ExtractChannelsAndOperations("{ not valid }}}");

        // Assert
        channels.Should().BeEmpty();
    }

    [Fact]
    public void ExtractChannelsAndOperations_Should_ReturnEmptyDictionary_When_NoChannelsProperty()
    {
        // Arrange
        var specWithoutChannels = """{ "asyncapi": "2.6.0", "info": { "title": "Test" } }""";

        // Act
        var channels = AsyncApiSpecParser.ExtractChannelsAndOperations(specWithoutChannels);

        // Assert
        channels.Should().BeEmpty();
    }

    [Fact]
    public void ExtractChannelsAndOperations_Should_FilterNonOperationProperties()
    {
        // Arrange — canal com propriedade que não é operação válida
        var specWithExtra = """
            {
              "asyncapi": "2.6.0",
              "channels": {
                "test/channel": {
                  "publish": {},
                  "description": "some channel",
                  "x-custom": true
                }
              }
            }
            """;

        // Act
        var channels = AsyncApiSpecParser.ExtractChannelsAndOperations(specWithExtra);

        // Assert — apenas publish deve ser extraído
        channels["test/channel"].Should().ContainSingle().Which.Should().Be("PUBLISH");
    }

    #endregion

    #region ExtractMessageSchema

    [Fact]
    public void ExtractMessageSchema_Should_ReturnFields_When_ValidChannelAndOperation()
    {
        // Act
        var schema = AsyncApiSpecParser.ExtractMessageSchema(ValidSpec, "user/signedup", "publish");

        // Assert
        schema.Should().HaveCount(3);
        schema.Should().ContainKey("userId").WhoseValue.Should().BeTrue();
        schema.Should().ContainKey("email").WhoseValue.Should().BeTrue();
        schema.Should().ContainKey("name").WhoseValue.Should().BeFalse();
    }

    [Fact]
    public void ExtractMessageSchema_Should_MarkOptional_When_FieldNotInRequired()
    {
        // Act — subscribe de user/signedup não tem required array
        var schema = AsyncApiSpecParser.ExtractMessageSchema(ValidSpec, "user/signedup", "subscribe");

        // Assert — status não é obrigatório
        schema.Should().ContainSingle();
        schema.Should().ContainKey("status").WhoseValue.Should().BeFalse();
    }

    [Fact]
    public void ExtractMessageSchema_Should_ReturnEmpty_When_ChannelNotFound()
    {
        // Act
        var schema = AsyncApiSpecParser.ExtractMessageSchema(ValidSpec, "nonexistent/channel", "publish");

        // Assert
        schema.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMessageSchema_Should_ReturnEmpty_When_OperationNotFound()
    {
        // Act
        var schema = AsyncApiSpecParser.ExtractMessageSchema(ValidSpec, "order/created", "subscribe");

        // Assert
        schema.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMessageSchema_Should_ReturnEmpty_When_JsonMalformed()
    {
        // Act
        var schema = AsyncApiSpecParser.ExtractMessageSchema("{ invalid }", "user/signedup", "publish");

        // Assert
        schema.Should().BeEmpty();
    }

    [Fact]
    public void ExtractMessageSchema_Should_ReturnEmpty_When_NoPayloadProperties()
    {
        // Arrange — operação sem payload
        var specNoPayload = """
            {
              "channels": {
                "test/channel": {
                  "publish": {
                    "message": {}
                  }
                }
              }
            }
            """;

        // Act
        var schema = AsyncApiSpecParser.ExtractMessageSchema(specNoPayload, "test/channel", "publish");

        // Assert
        schema.Should().BeEmpty();
    }

    #endregion
}
