using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.PropagateCanonicalEntityChange;

/// <summary>
/// Feature: PropagateCanonicalEntityChange — propaga o impacto de uma mudança de versão
/// de uma entidade canónica para todos os contratos que a referenciam.
/// Identifica contratos potencialmente impactados, classificando o nível de impacto
/// para apoiar change intelligence e governança de contratos.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class PropagateCanonicalEntityChange
{
    /// <summary>Command de propagação de mudança de entidade canónica.</summary>
    public sealed record Command(Guid CanonicalEntityId, string NewVersion) : ICommand<Response>;

    /// <summary>Valida a entrada do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CanonicalEntityId).NotEmpty();
            RuleFor(x => x.NewVersion).NotEmpty().MaximumLength(50);
        }
    }

    /// <summary>
    /// Handler que identifica e classifica contratos potencialmente impactados pela nova versão
    /// da entidade canónica, baseando-se no nome da entidade e no estado de ciclo de vida dos contratos.
    /// </summary>
    public sealed class Handler(
        ICanonicalEntityRepository entityRepository,
        IContractVersionRepository contractVersionRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entity = await entityRepository.GetByIdAsync(
                CanonicalEntityId.From(request.CanonicalEntityId), cancellationToken);
            if (entity is null)
                return ContractsErrors.CanonicalEntityNotFound(request.CanonicalEntityId.ToString());

            var (contracts, _) = await contractVersionRepository.SearchAsync(
                null, null, null, entity.Name, 1, 500, cancellationToken);

            var impacted = contracts
                .Where(c => c.SpecContent.Contains(entity.Name, StringComparison.OrdinalIgnoreCase))
                .Select(c => new ImpactedContractItem(
                    c.Id.Value,
                    c.ApiAssetId,
                    DeriveContractTitle(c.SpecContent),
                    DeriveServiceName(c.SpecContent),
                    entity.Domain,
                    DetermineImpactLevel(c.LifecycleState.ToString(), entity.Criticality)))
                .ToList()
                .AsReadOnly();

            return new Response(
                request.CanonicalEntityId,
                entity.Name,
                request.NewVersion,
                impacted.Count,
                impacted);
        }

        private static string DeriveContractTitle(string specContent)
        {
            // Tenta extrair o título do spec para melhor legibilidade
            try
            {
                if (specContent.TrimStart().StartsWith('{'))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(specContent);
                    if (doc.RootElement.TryGetProperty("info", out var info) &&
                        info.TryGetProperty("title", out var title))
                        return title.GetString() ?? "Unknown";
                }
            }
            catch { /* Ignorar */ }
            return "Unknown";
        }

        private static string DeriveServiceName(string specContent)
        {
            try
            {
                if (specContent.TrimStart().StartsWith('{'))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(specContent);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("info", out var info))
                    {
                        if (info.TryGetProperty("x-service-name", out var sn)) return sn.GetString() ?? "";
                        if (info.TryGetProperty("x-owner", out var owner)) return owner.GetString() ?? "";
                    }
                }
            }
            catch { /* Ignorar */ }
            return "Unknown";
        }

        private static string DetermineImpactLevel(string lifecycleState, string criticality)
        {
            if (lifecycleState is "Locked" or "Approved")
            {
                return criticality switch
                {
                    "Critical" => "High",
                    "High" => "High",
                    "Medium" => "Medium",
                    _ => "Low"
                };
            }
            return "Low";
        }
    }

    /// <summary>Contrato impactado pela mudança da entidade canónica.</summary>
    public sealed record ImpactedContractItem(
        Guid ContractVersionId,
        Guid ContractId,
        string ContractTitle,
        string ServiceName,
        string Domain,
        string ImpactLevel);

    /// <summary>Resposta da propagação de mudança de entidade canónica.</summary>
    public sealed record Response(
        Guid CanonicalEntityId,
        string EntityName,
        string NewVersion,
        int TotalImpacted,
        IReadOnlyList<ImpactedContractItem> ImpactedContracts);
}
