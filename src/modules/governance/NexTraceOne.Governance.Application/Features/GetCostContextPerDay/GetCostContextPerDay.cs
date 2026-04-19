using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetCostContextPerDay;

/// <summary>
/// Feature: GetCostContextPerDay — contexto de custo por dia para um serviço num ambiente.
/// Usado pelo PromotionPage antes de avaliar o gate de budget para obter actualCostPerDay e baselineCostPerDay.
/// Retorna null quando nenhum dado de custo está disponível para o serviço/ambiente.
/// </summary>
public static class GetCostContextPerDay
{
    /// <summary>Query com nome de serviço e ambiente.</summary>
    public sealed record Query(string ServiceName, string Environment) : IQuery<Response?>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que delega para ICostIntelligenceModule.GetCostContextPerDayAsync.</summary>
    public sealed class Handler(ICostIntelligenceModule costModule) : IQueryHandler<Query, Response?>
    {
        public async Task<Result<Response?>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var context = await costModule.GetCostContextPerDayAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (context is null)
                return Result<Response?>.Success(null);

            return Result<Response?>.Success(new Response(
                ServiceName: context.ServiceName,
                Environment: context.Environment,
                ActualCostPerDay: context.ActualCostPerDay,
                BaselineCostPerDay: context.BaselineCostPerDay,
                Currency: context.Currency));
        }
    }

    /// <summary>Resposta com contexto de custo por dia.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        decimal ActualCostPerDay,
        decimal BaselineCostPerDay,
        string Currency);
}
