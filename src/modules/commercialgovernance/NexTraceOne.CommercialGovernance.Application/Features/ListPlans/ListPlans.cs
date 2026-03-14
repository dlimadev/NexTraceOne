using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.CommercialCatalog.Application.Abstractions;

namespace NexTraceOne.CommercialCatalog.Application.Features.ListPlans;

/// <summary>
/// Feature: ListPlans — query para listar planos comerciais disponíveis.
///
/// Usada pelo backoffice interno e pelo portal para exibir planos cadastrados.
/// Suporta filtro opcional por estado ativo.
///
/// Permissão requerida: licensing:vendor:plan:read
/// </summary>
public static class ListPlans
{
    /// <summary>Query para listar planos com filtro opcional por estado ativo.</summary>
    public sealed record Query(bool? ActiveOnly = null) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query (nenhuma regra obrigatória).</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
    }

    /// <summary>
    /// Handler que retorna uma lista de planos comerciais.
    /// Projeção leve para listagem — sem detalhes de FeaturePacks associados.
    /// </summary>
    public sealed class Handler(
        IPlanRepository planRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var plans = await planRepository.ListAsync(request.ActiveOnly, cancellationToken);

            var items = plans.Select(p => new PlanItem(
                p.Id.Value,
                p.Code,
                p.Name,
                p.Description,
                p.CommercialModel.ToString(),
                p.DeploymentModel.ToString(),
                p.IsActive,
                p.MaxActivations,
                p.GracePeriodDays,
                p.TrialDurationDays,
                p.PriceTag)).ToList();

            return new Response(items);
        }
    }

    /// <summary>Item resumido de plano para listagem.</summary>
    public sealed record PlanItem(
        Guid PlanId,
        string Code,
        string Name,
        string? Description,
        string CommercialModel,
        string DeploymentModel,
        bool IsActive,
        int MaxActivations,
        int GracePeriodDays,
        int? TrialDurationDays,
        string? PriceTag);

    /// <summary>Resposta da listagem de planos comerciais.</summary>
    public sealed record Response(IReadOnlyList<PlanItem> Items);
}
