using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SuggestSchemaFromContext;

/// <summary>
/// Feature: SuggestSchemaFromContext — sugere schemas de request/response a partir do contexto
/// do endpoint (método HTTP, path, domínio).
/// Baseado em regras e entidades canónicas publicadas no domínio — não usa IA externa.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class SuggestSchemaFromContext
{
    /// <summary>Query de sugestão de schema para um endpoint.</summary>
    public sealed record Query(
        string Method,
        string Path,
        string? Domain = null,
        Guid? ServiceAssetId = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Method).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Path).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>
    /// Handler que sugere schemas baseado em entidades canónicas do domínio
    /// e convenções RESTful baseadas no método e path.
    /// </summary>
    public sealed class Handler(ICanonicalEntityRepository canonicalEntityRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var method = request.Method.ToUpperInvariant();

            // Obter entidades canónicas do domínio
            var (entities, _) = await canonicalEntityRepository.SearchAsync(
                null,
                request.Domain,
                null,
                1,
                50,
                cancellationToken);

            // Sugerir parâmetros comuns baseados no método
            var suggestedParameters = BuildCommonParameters(method);

            // Sugerir request body baseado no método e path
            var requestBodySuggestion = BuildRequestBodySuggestion(method, request.Path, entities.Count > 0 ? entities[0].Name : null);

            // Sugerir response baseado no método
            var responseSuggestion = BuildResponseSuggestion(method, request.Path);

            // Entidades canónicas relevantes
            var relevantEntities = entities
                .Take(5)
                .Select(e => new RelevantEntity(
                    e.Id.Value,
                    e.Name,
                    e.Domain,
                    e.Category,
                    $"#/components/schemas/{e.Name}"))
                .ToList()
                .AsReadOnly();

            return new Response(
                method,
                request.Path,
                requestBodySuggestion,
                responseSuggestion,
                suggestedParameters,
                relevantEntities);
        }

        private static IReadOnlyList<SuggestedParameter> BuildCommonParameters(string method)
        {
            var parameters = new List<SuggestedParameter>
            {
                new("X-Correlation-Id", "header", "string", "Identificador de correlação para rastreabilidade.")
            };

            if (method == "GET")
            {
                parameters.Add(new("page", "query", "integer", "Número da página (paginação)."));
                parameters.Add(new("pageSize", "query", "integer", "Tamanho da página (máximo 100)."));
                parameters.Add(new("q", "query", "string", "Termo de pesquisa livre."));
            }

            return parameters.AsReadOnly();
        }

        private static IReadOnlyList<SuggestedProperty> BuildRequestBodySuggestion(string method, string path, string? canonicalEntityName)
        {
            if (method is "GET" or "DELETE" or "HEAD" or "OPTIONS")
                return [];

            var properties = new List<SuggestedProperty>();

            if (canonicalEntityName is not null)
                properties.Add(new(canonicalEntityName.ToLowerInvariant(), "$ref", $"#/components/schemas/{canonicalEntityName}", "Entidade do domínio.", "canonical-entity"));

            // Propriedades genéricas inferidas do path
            var segment = path.Split('/').LastOrDefault(s => !s.StartsWith('{')) ?? "resource";
            properties.Add(new("name", "string", null, $"Nome do {segment}.", "convention"));
            properties.Add(new("description", "string", null, $"Descrição do {segment}.", "convention"));

            return properties.AsReadOnly();
        }

        private static IReadOnlyList<SuggestedProperty> BuildResponseSuggestion(string method, string path)
        {
            var segment = path.Split('/').LastOrDefault(s => !s.StartsWith('{')) ?? "item";

            if (method == "GET" && !path.Contains('{'))
            {
                // Listagem — wrapper paginado
                return new List<SuggestedProperty>
                {
                    new("items", "array", null, $"Lista de {segment}.", "pagination-wrapper"),
                    new("totalCount", "integer", null, "Total de resultados.", "pagination-wrapper"),
                    new("page", "integer", null, "Página actual.", "pagination-wrapper"),
                    new("pageSize", "integer", null, "Tamanho da página.", "pagination-wrapper"),
                }.AsReadOnly();
            }

            // Resposta singular
            return new List<SuggestedProperty>
            {
                new("id", "string", null, $"Identificador único do {segment}.", "convention"),
                new("createdAt", "string", null, "Timestamp de criação (ISO 8601).", "convention"),
            }.AsReadOnly();
        }
    }

    /// <summary>Parâmetro sugerido para o endpoint.</summary>
    public sealed record SuggestedParameter(
        string Name,
        string In,
        string Type,
        string Description);

    /// <summary>Propriedade sugerida para o schema.</summary>
    public sealed record SuggestedProperty(
        string Name,
        string Type,
        string? RefTarget,
        string Description,
        string Source);

    /// <summary>Entidade canónica relevante para o contexto do endpoint.</summary>
    public sealed record RelevantEntity(
        Guid EntityId,
        string Name,
        string Domain,
        string Category,
        string RefPath);

    /// <summary>Resposta com sugestões de schema para o endpoint.</summary>
    public sealed record Response(
        string Method,
        string Path,
        IReadOnlyList<SuggestedProperty> RequestBodySuggestion,
        IReadOnlyList<SuggestedProperty> ResponseSuggestion,
        IReadOnlyList<SuggestedParameter> SuggestedParameters,
        IReadOnlyList<RelevantEntity> RelevantCanonicalEntities);
}
