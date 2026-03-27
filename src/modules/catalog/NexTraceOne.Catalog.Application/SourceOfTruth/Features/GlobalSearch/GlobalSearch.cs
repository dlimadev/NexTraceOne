using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.SourceOfTruth.Abstractions;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;
using NexTraceOne.Knowledge.Contracts;

namespace NexTraceOne.Catalog.Application.SourceOfTruth.Features.GlobalSearch;

/// <summary>
/// Feature: GlobalSearch — pesquisa global unificada do NexTraceOne.
/// Endpoint único que permite encontrar serviços, contratos, referências documentais,
/// runbooks, documentos de conhecimento e notas operacionais a partir de um termo
/// de busca, devolvendo uma lista homogénea de resultados com facetas e relevância calculada.
///
/// P10.2: Integração com Knowledge Hub via IKnowledgeSearchProvider cross-module.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GlobalSearch
{
    private static readonly string[] ValidScopes =
        ["all", "services", "contracts", "changes", "incidents", "runbooks", "docs", "knowledge", "notes"];

    /// <summary>
    /// Query de pesquisa global unificada.
    /// O parâmetro Persona é reservado para filtragem persona-aware futura.
    /// </summary>
    public sealed record Query(
        string SearchTerm,
        string? Scope = null,
        string? Persona = null,
        int MaxResults = 25) : IQuery<Response>;

    /// <summary>Valida a entrada da query de pesquisa global.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SearchTerm).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MaxResults).InclusiveBetween(1, 100);
            RuleFor(x => x.Scope)
                .Must(s => ValidScopes.Contains(s))
                .When(x => x.Scope is not null)
                .WithMessage("Scope must be one of: all, services, contracts, changes, incidents, runbooks, docs, knowledge, notes.");
        }
    }

    /// <summary>
    /// Handler que pesquisa serviços, contratos, referências, documentos de conhecimento
    /// e notas operacionais, devolvendo resultados unificados com facetas e ordenação
    /// por relevância.
    ///
    /// P10.2: IKnowledgeSearchProvider é injectado como dependência opcional para
    /// permitir que a pesquisa funcione mesmo sem o módulo Knowledge registado.
    /// </summary>
    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IContractVersionRepository contractRepository,
        ILinkedReferenceRepository referenceRepository,
        IKnowledgeSearchProvider? knowledgeSearchProvider = null) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = new List<SearchResultItem>();
            var facetCounts = new Dictionary<string, int>();
            var scopeAll = string.IsNullOrWhiteSpace(request.Scope) || request.Scope == "all";
            var term = request.SearchTerm;

            // Pesquisar serviços
            if (scopeAll || request.Scope == "services")
            {
                var found = await serviceRepository.SearchAsync(term, cancellationToken);
                var services = found.Take(request.MaxResults).ToList();

                foreach (var s in services)
                {
                    var score = CalculateRelevance(term, s.Name, s.DisplayName);
                    items.Add(new SearchResultItem(
                        s.Id.Value,
                        "service",
                        s.DisplayName,
                        $"{s.Domain} · {s.TeamName}",
                        s.TeamName,
                        s.LifecycleStatus.ToString(),
                        $"/services/{s.Id.Value}",
                        score));
                }

                facetCounts["services"] = found.Count;
            }

            // Pesquisar contratos
            if (scopeAll || request.Scope == "contracts")
            {
                var (contracts, totalContracts) = await contractRepository.SearchAsync(
                    null, null, null, term, 1, request.MaxResults, cancellationToken);

                foreach (var c in contracts)
                {
                    var score = CalculateRelevance(term, c.SemVer, c.Protocol.ToString());
                    items.Add(new SearchResultItem(
                        c.Id.Value,
                        "contract",
                        $"{c.Protocol} v{c.SemVer}",
                        c.LifecycleState.ToString(),
                        null,
                        c.LifecycleState.ToString(),
                        $"/contracts?versionId={c.Id.Value}",
                        score));
                }

                facetCounts["contracts"] = totalContracts;
            }

            // Pesquisar referências documentais e runbooks
            if (scopeAll || request.Scope == "docs" || request.Scope == "runbooks")
            {
                LinkedReferenceType? refType = request.Scope switch
                {
                    "docs" => LinkedReferenceType.Documentation,
                    "runbooks" => LinkedReferenceType.Runbook,
                    _ => null
                };

                var references = await referenceRepository.SearchAsync(term, refType, cancellationToken);
                var referenceList = references.Take(request.MaxResults).ToList();

                foreach (var r in referenceList)
                {
                    var entityType = r.ReferenceType == LinkedReferenceType.Runbook ? "runbook" : "doc";
                    var score = CalculateRelevance(term, r.Title, r.Description);
                    items.Add(new SearchResultItem(
                        r.Id.Value,
                        entityType,
                        r.Title,
                        r.Description,
                        null,
                        r.IsActive ? "Active" : "Inactive",
                        $"/knowledge/references/{r.Id.Value}",
                        score));
                }

                if (scopeAll)
                {
                    var runbookCount = referenceList.Count(r => r.ReferenceType == LinkedReferenceType.Runbook);
                    facetCounts["runbooks"] = runbookCount;
                    facetCounts["docs"] = referenceList.Count - runbookCount;
                }
                else
                {
                    facetCounts[request.Scope!] = referenceList.Count;
                }
            }

            // Pesquisar no Knowledge Hub — documentos e notas operacionais (P10.2)
            if (knowledgeSearchProvider is not null
                && (scopeAll || request.Scope == "knowledge" || request.Scope == "notes"))
            {
                var knowledgeResults = await knowledgeSearchProvider.SearchAsync(
                    term, request.Scope, request.MaxResults, cancellationToken);

                foreach (var kr in knowledgeResults)
                {
                    items.Add(new SearchResultItem(
                        kr.EntityId,
                        kr.EntityType,
                        kr.Title,
                        kr.Subtitle,
                        null,
                        kr.Status,
                        kr.Route,
                        kr.RelevanceScore));
                }

                var knowledgeCount = knowledgeResults.Count(r => r.EntityType == "knowledge");
                var notesCount = knowledgeResults.Count(r => r.EntityType == "note");

                if (scopeAll)
                {
                    facetCounts["knowledge"] = knowledgeCount;
                    facetCounts["notes"] = notesCount;
                }
                else if (request.Scope == "knowledge")
                {
                    facetCounts["knowledge"] = knowledgeCount;
                }
                else if (request.Scope == "notes")
                {
                    facetCounts["notes"] = notesCount;
                }
            }
            else
            {
                if (scopeAll || request.Scope == "knowledge")
                    facetCounts.TryAdd("knowledge", 0);
                if (scopeAll || request.Scope == "notes")
                    facetCounts.TryAdd("notes", 0);
            }

            // Escopos futuros — ainda sem repositórios implementados
            if (scopeAll || request.Scope == "changes")
                facetCounts.TryAdd("changes", 0);

            if (scopeAll || request.Scope == "incidents")
                facetCounts.TryAdd("incidents", 0);

            // Ordenar por relevância descendente e limitar ao máximo solicitado
            var sorted = items
                .OrderByDescending(i => i.RelevanceScore)
                .Take(request.MaxResults)
                .ToList();

            return new Response(sorted, facetCounts, TotalResults: items.Count);
        }

        /// <summary>
        /// Calcula relevância simples: correspondência exata no campo primário gera
        /// pontuação mais alta; correspondência parcial gera pontuação intermédia.
        /// </summary>
        private static double CalculateRelevance(
            string searchTerm,
            string primaryField,
            string? secondaryField)
        {
            var score = 0.0;

            if (!string.IsNullOrWhiteSpace(primaryField))
            {
                if (primaryField.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                    score += 1.0;
                else if (primaryField.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    score += 0.7;
            }

            if (!string.IsNullOrWhiteSpace(secondaryField))
            {
                if (secondaryField.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                    score += 0.5;
                else if (secondaryField.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    score += 0.3;
            }

            return score;
        }
    }

    /// <summary>Item individual de resultado da pesquisa global.</summary>
    public sealed record SearchResultItem(
        Guid EntityId,
        string EntityType,
        string Title,
        string? Subtitle,
        string? Owner,
        string? Status,
        string Route,
        double RelevanceScore);

    /// <summary>Resposta da pesquisa global unificada com facetas e contagem total.</summary>
    public sealed record Response(
        IReadOnlyList<SearchResultItem> Items,
        IReadOnlyDictionary<string, int> FacetCounts,
        int TotalResults);
}
