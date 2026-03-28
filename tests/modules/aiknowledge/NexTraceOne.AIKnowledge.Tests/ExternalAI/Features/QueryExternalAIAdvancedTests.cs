using NexTraceOne.AIKnowledge.Application.ExternalAI.Features.QueryExternalAIAdvanced;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Tests.ExternalAI.Features;

public sealed class QueryExternalAIAdvancedTests
{
    private readonly IAiModelRepository _modelRepository = Substitute.For<IAiModelRepository>();

    [Fact]
    public async Task Handle_ShouldReturnModelsFromRegistry_WithoutFictionalNames()
    {
        var models = new List<AIModel>
        {
            AIModel.Register("gpt-4o", "GPT-4o", "OpenAI", ModelType.Chat, false, "chat,reasoning", 2, DateTimeOffset.UtcNow, contextWindow: 128_000),
            AIModel.Register("llama3.1:8b", "Llama 3.1 8B", "Ollama", ModelType.Chat, true, "chat", 2, DateTimeOffset.UtcNow, contextWindow: 32_000)
        };

        _modelRepository.ListAsync("OpenAI", null, ModelStatus.Active, null, Arg.Any<CancellationToken>())
            .Returns(new List<AIModel> { models[0] });

        var handler = new QueryExternalAIAdvanced.Handler(_modelRepository);
        var result = await handler.Handle(new QueryExternalAIAdvanced.Command(Provider: "OpenAI"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Models.Should().ContainSingle();
        result.Value.Models[0].Name.Should().Be("gpt-4o");
        result.Value.Models[0].Name.Should().NotBe("NexTrace-Internal-v1");
        result.Value.Models[0].Provider.Should().Be("OpenAI");
    }
}
