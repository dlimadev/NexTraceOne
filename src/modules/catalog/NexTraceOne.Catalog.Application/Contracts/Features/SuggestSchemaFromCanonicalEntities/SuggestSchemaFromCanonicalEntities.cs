using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SuggestSchemaFromCanonicalEntities;

/// <summary>
/// Feature: SuggestSchemaFromCanonicalEntities — dado um contexto descritivo ou
/// nome de propriedade, sugere entidades canónicas correspondentes e gera snippets
/// de referência ($ref). Motor de sugestão baseado em regras (não depende de LLM
/// externo), seguindo o princípio de IA interna do NexTraceOne.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SuggestSchemaFromCanonicalEntities
{
    /// <summary>Query para sugestão de schemas a partir de entidades canónicas.</summary>
    public sealed record Query(
        string Context,
        string? PropertyName = null,
        string? Domain = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Context).NotEmpty().MaximumLength(500);
            RuleFor(x => x.PropertyName).MaximumLength(200).When(x => x.PropertyName is not null);
            RuleFor(x => x.Domain).MaximumLength(100).When(x => x.Domain is not null);
        }
    }

    /// <summary>
    /// Handler que pesquisa entidades canónicas correspondentes ao contexto fornecido,
    /// pontua as correspondências por relevância de keywords e gera snippets $ref
    /// para reutilização directa nos contratos.
    /// </summary>
    public sealed class Handler(
        ICanonicalEntityRepository entityRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var keywords = ParseKeywords(request.Context, request.PropertyName);
            if (keywords.Count == 0)
                return new Response([]);

            // Pesquisar entidades canónicas para cada keyword e consolidar resultados
            var candidateMap = new Dictionary<Guid, (CanonicalEntity Entity, int Hits, List<string> MatchedKeywords)>();

            foreach (var keyword in keywords)
            {
                var (items, _) = await entityRepository.SearchAsync(
                    keyword, request.Domain, null, 1, 100, cancellationToken);

                foreach (var entity in items)
                {
                    if (!candidateMap.TryGetValue(entity.Id.Value, out var existing))
                    {
                        candidateMap[entity.Id.Value] = (entity, 1, [keyword]);
                    }
                    else
                    {
                        existing.Hits++;
                        existing.MatchedKeywords.Add(keyword);
                        candidateMap[entity.Id.Value] = existing;
                    }
                }
            }

            if (candidateMap.Count == 0)
                return new Response([]);

            // Pontuar e ordenar por relevância
            var suggestions = candidateMap.Values
                .Select(candidate =>
                {
                    var score = ComputeRelevanceScore(
                        candidate.Entity, keywords, candidate.Hits, candidate.MatchedKeywords);

                    var matchReason = BuildMatchReason(candidate.MatchedKeywords, candidate.Entity);
                    var refPath = $"#/components/schemas/{candidate.Entity.Name}";

                    return new SchemaSuggestion(
                        CanonicalEntityId: candidate.Entity.Id.Value,
                        EntityName: candidate.Entity.Name,
                        Domain: candidate.Entity.Domain,
                        Category: candidate.Entity.Category,
                        RefPath: refPath,
                        RelevanceScore: score,
                        MatchReason: matchReason);
                })
                .OrderByDescending(s => s.RelevanceScore)
                .ThenBy(s => s.EntityName)
                .ToList()
                .AsReadOnly();

            return new Response(suggestions);
        }

        /// <summary>Extrai keywords do contexto e nome de propriedade.</summary>
        private static List<string> ParseKeywords(string context, string? propertyName)
        {
            var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var word in context.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var normalized = word.Trim('.', ',', ';', ':', '!', '?', '"', '\'').ToLowerInvariant();
                if (normalized.Length >= 2)
                    words.Add(normalized);
            }

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                var normalized = propertyName.Trim().ToLowerInvariant();
                if (normalized.Length >= 2)
                    words.Add(normalized);
            }

            return words.ToList();
        }

        /// <summary>Calcula score de relevância baseado em correspondências de keywords.</summary>
        private static decimal ComputeRelevanceScore(
            CanonicalEntity entity,
            List<string> keywords,
            int hits,
            List<string> matchedKeywords)
        {
            var baseScore = (decimal)hits / keywords.Count;

            // Bonus por correspondência exacta no nome
            if (matchedKeywords.Any(kw =>
                entity.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                baseScore += 0.2m;

            // Bonus por correspondência na descrição
            if (matchedKeywords.Any(kw =>
                entity.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                baseScore += 0.1m;

            return Math.Min(1.0m, Math.Round(baseScore, 2));
        }

        /// <summary>Constrói razão legível da correspondência.</summary>
        private static string BuildMatchReason(List<string> matchedKeywords, CanonicalEntity entity)
        {
            var distinctKeywords = matchedKeywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var keywordsText = string.Join(", ", distinctKeywords.Select(k => $"'{k}'"));
            return $"Matched keywords {keywordsText} in canonical entity '{entity.Name}' (domain: {entity.Domain})";
        }
    }

    /// <summary>Sugestão de schema baseada em entidade canónica.</summary>
    public sealed record SchemaSuggestion(
        Guid CanonicalEntityId,
        string EntityName,
        string Domain,
        string Category,
        string RefPath,
        decimal RelevanceScore,
        string MatchReason);

    /// <summary>Resposta com sugestões de schemas baseadas em entidades canónicas.</summary>
    public sealed record Response(IReadOnlyList<SchemaSuggestion> Suggestions);
}
