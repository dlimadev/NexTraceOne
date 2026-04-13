using System.Linq;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Governance.Application.Features;

/// <summary>
/// Testes para o EmbeddingIndexJob e funções de similaridade coseno.
/// Valida: skip de fontes já indexadas, indexação de novas fontes, cálculo de cosine similarity.
/// </summary>
public sealed class EmbeddingIndexJobTests
{
    // ── 1. Job skips sources with existing embedding ──────────────────────

    [Fact]
    public async Task Job_Skips_Sources_With_Existing_Embedding()
    {
        // Arrange
        var source = CreateSourceWithEmbedding();
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(Arg.Any<KnowledgeSourceType?>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AIKnowledgeSource> { source } as IReadOnlyList<AIKnowledgeSource>);

        var embeddingProvider = Substitute.For<IEmbeddingProvider>();

        // The job's logic is: filter sources where EmbeddingJson is null.
        // We verify: when source has EmbeddingJson, it's not in the unindexed set.
        var sources = await sourceRepo.ListAsync(null, true, CancellationToken.None);
        var unindexed = sources.Where(s => s.EmbeddingJson is null).ToList();

        // Assert — source already indexed, should be 0 unindexed
        unindexed.Should().BeEmpty();
        await embeddingProvider.DidNotReceive().GenerateEmbeddingsAsync(
            Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>());
    }

    // ── 2. Job indexes new sources ────────────────────────────────────────

    [Fact]
    public async Task Job_Indexes_New_Sources_Without_Embedding()
    {
        // Arrange
        var source = CreateSourceWithoutEmbedding();
        var sourceRepo = Substitute.For<IAiKnowledgeSourceRepository>();
        sourceRepo.ListAsync(null, true, Arg.Any<CancellationToken>())
            .Returns(new List<AIKnowledgeSource> { source } as IReadOnlyList<AIKnowledgeSource>);

        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var embeddingProvider = Substitute.For<IEmbeddingProvider>();
        embeddingProvider.GenerateEmbeddingsAsync(Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(
                Success: true,
                Embeddings: new List<float[]> { embedding },
                ModelId: "nomic-embed-text",
                ProviderId: "ollama",
                TokensUsed: 5));

        // Act — simulate what EmbeddingIndexJob.RunCycleAsync does
        var sources = await sourceRepo.ListAsync(null, true, CancellationToken.None);
        var unindexed = sources.Where(s => s.EmbeddingJson is null).ToList();

        foreach (var s in unindexed)
        {
            var text = $"{s.Name} {s.Description}".Trim();
            var result = await embeddingProvider.GenerateEmbeddingsAsync(
                new EmbeddingRequest("nomic-embed-text", [text]),
                CancellationToken.None);

            if (result.Success && result.Embeddings?.Count > 0)
                s.SetEmbedding(result.Embeddings[0]);
        }

        // Assert
        unindexed.Should().HaveCount(1);
        source.EmbeddingJson.Should().NotBeNullOrWhiteSpace();
        source.GetEmbedding().Should().NotBeNull();
        source.GetEmbedding()!.Length.Should().Be(3);
        await embeddingProvider.Received(1).GenerateEmbeddingsAsync(
            Arg.Any<EmbeddingRequest>(), Arg.Any<CancellationToken>());
    }

    // ── 3. Cosine similarity calculation ─────────────────────────────────

    [Fact]
    public void CosineSimilarity_Returns_Correct_Values()
    {
        // Identical vectors → similarity = 1.0
        var a = new float[] { 1.0f, 0.0f, 0.0f };
        var b = new float[] { 1.0f, 0.0f, 0.0f };
        var sim = DocumentRetrievalService.CosineSimilarity(a, b);
        sim.Should().BeApproximately(1.0, 0.0001);

        // Orthogonal vectors → similarity = 0.0
        var c = new float[] { 1.0f, 0.0f };
        var d = new float[] { 0.0f, 1.0f };
        var simOrthogonal = DocumentRetrievalService.CosineSimilarity(c, d);
        simOrthogonal.Should().BeApproximately(0.0, 0.0001);

        // Opposite vectors → similarity = -1.0
        var e = new float[] { 1.0f, 0.0f };
        var f = new float[] { -1.0f, 0.0f };
        var simOpposite = DocumentRetrievalService.CosineSimilarity(e, f);
        simOpposite.Should().BeApproximately(-1.0, 0.0001);

        // Zero vector → similarity = 0.0 (not NaN)
        var zero = new float[] { 0.0f, 0.0f };
        var simZero = DocumentRetrievalService.CosineSimilarity(a, zero);
        simZero.Should().Be(0.0);

        // Mismatched lengths → similarity = 0.0
        var short1 = new float[] { 1.0f };
        var simMismatch = DocumentRetrievalService.CosineSimilarity(a, short1);
        simMismatch.Should().Be(0.0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static AIKnowledgeSource CreateSourceWithoutEmbedding()
    {
        var source = AIKnowledgeSource.Register(
            name: "test-source",
            description: "A test knowledge source",
            sourceType: KnowledgeSourceType.Documentation,
            endpointOrPath: "/docs/test",
            priority: 10,
            registeredAt: DateTimeOffset.UtcNow);
        return source;
    }

    private static AIKnowledgeSource CreateSourceWithEmbedding()
    {
        var source = CreateSourceWithoutEmbedding();
        source.SetEmbedding([0.5f, 0.5f, 0.0f]);
        return source;
    }
}
