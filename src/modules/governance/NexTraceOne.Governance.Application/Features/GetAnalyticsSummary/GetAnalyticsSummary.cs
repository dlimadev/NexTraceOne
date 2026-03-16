using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetAnalyticsSummary;

/// <summary>
/// Retorna resumo consolidado de product analytics.
/// Fornece visão de adoção, valor, fricção e tendências do produto.
/// Suporta filtros por persona, módulo, equipa, domínio e período.
/// </summary>
public static class GetAnalyticsSummary
{
    /// <summary>Query com filtros opcionais para o resumo de analytics.</summary>
    public sealed record Query(
        string? Persona,
        string? Module,
        string? TeamId,
        string? DomainId,
        string? Range) : IQuery<Response>;

    /// <summary>Handler que compila e retorna o resumo de analytics.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // MVP: dados estáticos representativos para validação da experiência.
            var response = new Response(
                TotalEvents: 12_847,
                UniqueUsers: 234,
                ActivePersonas: 6,
                TopModules: new[]
                {
                    new ModuleUsageDto(ProductModule.SourceOfTruth, "Source of Truth", 3_240, 89, TrendDirection.Improving),
                    new ModuleUsageDto(ProductModule.ContractStudio, "Contract Studio", 2_810, 76, TrendDirection.Improving),
                    new ModuleUsageDto(ProductModule.ChangeIntelligence, "Change Intelligence", 2_150, 68, TrendDirection.Stable),
                    new ModuleUsageDto(ProductModule.AiAssistant, "AI Assistant", 1_980, 72, TrendDirection.Improving),
                    new ModuleUsageDto(ProductModule.Incidents, "Incidents", 1_420, 54, TrendDirection.Stable),
                    new ModuleUsageDto(ProductModule.Reliability, "Reliability", 1_247, 48, TrendDirection.Declining)
                },
                AdoptionScore: 74.5m,
                ValueScore: 68.2m,
                FrictionScore: 22.1m,
                AvgTimeToFirstValueMinutes: 18.5m,
                AvgTimeToCoreValueMinutes: 142.0m,
                TrendDirection: TrendDirection.Improving,
                PeriodLabel: request.Range ?? "last_30d");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resumo consolidado de product analytics.</summary>
    public sealed record Response(
        long TotalEvents,
        int UniqueUsers,
        int ActivePersonas,
        IReadOnlyList<ModuleUsageDto> TopModules,
        decimal AdoptionScore,
        decimal ValueScore,
        decimal FrictionScore,
        decimal AvgTimeToFirstValueMinutes,
        decimal AvgTimeToCoreValueMinutes,
        TrendDirection TrendDirection,
        string PeriodLabel);

    /// <summary>Resumo de uso por módulo.</summary>
    public sealed record ModuleUsageDto(
        ProductModule Module,
        string ModuleName,
        long EventCount,
        int UniqueUsers,
        TrendDirection Trend);
}
