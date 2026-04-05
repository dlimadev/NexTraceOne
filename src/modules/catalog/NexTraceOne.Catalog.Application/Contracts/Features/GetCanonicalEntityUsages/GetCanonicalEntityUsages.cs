using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityUsages;

/// <summary>
/// Feature: GetCanonicalEntityUsages — lista os contratos que utilizam uma entidade canónica.
/// Permite rastrear onde e como as entidades canónicas são referenciadas nos contratos,
/// suportando análise de impacto e governança de reutilização.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCanonicalEntityUsages
{
    /// <summary>Query de usos de uma entidade canónica.</summary>
    public sealed record Query(Guid CanonicalEntityId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.CanonicalEntityId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que busca os contratos que referenciam a entidade canónica.
    /// Consulta as versões de contrato para encontrar referências ao schema canónico
    /// no conteúdo da especificação (por nome da entidade).
    /// </summary>
    public sealed class Handler(
        ICanonicalEntityRepository entityRepository,
        IContractVersionRepository contractVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entity = await entityRepository.GetByIdAsync(
                CanonicalEntityId.From(request.CanonicalEntityId), cancellationToken);

            if (entity is null)
                return ContractsErrors.CanonicalEntityNotFound(request.CanonicalEntityId.ToString());

            // Pesquisa nos contratos por referências ao nome da entidade canónica
            var (contracts, _) = await contractVersionRepository.SearchAsync(
                null, null, null, entity.Name, 1, 500, cancellationToken);

            var usages = contracts
                .Where(c => c.SpecContent.Contains(entity.Name, StringComparison.OrdinalIgnoreCase))
                .Select(c => new UsageReference(
                    c.Id.Value,
                    c.ApiAssetId,
                    c.SemVer,
                    c.Protocol.ToString(),
                    c.LifecycleState.ToString(),
                    c.CreatedAt))
                .ToList()
                .AsReadOnly();

            return new Response(
                request.CanonicalEntityId,
                entity.Name,
                usages.Count,
                usages);
        }
    }

    /// <summary>Referência de uso de uma entidade canónica num contrato.</summary>
    public sealed record UsageReference(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Protocol,
        string LifecycleState,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta com a lista de contratos que usam a entidade canónica.</summary>
    public sealed record Response(
        Guid CanonicalEntityId,
        string EntityName,
        int TotalUsages,
        IReadOnlyList<UsageReference> Usages);
}
