using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Tests.Domain.Services;

/// <summary>
/// Testes unitários para <see cref="AsyncApiDiffCalculator"/>.
/// Valida a detecção de mudanças breaking, aditivas e non-breaking entre
/// especificações AsyncAPI, incluindo canais, operações e schemas de mensagem.
/// </summary>
public sealed class AsyncApiDiffCalculatorTests
{
    private const string BaseSpec = """
        {
          "asyncapi": "2.6.0",
          "channels": {
            "user/signedup": {
              "publish": {
                "message": {
                  "payload": {
                    "type": "object",
                    "required": ["userId"],
                    "properties": {
                      "userId": { "type": "string" },
                      "email": { "type": "string" }
                    }
                  }
                }
              },
              "subscribe": {}
            },
            "order/created": {
              "publish": {}
            }
          }
        }
        """;

    [Fact]
    public void ComputeDiff_Should_DetectRemovedChannel_When_ChannelMissingInTarget()
    {
        // Arrange
        var targetSpec = """
            {
              "asyncapi": "2.6.0",
              "channels": {
                "user/signedup": {
                  "publish": {
                    "message": {
                      "payload": {
                        "type": "object",
                        "required": ["userId"],
                        "properties": {
                          "userId": { "type": "string" },
                          "email": { "type": "string" }
                        }
                      }
                    }
                  },
                  "subscribe": {}
                }
              }
            }
            """;

        // Act
        var result = AsyncApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "ChannelRemoved" && c.Path == "order/created");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectAddedChannel_When_ChannelMissingInBase()
    {
        // Arrange
        var targetSpec = """
            {
              "asyncapi": "2.6.0",
              "channels": {
                "user/signedup": {
                  "publish": {
                    "message": {
                      "payload": {
                        "type": "object",
                        "required": ["userId"],
                        "properties": {
                          "userId": { "type": "string" },
                          "email": { "type": "string" }
                        }
                      }
                    }
                  },
                  "subscribe": {}
                },
                "order/created": {
                  "publish": {}
                },
                "payment/processed": {
                  "subscribe": {}
                }
              }
            }
            """;

        // Act
        var result = AsyncApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.AdditiveChanges.Should().ContainSingle(c => c.ChangeType == "ChannelAdded" && c.Path == "payment/processed");
        result.ChangeLevel.Should().Be(ChangeLevel.Additive);
    }

    [Fact]
    public void ComputeDiff_Should_DetectRemovedOperation_When_OperationMissingInTarget()
    {
        // Arrange — remove subscribe de user/signedup
        var targetSpec = """
            {
              "asyncapi": "2.6.0",
              "channels": {
                "user/signedup": {
                  "publish": {
                    "message": {
                      "payload": {
                        "type": "object",
                        "required": ["userId"],
                        "properties": {
                          "userId": { "type": "string" },
                          "email": { "type": "string" }
                        }
                      }
                    }
                  }
                },
                "order/created": {
                  "publish": {}
                }
              }
            }
            """;

        // Act
        var result = AsyncApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().ContainSingle(c => c.ChangeType == "OperationRemoved" && c.Method == "SUBSCRIBE");
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_ReturnNonBreaking_When_SpecsAreIdentical()
    {
        // Act
        var result = AsyncApiDiffCalculator.ComputeDiff(BaseSpec, BaseSpec);

        // Assert
        result.BreakingChanges.Should().BeEmpty();
        result.AdditiveChanges.Should().BeEmpty();
        result.ChangeLevel.Should().Be(ChangeLevel.NonBreaking);
    }

    [Fact]
    public void ComputeDiff_Should_DetectRequiredFieldAdded_When_NewRequiredFieldInMessage()
    {
        // Arrange — adiciona campo obrigatório "tenantId" em user/signedup publish
        var targetSpec = """
            {
              "asyncapi": "2.6.0",
              "channels": {
                "user/signedup": {
                  "publish": {
                    "message": {
                      "payload": {
                        "type": "object",
                        "required": ["userId", "tenantId"],
                        "properties": {
                          "userId": { "type": "string" },
                          "email": { "type": "string" },
                          "tenantId": { "type": "string" }
                        }
                      }
                    }
                  },
                  "subscribe": {}
                },
                "order/created": {
                  "publish": {}
                }
              }
            }
            """;

        // Act
        var result = AsyncApiDiffCalculator.ComputeDiff(BaseSpec, targetSpec);

        // Assert
        result.BreakingChanges.Should().Contain(c => c.ChangeType == "FieldRequired" && c.Description.Contains("tenantId"));
        result.ChangeLevel.Should().Be(ChangeLevel.Breaking);
    }

    [Fact]
    public void ComputeDiff_Should_HandleMalformedJson_Gracefully()
    {
        // Act
        var result = AsyncApiDiffCalculator.ComputeDiff("{ invalid }", BaseSpec);

        // Assert
        result.Should().NotBeNull();
        result.AdditiveChanges.Should().NotBeEmpty();
    }
}
