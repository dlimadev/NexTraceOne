using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityImpact;

/// <summary>
/// Feature: GetCanonicalEntityImpact — analisa o impacto potencial de uma alteração
/// numa entidade canónica, identificando todos os contratos potencialmente afetados.
/// Fundamental para change intelligence e governança de contratos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetCanonicalEntityImpact
{
    /// <summary>Query de análise de impacto de uma entidade canónica.</summary>
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
    /// Handler que identifica contratos potencialmente afetados quando uma entidade canónica muda.
    /// Procura referências ao nome da entidade canónica nos contratos e classifica o impacto
    /// com base no estado de ciclo de vida do contrato.
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

            var impacted = contracts
                .Where(c => c.SpecContent.Contains(entity.Name, StringComparison.OrdinalIgnoreCase))
                .Select(c => new ImpactedContract(
                    c.Id.Value,
                    c.ApiAssetId,
                    c.SemVer,
                    c.Protocol.ToString(),
                    c.LifecycleState.ToString(),
                    c.IsLocked,
                    c.CreatedAt))
                .ToList()
                .AsReadOnly();

            var riskLevel = impacted.Count switch
            {
                0 => "None",
                <= 3 => "Low",
                <= 10 => "Medium",
                _ => "High"
            };

            return new Response(
                request.CanonicalEntityId,
                entity.Name,
                entity.Criticality,
                impacted.Count,
                riskLevel,
                impacted);
        }
    }

    /// <summary>Contrato potencialmente afetado por uma alteração na entidade canónica.</summary>
    public sealed record ImpactedContract(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Protocol,
        string LifecycleState,
        bool IsLocked,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da análise de impacto de alteração numa entidade canónica.</summary>
    public sealed record Response(
        Guid CanonicalEntityId,
        string EntityName,
        string Criticality,
        int TotalImpacted,
        string RiskLevel,
        IReadOnlyList<ImpactedContract> ImpactedContracts);
}
