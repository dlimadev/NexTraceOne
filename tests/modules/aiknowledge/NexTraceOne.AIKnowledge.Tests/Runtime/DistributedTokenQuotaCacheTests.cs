using System.Text;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NexTraceOne.AIKnowledge.Tests.Runtime;

public sealed class DistributedTokenQuotaCacheTests
{
    private readonly IDistributedCache _cache = Substitute.For<IDistributedCache>();

    private DistributedTokenQuotaCache CreateCache() =>
        new(_cache, NullLogger<DistributedTokenQuotaCache>.Instance);

    // ── GetUsageAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsageAsync_WhenCacheHit_ReturnsValue()
    {
        var bytes = Encoding.UTF8.GetBytes("12345");
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(bytes);

        var sut = CreateCache();
        var result = await sut.GetUsageAsync("user1", "daily");

        result.Should().Be(12345L);
    }

    [Fact]
    public async Task GetUsageAsync_WhenCacheMiss_ReturnsNull()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var sut = CreateCache();
        var result = await sut.GetUsageAsync("user1", "daily");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUsageAsync_WhenCacheThrows_ReturnsnullSilently()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Redis connection refused"));

        var sut = CreateCache();
        var result = await sut.GetUsageAsync("user1", "daily");

        result.Should().BeNull("exceções no cache devem ser silenciadas — fail-open");
    }

    [Fact]
    public async Task GetUsageAsync_KeyFormat_ContainsUserAndGranularity()
    {
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);

        var sut = CreateCache();
        await sut.GetUsageAsync("user-abc", "monthly");

        await _cache.Received(1).GetAsync(
            Arg.Is<string>(k => k.Contains("user-abc") && k.Contains("monthly")),
            Arg.Any<CancellationToken>());
    }

    // ── SetUsageAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SetUsageAsync_StoresEncodedValue()
    {
        var sut = CreateCache();
        await sut.SetUsageAsync("user1", "daily", 5000L, TimeSpan.FromMinutes(1));

        await _cache.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("user1") && k.Contains("daily")),
            Arg.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "5000"),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetUsageAsync_WhenCacheThrows_SilentlyIgnores()
    {
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Redis timeout"));

        var sut = CreateCache();

        // Não deve lançar exceção
        await sut.SetUsageAsync("user1", "daily", 100L, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SetUsageAsync_UsesTtlFromCaller()
    {
        var sut = CreateCache();
        var ttl = TimeSpan.FromMinutes(5);
        await sut.SetUsageAsync("user1", "daily", 100L, ttl);

        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(opts =>
                opts.AbsoluteExpirationRelativeToNow == ttl),
            Arg.Any<CancellationToken>());
    }

    // ── InvalidateUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidateUserAsync_RemovesMultipleGranularities()
    {
        var sut = CreateCache();
        await sut.InvalidateUserAsync("user1");

        await _cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains("daily") && k.Contains("user1")),
            Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(
            Arg.Is<string>(k => k.Contains("monthly") && k.Contains("user1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateUserAsync_WhenCacheThrows_ContinuesWithOtherGranularities()
    {
        var callCount = 0;
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (callCount++ == 0) throw new Exception("Redis fail");
                return Task.CompletedTask;
            });

        var sut = CreateCache();

        // Não deve lançar
        await sut.InvalidateUserAsync("user1");
    }
}
