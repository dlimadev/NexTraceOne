using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.RuntimeIntelligence.Application.Abstractions;
using NexTraceOne.RuntimeIntelligence.Domain.Errors;

namespace NexTraceOne.RuntimeIntelligence.Application.Features.GetRuntimeHealth;

/// <summary>
/// Feature: GetRuntimeHealth — obtém o estado de saúde mais recente de um serviço em runtime.
/// Consulta o último snapshot capturado para o serviço e ambiente informados,
/// retornando métricas operacionais e a classificação de saúde (Healthy/Degraded/Unhealthy).
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRuntimeHealth
{
    /// <summary>Query para obter a saúde mais recente de um serviço e ambiente.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de saúde de runtime.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que busca o snapshot de runtime mais recente de um serviço.
    /// Retorna erro NotFound se nenhum snapshot foi capturado para a combinação serviço/ambiente.
    /// </summary>
    public sealed class Handler(
        IRuntimeSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshot = await repository.GetLatestByServiceAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (snapshot is null)
                return RuntimeIntelligenceErrors.SnapshotNotFound($"{request.ServiceName}/{request.Environment}");

            return new Response(
                snapshot.Id.Value,
                snapshot.ServiceName,
                snapshot.Environment,
                snapshot.HealthStatus.ToString(),
                snapshot.AvgLatencyMs,
                snapshot.P99LatencyMs,
                snapshot.ErrorRate,
                snapshot.RequestsPerSecond,
                snapshot.CpuUsagePercent,
                snapshot.MemoryUsageMb,
                snapshot.ActiveInstances,
                snapshot.CapturedAt,
                snapshot.Source);
        }
    }

    /// <summary>Resposta com o estado de saúde e métricas do último snapshot de runtime.</summary>
    public sealed record Response(
        Guid SnapshotId,
        string ServiceName,
        string Environment,
        string HealthStatus,
        decimal AvgLatencyMs,
        decimal P99LatencyMs,
        decimal ErrorRate,
        decimal RequestsPerSecond,
        decimal CpuUsagePercent,
        decimal MemoryUsageMb,
        int ActiveInstances,
        DateTimeOffset CapturedAt,
        string Source);
}
