using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.ListChaosExperiments;

/// <summary>
/// Feature: ListChaosExperiments — lista experimentos de chaos engineering disponíveis.
/// Retorna uma lista demonstrativa de experimentos enquanto a persistência dedicada não está implementada.
/// O IDriftFindingRepository é injetado para representar a intenção arquitetural de consulta futura.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListChaosExperiments
{
    /// <summary>Query para listar experimentos de chaos engineering com filtros opcionais e paginação.</summary>
    public sealed record Query(
        string? ServiceName,
        string? Environment,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de listagem de experimentos de chaos.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna uma lista demonstrativa de experimentos de chaos.
    /// Arquitetura preparada para substituir pela consulta real ao repositório.
    /// </summary>
    public sealed class Handler(IDriftFindingRepository repository) : IQueryHandler<Query, Response>
    {
        private static readonly DateTimeOffset DemoTime = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Demonstração estática — futuro: repositório de chaos experiments dedicado
            _ = repository;

            var items = BuildDemoItems(request.ServiceName, request.Environment);
            return Task.FromResult(Result<Response>.Success(new Response(items, items.Count)));
        }

        private static IReadOnlyList<ChaosExperimentSummary> BuildDemoItems(
            string? serviceName,
            string? environment)
        {
            var all = new[]
            {
                new ChaosExperimentSummary(
                    Guid.Parse("11111111-0000-0000-0000-000000000001"),
                    serviceName ?? "payment-service",
                    environment ?? "Production",
                    "latency-injection",
                    "Low",
                    "Completed",
                    DemoTime.AddDays(-2)),
                new ChaosExperimentSummary(
                    Guid.Parse("22222222-0000-0000-0000-000000000002"),
                    serviceName ?? "order-service",
                    environment ?? "Staging",
                    "pod-kill",
                    "High",
                    "Planned",
                    DemoTime.AddDays(-1)),
                new ChaosExperimentSummary(
                    Guid.Parse("33333333-0000-0000-0000-000000000003"),
                    serviceName ?? "notification-service",
                    environment ?? "Development",
                    "memory-stress",
                    "Medium",
                    "Running",
                    DemoTime),
            };

            return all
                .Where(e =>
                    (serviceName is null || string.Equals(e.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
                    && (environment is null || string.Equals(e.Environment, environment, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    /// <summary>Resumo de um experimento de chaos para listagem.</summary>
    public sealed record ChaosExperimentSummary(
        Guid ExperimentId,
        string ServiceName,
        string Environment,
        string ExperimentType,
        string RiskLevel,
        string Status,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta paginada com lista de experimentos de chaos.</summary>
    public sealed record Response(
        IReadOnlyList<ChaosExperimentSummary> Items,
        int TotalCount);
}
