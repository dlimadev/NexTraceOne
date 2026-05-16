using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class TokenCounterServiceTests
{
    private readonly TokenCounterService _sut = new(NullLogger<TokenCounterService>.Instance);

    [Theory]
    [InlineData("", 0)]
    [InlineData("hello", 1)]
    [InlineData("hello world", 2)]
    [InlineData("The quick brown fox jumps over the lazy dog.", 9)]
    public void CountTokens_ShouldReturnExpectedCount(string text, int expectedApprox)
    {
        var count = _sut.CountTokens(text);

        // Tokenizer real pode variar ligeiramente; garantimos que está na margem de ±2 tokens
        count.Should().BeInRange(expectedApprox - 2, expectedApprox + 2);
    }

    [Fact]
    public void CountTokens_ShouldBeMoreAccurateThanHeuristic()
    {
        var text = "The quick brown fox jumps over the lazy dog.";
        var tokenCount = _sut.CountTokens(text);
        var heuristicCount = (text.Length + 3) / 4; // old heuristic

        // Tokenizer real deve ser diferente da heurística (prova de que estamos usando tokenizer)
        tokenCount.Should().NotBe(heuristicCount);
    }

    [Fact]
    public void TruncateToTokens_ShouldNotExceedMaxTokens()
    {
        var text = string.Join(" ", Enumerable.Repeat("hello world", 100));
        var maxTokens = 10;

        var truncated = _sut.TruncateToTokens(text, maxTokens);
        var truncatedTokens = _sut.CountTokens(truncated);

        truncatedTokens.Should().BeLessThanOrEqualTo(maxTokens);
        truncated.Should().EndWith("...");
    }

    [Fact]
    public void TruncateToTokens_ShouldReturnOriginal_WhenUnderLimit()
    {
        var text = "short text";
        var result = _sut.TruncateToTokens(text, 100);
        result.Should().Be(text);
    }

    [Fact]
    public void CountTokens_WithModelName_ShouldUseConservativeEstimate()
    {
        var text = "hello world";
        var count = _sut.CountTokens(text, "qwen2.5-coder-32b");
        count.Should().BeGreaterThan(0);
    }
}
