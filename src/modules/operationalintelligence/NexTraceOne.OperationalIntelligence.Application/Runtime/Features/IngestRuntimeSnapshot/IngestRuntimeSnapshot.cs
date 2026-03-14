using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Entities;

namespace NexTraceOne.RuntimeIntelligence.Application.Features.IngestRuntimeSnapshot;

/// <summary>
/// Feature: IngestRuntimeSnapshot — recebe métricas de saúde de um serviço e cria um snapshot de runtime.
/// Permite captura periódica de indicadores operacionais (latência, erro, throughput, recursos)
/// para análise histórica, detecção de drift e correlação com releases.
/// A classificação de saúde (Healthy/Degraded/Unhealthy) é determinada automaticamente pelo domínio.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class IngestRuntimeSnapshot
{
    /// <summary>Comando para ingestão de um novo snapshot de saúde e performance de runtime.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        decimal AvgLatencyMs,
        decimal P99LatencyMs,
        decimal ErrorRate,
        decimal RequestsPerSecond,
        decimal CpuUsagePercent,
        decimal MemoryUsageMb,
        int ActiveInstances,
        DateTimeOffset CapturedAt,
        string Source) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de ingestão de snapshot de runtime.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AvgLatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.P99LatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ErrorRate).InclusiveBetween(0m, 1m);
            RuleFor(x => x.RequestsPerSecond).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CpuUsagePercent).InclusiveBetween(0m, 100m);
            RuleFor(x => x.MemoryUsageMb).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ActiveInstances).GreaterThanOrEqualTo(1);
            RuleFor(x => x.CapturedAt).NotEmpty();
            RuleFor(x => x.Source).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que cria e persiste um snapshot de runtime via factory method do domínio.
    /// A classificação de saúde é executada automaticamente dentro de RuntimeSnapshot.Create,
    /// com base nos limiares de taxa de erro e latência P99 definidos no aggregate.
    /// </summary>
    public sealed class Handler(
        IRuntimeSnapshotRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshot = RuntimeSnapshot.Create(
                request.ServiceName,
                request.Environment,
                request.AvgLatencyMs,
                request.P99LatencyMs,
                request.ErrorRate,
                request.RequestsPerSecond,
                request.CpuUsagePercent,
                request.MemoryUsageMb,
                request.ActiveInstances,
                request.CapturedAt,
                request.Source);

            repository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                snapshot.Id.Value,
                snapshot.ServiceName,
                snapshot.Environment,
                snapshot.HealthStatus.ToString(),
                snapshot.AvgLatencyMs,
                snapshot.P99LatencyMs,
                snapshot.ErrorRate,
                snapshot.RequestsPerSecond,
                snapshot.CapturedAt);
        }
    }

    /// <summary>Resposta da ingestão do snapshot de runtime com classificação de saúde.</summary>
    public sealed record Response(
        Guid SnapshotId,
        string ServiceName,
        string Environment,
        string HealthStatus,
        decimal AvgLatencyMs,
        decimal P99LatencyMs,
        decimal ErrorRate,
        decimal RequestsPerSecond,
        DateTimeOffset CapturedAt);
}
