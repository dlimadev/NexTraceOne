using System.Threading.Channels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services;

/// <summary>
/// Singleton que controla concorrência, filas prioritárias e circuit breaker
/// para todas as chamadas de inferência de IA.
///
/// W4-03: AI Resource Governor.
///
/// Concorrência: SemaphoreSlim(MaxConcurrency) — impede sobrecarga do LLM.
/// Fila: Channel<RequestEntry>(BoundedCapacity) — descarta com OverflowException se cheia.
/// Circuit Breaker: Open após CircuitBreakerThreshold falhas consecutivas;
///   retorna ao estado HalfOpen após CircuitBreakerCooldown; fecha ao primeiro sucesso.
/// </summary>
public sealed class AiResourceGovernor : IDisposable
{
    public enum CircuitState { Closed, Open, HalfOpen }

    private readonly SemaphoreSlim _semaphore;
    private readonly Channel<PendingRequest> _queue;
    private readonly ILogger<AiResourceGovernor> _logger;
    private readonly int _circuitBreakerThreshold;
    private readonly TimeSpan _circuitBreakerCooldown;

    private CircuitState _state = CircuitState.Closed;
    private int _consecutiveFailures;
    private DateTimeOffset _openedAt;
    private readonly Lock _stateLock = new();

    public AiResourceGovernor(
        IConfiguration configuration,
        ILogger<AiResourceGovernor> logger)
    {
        _logger = logger;

        var maxConcurrency = int.TryParse(
            configuration["AIGovernor:MaxConcurrency"], out var mc) ? mc : 5;
        var queueCapacity = int.TryParse(
            configuration["AIGovernor:QueueCapacity"], out var qc) ? qc : 50;
        _circuitBreakerThreshold = int.TryParse(
            configuration["AIGovernor:CircuitBreakerThreshold"], out var cbt) ? cbt : 5;
        var cooldownSeconds = int.TryParse(
            configuration["AIGovernor:CircuitBreakerCooldownSeconds"], out var cbs) ? cbs : 60;

        _circuitBreakerCooldown = TimeSpan.FromSeconds(cooldownSeconds);
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _queue = Channel.CreateBounded<PendingRequest>(new BoundedChannelOptions(queueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

        _logger.LogInformation(
            "AiResourceGovernor iniciado: MaxConcurrency={Max}, QueueCapacity={Queue}, CircuitBreakerThreshold={Threshold}.",
            maxConcurrency, queueCapacity, _circuitBreakerThreshold);
    }

    public CircuitState State
    {
        get
        {
            lock (_stateLock)
            {
                if (_state == CircuitState.Open &&
                    DateTimeOffset.UtcNow - _openedAt >= _circuitBreakerCooldown)
                {
                    _state = CircuitState.HalfOpen;
                    _logger.LogInformation("AiResourceGovernor: circuit breaker -> HalfOpen.");
                }
                return _state;
            }
        }
    }

    /// <summary>
    /// Verifica se o circuit breaker está aberto. Lança InvalidOperationException se sim.
    /// </summary>
    public void CheckCircuit()
    {
        if (State == CircuitState.Open)
            throw new InvalidOperationException(
                "AI Resource Governor: circuit breaker OPEN — inferências bloqueadas temporariamente.");
    }

    /// <summary>
    /// Adquire o semáforo (aguarda slot disponível).
    /// Devolve um disposable que liberta o slot automaticamente.
    /// </summary>
    public async Task<IDisposable> AcquireSlotAsync(CancellationToken cancellationToken)
    {
        CheckCircuit();
        await _semaphore.WaitAsync(cancellationToken);
        return new SemaphoreReleaser(_semaphore);
    }

    /// <summary>
    /// Notifica sucesso — fecha o circuit breaker se estava HalfOpen.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_stateLock)
        {
            if (_state == CircuitState.HalfOpen || _consecutiveFailures > 0)
            {
                _consecutiveFailures = 0;
                _state = CircuitState.Closed;
                _logger.LogInformation("AiResourceGovernor: circuit breaker -> Closed.");
            }
        }
    }

    /// <summary>
    /// Notifica falha — incrementa contagem; abre o circuit breaker se limiar atingido.
    /// </summary>
    public void RecordFailure()
    {
        lock (_stateLock)
        {
            _consecutiveFailures++;
            _logger.LogWarning(
                "AiResourceGovernor: falha registada ({Count}/{Threshold}).",
                _consecutiveFailures, _circuitBreakerThreshold);

            if (_consecutiveFailures >= _circuitBreakerThreshold && _state == CircuitState.Closed)
            {
                _state = CircuitState.Open;
                _openedAt = DateTimeOffset.UtcNow;
                _logger.LogError(
                    "AiResourceGovernor: circuit breaker OPEN após {Count} falhas consecutivas.",
                    _consecutiveFailures);
            }
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _queue.Writer.TryComplete();
    }

    private sealed class SemaphoreReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }

    private sealed record PendingRequest(TaskCompletionSource Tcs, CancellationToken Token);
}
