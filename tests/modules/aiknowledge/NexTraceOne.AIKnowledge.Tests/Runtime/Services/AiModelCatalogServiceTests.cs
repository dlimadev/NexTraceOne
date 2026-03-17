using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>Testes unitários do AiModelCatalogService — resolução de modelos do catálogo.</summary>
public sealed class AiModelCatalogServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2025, 6, 1, 10, 0, 0, TimeSpan.Zero);
    private readonly IAiModelRepository _modelRepository = Substitute.For<IAiModelRepository>();

    private AIModel CreateActiveModel(string name = "llama3", string provider = "ollama")
    {
        return AIModel.Register(
            name, name, provider, ModelType.Chat,
            isInternal: true, "chat", 1, FixedNow);
    }

    // ── ResolveDefaultModelAsync ────────────────────────────────────────

    [Fact]
    public async Task ResolveDefaultModelAsync_ShouldReturnFirstActiveModelOfType()
    {
        var model = CreateActiveModel();
        _modelRepository.ListAsync(null, ModelType.Chat, ModelStatus.Active, null, Arg.Any<CancellationToken>())
            .Returns([model]);

        var service = new AiModelCatalogService(_modelRepository);
        var result = await service.ResolveDefaultModelAsync("chat", CancellationToken.None);

        result.Should().NotBeNull();
        result!.ModelName.Should().Be("llama3");
        result.IsInternal.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveDefaultModelAsync_ShouldReturnNullWhenNoModelsAvailable()
    {
        _modelRepository.ListAsync(null, ModelType.Chat, ModelStatus.Active, null, Arg.Any<CancellationToken>())
            .Returns(new List<AIModel>());

        var service = new AiModelCatalogService(_modelRepository);
        var result = await service.ResolveDefaultModelAsync("chat", CancellationToken.None);

        result.Should().BeNull();
    }

    // ── ResolveModelByIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task ResolveModelByIdAsync_ShouldReturnModelWhenFound()
    {
        var model = CreateActiveModel("gpt-4o", "openai");
        _modelRepository.GetByIdAsync(Arg.Any<AIModelId>(), Arg.Any<CancellationToken>())
            .Returns(model);

        var service = new AiModelCatalogService(_modelRepository);
        var result = await service.ResolveModelByIdAsync(model.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.ModelName.Should().Be("gpt-4o");
    }

    [Fact]
    public async Task ResolveModelByIdAsync_ShouldReturnNullWhenModelNotFound()
    {
        _modelRepository.GetByIdAsync(Arg.Any<AIModelId>(), Arg.Any<CancellationToken>())
            .Returns((AIModel?)null);

        var service = new AiModelCatalogService(_modelRepository);
        var result = await service.ResolveModelByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }
}
