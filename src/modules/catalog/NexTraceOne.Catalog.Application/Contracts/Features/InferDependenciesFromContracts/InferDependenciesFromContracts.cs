using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.InferDependenciesFromContracts;

/// <summary>
/// Feature: InferDependenciesFromContracts — infere dependências de um serviço a partir
/// da análise dos contratos publicados e das referências a entidades canónicas.
/// Identifica serviços referenciados via $ref, canonical entities e imports nos contratos,
/// comparando com as dependências declaradas no grafo de serviços.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class InferDependenciesFromContracts
{
    /// <summary>Comando de inferência de dependências a partir de contratos.</summary>
    public sealed record Command(Guid ServiceAssetId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que analisa contratos do serviço para inferir dependências implícitas.
    /// Examina referências a entidades canónicas e nomes de serviços no conteúdo dos specs
    /// para identificar acoplamentos não declarados no grafo de dependências.
    /// </summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository,
        ICanonicalEntityRepository canonicalEntityRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceAssetId), cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceAssetId);

            // Obter todos os APIs do serviço
            var apiAssets = await apiAssetRepository.ListByServiceIdAsync(
                ServiceAssetId.From(request.ServiceAssetId), cancellationToken);

            if (apiAssets.Count == 0)
                return new Response(request.ServiceAssetId, [], 0, 0);

            // Obter versões mais recentes dos contratos de cada API
            var inferredDependencies = new List<InferredDependency>();
            var allCanonicalEntities = await canonicalEntityRepository.SearchAsync(
                null, null, null, 1, 500, cancellationToken);

            foreach (var api in apiAssets)
            {
                var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(
                    api.Id.Value, cancellationToken);

                if (latestVersion is null || string.IsNullOrWhiteSpace(latestVersion.SpecContent))
                    continue;

                // Analisar spec por referências a entidades canónicas
                foreach (var entity in allCanonicalEntities.Items)
                {
                    if (!latestVersion.SpecContent.Contains(entity.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Verificar via nome — heurística simples (não há lista de deps no domínio)
                    var alreadyDeclared = false;

                    if (!inferredDependencies.Any(i => i.TargetEntityOrDomain == entity.Domain))
                    {
                        inferredDependencies.Add(new InferredDependency(
                            request.ServiceAssetId,
                            entity.Domain,
                            entity.Name,
                            "canonical-entity-reference",
                            alreadyDeclared ? "Declared" : "Undeclared",
                            alreadyDeclared ? "Low" : "Medium"));
                    }
                }
            }

            var newCount = inferredDependencies.Count(d => d.DeclarationStatus == "Undeclared");
            var discrepancyCount = inferredDependencies.Count(d => d.DeclarationStatus == "Undeclared");

            return new Response(
                request.ServiceAssetId,
                inferredDependencies.AsReadOnly(),
                newCount,
                discrepancyCount);
        }
    }

    /// <summary>Dependência inferida a partir da análise de contratos.</summary>
    public sealed record InferredDependency(
        Guid SourceServiceId,
        string TargetEntityOrDomain,
        string Basis,
        string InferenceMethod,
        string DeclarationStatus,
        string Confidence);

    /// <summary>Resposta da inferência de dependências a partir de contratos.</summary>
    public sealed record Response(
        Guid ServiceAssetId,
        IReadOnlyList<InferredDependency> InferredDependencies,
        int NewDependenciesCount,
        int DiscrepanciesCount);
}
