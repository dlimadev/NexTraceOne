using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.VectorStore;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class RagGroundingServiceTests
{
    private readonly IEmbeddingProvider _embeddingProvider = Substitute.For<IEmbeddingProvider>();
    private readonly IVectorStoreRepository _vectorStore = Substitute.For<IVectorStoreRepository>();
    private readonly ILogger<RagGroundingService> _logger = Substitute.For<ILogger<RagGroundingService>>();

    private RagGroundingService CreateService()
        => new(new[] { _embeddingProvider }, _vectorStore, _logger);

    [Fact]
    public async Task GetGroundingContextAsync_EmptyQuery_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.GetGroundingContextAsync("");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGroundingContextAsync_NoEmbeddingProvider_ReturnsNull()
    {
        var service = new RagGroundingService(
            Array.Empty<IEmbeddingProvider>(),
            _vectorStore,
            _logger);

        var result = await service.GetGroundingContextAsync("test query");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGroundingContextAsync_EmbeddingFails_ReturnsNull()
    {
        _embeddingProvider.ProviderId.Returns("ollama");
        _embeddingProvider.GenerateEmbeddingsAsync(Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(false, null, "model", "ollama", 0, "timeout"));

        var service = CreateService();
        var result = await service.GetGroundingContextAsync("query");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGroundingContextAsync_NoVectorResults_ReturnsNull()
    {
        _embeddingProvider.ProviderId.Returns("ollama");
        _embeddingProvider.GenerateEmbeddingsAsync(Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(true, new[] { new[] { 0.1f, 0.2f, 0.3f } }, "model", "ollama", 3));

        _vectorStore.SearchAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VectorSearchResult>());

        var service = CreateService();
        var result = await service.GetGroundingContextAsync("query");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGroundingContextAsync_WithResults_ReturnsConcatenatedContext()
    {
        _embeddingProvider.ProviderId.Returns("ollama");
        _embeddingProvider.GenerateEmbeddingsAsync(Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(true, new[] { new[] { 0.1f, 0.2f, 0.3f } }, "model", "ollama", 3));

        _vectorStore.SearchAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new VectorSearchResult(Guid.NewGuid(), 0.95f, new Dictionary<string, object> { ["content"] = "First relevant doc" }),
                new VectorSearchResult(Guid.NewGuid(), 0.88f, new Dictionary<string, object> { ["content"] = "Second relevant doc" })
            });

        var service = CreateService();
        var result = await service.GetGroundingContextAsync("query");

        result.Should().NotBeNull();
        result.Should().Contain("First relevant doc");
        result.Should().Contain("Second relevant doc");
    }

    [Fact]
    public async Task GetGroundingContextAsync_UsesSnippetFallback_WhenContentMissing()
    {
        _embeddingProvider.ProviderId.Returns("ollama");
        _embeddingProvider.GenerateEmbeddingsAsync(Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(true, new[] { new[] { 0.1f, 0.2f, 0.3f } }, "model", "ollama", 3));

        _vectorStore.SearchAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new VectorSearchResult(Guid.NewGuid(), 0.95f, new Dictionary<string, object> { ["snippet"] = "Snippet text" })
            });

        var service = CreateService();
        var result = await service.GetGroundingContextAsync("query");

        result.Should().Contain("Snippet text");
    }

    [Fact]
    public async Task GetGroundingContextAsync_VectorStoreThrows_ReturnsNull()
    {
        _embeddingProvider.ProviderId.Returns("ollama");
        _embeddingProvider.GenerateEmbeddingsAsync(Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(true, new[] { new[] { 0.1f, 0.2f, 0.3f } }, "model", "ollama", 3));

        _vectorStore.SearchAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<VectorSearchResult>>>(_ => throw new InvalidOperationException("qdrant down"));

        var service = CreateService();
        var result = await service.GetGroundingContextAsync("query");

        result.Should().BeNull();
    }
}
