using NexTraceOne.Integrations.Domain.LegacyTelemetry;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

/// <summary>
/// Serviço de correlação de eventos legacy com ativos do catálogo.
/// </summary>
public interface ILegacyEventCorrelator
{
    Task<CorrelationResult> CorrelateByJobNameAsync(string jobName, string? systemName, CancellationToken ct);
    Task<CorrelationResult> CorrelateByQueueAsync(string? queueManagerName, string? queueName, CancellationToken ct);
    Task<CorrelationResult> CorrelateByTransactionAsync(string? transactionId, string? systemName, CancellationToken ct);
    Task<CorrelationResult> CorrelateByProgramNameAsync(string programName, string? systemName, CancellationToken ct);
    Task<CorrelationResult> CorrelateBySystemNameAsync(string systemName, CancellationToken ct);
}
