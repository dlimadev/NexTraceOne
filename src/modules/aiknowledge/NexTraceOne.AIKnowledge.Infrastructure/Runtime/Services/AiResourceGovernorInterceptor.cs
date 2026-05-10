using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Decorador de IChatCompletionProvider que aplica controlo de recursos:
/// - SemaphoreSlim: impede sobrecarga de inferências concorrentes.
/// - Circuit Breaker: bloqueia inferências após falhas consecutivas.
///
/// W4-03: AI Resource Governor.
/// </summary>
public sealed class AiResourceGovernorInterceptor : IChatCompletionProvider
{
    private readonly IChatCompletionProvider _inner;
    private readonly AiResourceGovernor _governor;
    private readonly ILogger<AiResourceGovernorInterceptor> _logger;

    public AiResourceGovernorInterceptor(
        IChatCompletionProvider inner,
        AiResourceGovernor governor,
        ILogger<AiResourceGovernorInterceptor> logger)
    {
        _inner = inner;
        _governor = governor;
        _logger = logger;
    }

    public string ProviderId => _inner.ProviderId;

    public async Task<ChatCompletionResult> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        _governor.CheckCircuit();

        using var slot = await _governor.AcquireSlotAsync(cancellationToken);

        try
        {
            var result = await _inner.CompleteAsync(request, cancellationToken);

            if (result.Success)
                _governor.RecordSuccess();
            else
                _governor.RecordFailure();

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _governor.RecordFailure();
            _logger.LogWarning(ex,
                "AiResourceGovernorInterceptor: exceção na inferência '{Provider}'.",
                ProviderId);
            throw;
        }
    }

    public async IAsyncEnumerable<ChatStreamChunk> CompleteStreamingAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _governor.CheckCircuit();

        using var slot = await _governor.AcquireSlotAsync(cancellationToken);
        var failed = false;

        IAsyncEnumerator<ChatStreamChunk>? enumerator = null;
        try
        {
            enumerator = _inner.CompleteStreamingAsync(request, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);

            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed = true;
                    _governor.RecordFailure();
                    _logger.LogWarning(ex,
                        "AiResourceGovernorInterceptor: exceção no streaming '{Provider}'.",
                        ProviderId);
                    throw;
                }

                if (!hasNext) break;

                var chunk = enumerator.Current;
                yield return chunk;

                if (chunk.IsComplete)
                    break;
            }

            if (!failed)
                _governor.RecordSuccess();
        }
        finally
        {
            if (enumerator is not null)
                await enumerator.DisposeAsync();
        }
    }

    bool IChatCompletionProvider.SupportsStreaming => _inner.SupportsStreaming;
}
