using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

/// <summary>
/// Fake simples de IDistributedCache para testes — evita problemas com extension methods e NSubstitute.
/// </summary>
public sealed class FakeDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _store = new();

    public byte[]? Get(string key) => _store.TryGetValue(key, out var value) ? value : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => _store[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => _store.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}

public sealed class DistributedPromptCacheServiceTests
{
    private readonly FakeDistributedCache _cache = new();
    private readonly DistributedPromptCacheService _sut;

    public DistributedPromptCacheServiceTests()
    {
        _sut = new DistributedPromptCacheService(_cache, NullLogger<DistributedPromptCacheService>.Instance);
    }

    [Fact]
    public async Task GetCachedResponse_ShouldReturnNull_WhenNotCached()
    {
        var result = await _sut.GetCachedResponseAsync("nonexistent-hash", "gpt-4o");
        result.Should().BeNull();
    }

    [Fact]
    public async Task CacheAndRetrieve_ShouldRoundTrip()
    {
        var hash = _sut.ComputePromptHash("What is the status of service X?", "gpt-4o");
        var expectedResponse = "Service X is healthy.";

        await _sut.CacheResponseAsync(hash, "gpt-4o", expectedResponse);
        var result = await _sut.GetCachedResponseAsync(hash, "gpt-4o");

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public void ComputePromptHash_ShouldBeDeterministic()
    {
        var hash1 = _sut.ComputePromptHash("Hello world", "gpt-4o", 0.7, 1024);
        var hash2 = _sut.ComputePromptHash("Hello world", "gpt-4o", 0.7, 1024);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputePromptHash_ShouldDiffer_OnDifferentInputs()
    {
        var hash1 = _sut.ComputePromptHash("Hello world", "gpt-4o");
        var hash2 = _sut.ComputePromptHash("Hello world", "claude-3-5-sonnet");
        var hash3 = _sut.ComputePromptHash("Hello world!", "gpt-4o");

        hash1.Should().NotBe(hash2);
        hash1.Should().NotBe(hash3);
    }

    [Fact]
    public void ComputePromptHash_ShouldNormalizeWhitespaceAndCase()
    {
        var hash1 = _sut.ComputePromptHash("  Hello World  ", "gpt-4o");
        var hash2 = _sut.ComputePromptHash("hello world", "gpt-4o");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task CacheResponse_ShouldStoreInCache()
    {
        var hash = _sut.ComputePromptHash("TTL test", "gpt-4o");
        await _sut.CacheResponseAsync(hash, "gpt-4o", "response", TimeSpan.FromMinutes(5));

        var cached = await _sut.GetCachedResponseAsync(hash, "gpt-4o");
        cached.Should().Be("response");
    }
}
