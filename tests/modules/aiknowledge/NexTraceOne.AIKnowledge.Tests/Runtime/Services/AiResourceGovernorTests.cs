using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

namespace NexTraceOne.AIKnowledge.Tests.Runtime.Services;

public sealed class AiResourceGovernorTests : IDisposable
{
    private readonly AiResourceGovernor _governor;

    public AiResourceGovernorTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AIGovernor:MaxConcurrency"] = "3",
                ["AIGovernor:QueueCapacity"] = "10",
                ["AIGovernor:CircuitBreakerThreshold"] = "3",
                ["AIGovernor:CircuitBreakerCooldownSeconds"] = "60",
            })
            .Build();

        _governor = new AiResourceGovernor(config, NullLogger<AiResourceGovernor>.Instance);
    }

    public void Dispose() => _governor.Dispose();

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void InitialState_IsClosed()
    {
        _governor.State.Should().Be(AiResourceGovernor.CircuitState.Closed);
    }

    // ── Circuit Breaker ───────────────────────────────────────────────────────

    [Fact]
    public void RecordFailure_BelowThreshold_StaysClosed()
    {
        _governor.RecordFailure();
        _governor.RecordFailure();

        _governor.State.Should().Be(AiResourceGovernor.CircuitState.Closed);
    }

    [Fact]
    public void RecordFailure_AtThreshold_OpensCircuit()
    {
        _governor.RecordFailure();
        _governor.RecordFailure();
        _governor.RecordFailure();

        _governor.State.Should().Be(AiResourceGovernor.CircuitState.Open);
    }

    [Fact]
    public void CheckCircuit_WhenOpen_ThrowsInvalidOperationException()
    {
        _governor.RecordFailure();
        _governor.RecordFailure();
        _governor.RecordFailure();

        var act = () => _governor.CheckCircuit();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*circuit breaker*");
    }

    [Fact]
    public void RecordSuccess_AfterFailures_ClosesCircuit()
    {
        _governor.RecordFailure();
        _governor.RecordFailure();
        _governor.RecordSuccess();

        _governor.State.Should().Be(AiResourceGovernor.CircuitState.Closed);
    }

    // ── Semaphore ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AcquireSlotAsync_WithinConcurrencyLimit_Succeeds()
    {
        var slots = new List<IDisposable>();
        for (var i = 0; i < 3; i++)
            slots.Add(await _governor.AcquireSlotAsync(CancellationToken.None));

        slots.Should().HaveCount(3);
        slots.ForEach(s => s.Dispose());
    }

    [Fact]
    public async Task AcquireSlotAsync_WhenCircuitOpen_ThrowsBeforeWaiting()
    {
        _governor.RecordFailure();
        _governor.RecordFailure();
        _governor.RecordFailure();

        var act = async () => await _governor.AcquireSlotAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Interceptor ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Interceptor_WhenInnerSucceeds_RecordsSuccess()
    {
        var inner = Substitute.For<IChatCompletionProvider>();
        inner.ProviderId.Returns("test");
        inner.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatCompletionResult(true, "Hello", "model-1", "test", 10, 20, TimeSpan.FromMilliseconds(100)));

        var interceptor = new AiResourceGovernorInterceptor(
            inner, _governor, NullLogger<AiResourceGovernorInterceptor>.Instance);

        await interceptor.CompleteAsync(
            new ChatCompletionRequest("model-1", [new ChatMessage("user", "Hi")]),
            CancellationToken.None);

        _governor.State.Should().Be(AiResourceGovernor.CircuitState.Closed);
    }

    [Fact]
    public async Task Interceptor_WhenInnerThrows_RecordsFailure()
    {
        var inner = Substitute.For<IChatCompletionProvider>();
        inner.ProviderId.Returns("test");
        inner.CompleteAsync(Arg.Any<ChatCompletionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatCompletionResult>(new HttpRequestException("connection refused")));

        var interceptor = new AiResourceGovernorInterceptor(
            inner, _governor, NullLogger<AiResourceGovernorInterceptor>.Instance);

        var act = () => interceptor.CompleteAsync(
            new ChatCompletionRequest("model-1", [new ChatMessage("user", "Hi")]),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();

        // 1 failure recorded but circuit still closed (threshold is 3)
        _governor.State.Should().Be(AiResourceGovernor.CircuitState.Closed);
    }
}
