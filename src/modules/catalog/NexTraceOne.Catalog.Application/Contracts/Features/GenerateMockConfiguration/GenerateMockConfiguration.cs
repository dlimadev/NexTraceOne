using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateMockConfiguration;

/// <summary>
/// Feature: GenerateMockConfiguration — gera configuração de mock a partir do spec de uma versão de contrato.
/// Analisa o spec OpenAPI/Swagger para extrair endpoints e exemplos de resposta,
/// produzindo um MockConfigurationResult pronto para uso em ferramentas de mock (WireMock, MSW, etc).
/// Para specs vazias ou malformadas, produz mock razoável baseado nos metadados disponíveis.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateMockConfiguration
{
    /// <summary>Query de geração de configuração de mock para uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que extrai rotas e exemplos de resposta da spec e gera configuração de mock.
    /// Usa CanonicalModelBuilder para normalizar a spec; para specs malformadas gera mock básico.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var routes = new List<MockRoute>();
            string contractTitle;
            string instructions;

            if (string.IsNullOrWhiteSpace(version.SpecContent))
            {
                contractTitle = $"Contract {version.SemVer}";
                routes.Add(new MockRoute("/api/v1/resource", "GET", 200, "{\"message\": \"mock response\"}", "application/json"));
                instructions = BuildInstructions(contractTitle, routes);
                return new Response(request.ContractVersionId, contractTitle, routes.AsReadOnly(), instructions);
            }

            try
            {
                var canonical = CanonicalModelBuilder.Build(version.SpecContent, version.Protocol);
                contractTitle = canonical.Title;

                foreach (var op in canonical.Operations)
                {
                    var statusCode = op.Method.ToUpperInvariant() == "POST" ? 201 : 200;
                    var body = BuildExampleBody(op.OutputFields, op.OperationId);
                    routes.Add(new MockRoute(op.Path, op.Method.ToUpperInvariant(), statusCode, body, "application/json"));
                }

                // Tentar extrair exemplos reais do spec JSON
                TryEnrichRoutesFromSpec(version.SpecContent, routes);

                if (routes.Count == 0)
                    routes.Add(new MockRoute("/api/v1/resource", "GET", 200, "{\"message\": \"mock response\"}", "application/json"));
            }
            catch
            {
                contractTitle = $"Contract {version.SemVer}";
                routes.Add(new MockRoute("/api/v1/resource", "GET", 200, "{\"message\": \"mock response\"}", "application/json"));
            }

            instructions = BuildInstructions(contractTitle, routes);
            return new Response(request.ContractVersionId, contractTitle, routes.AsReadOnly(), instructions);
        }

        private static string BuildExampleBody(
            IReadOnlyList<NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractSchemaElement> fields,
            string operationId)
        {
            if (fields.Count == 0)
                return $"{{\"id\": \"mock-{operationId.ToLowerInvariant().Replace(" ", "-")}\", \"status\": \"ok\"}}";

            var props = new Dictionary<string, object?>();
            foreach (var field in fields.Take(5))
            {
                props[field.Name] = field.DataType?.ToLowerInvariant() switch
                {
                    "string" => "string-value",
                    "integer" or "int" or "number" => 0,
                    "boolean" or "bool" => false,
                    "array" => new object[] { },
                    _ => null
                };
            }
            return JsonSerializer.Serialize(props, new JsonSerializerOptions { WriteIndented = false });
        }

        private static void TryEnrichRoutesFromSpec(string specContent, List<MockRoute> routes)
        {
            try
            {
                using var doc = JsonDocument.Parse(specContent);
                var root = doc.RootElement;

                if (!root.TryGetProperty("paths", out var pathsEl)) return;

                foreach (var pathProp in pathsEl.EnumerateObject())
                {
                    foreach (var methodProp in pathProp.Value.EnumerateObject())
                    {
                        var method = methodProp.Name.ToUpperInvariant();
                        if (!IsHttpMethod(method)) continue;

                        // Procura exemplos nas respostas
                        if (!methodProp.Value.TryGetProperty("responses", out var responses)) continue;

                        foreach (var responseProp in responses.EnumerateObject())
                        {
                            if (!int.TryParse(responseProp.Key, out var statusCode)) continue;
                            if (statusCode < 200 || statusCode >= 300) continue;

                            // Procura exemplo no content
                            var exampleBody = ExtractExampleFromResponse(responseProp.Value);
                            if (exampleBody is null) continue;

                            // Actualiza rota existente ou adiciona nova
                            var existing = routes.FirstOrDefault(r => r.Path == pathProp.Name && r.Method == method);
                            if (existing is not null)
                                routes[routes.IndexOf(existing)] = existing with { ResponseBody = exampleBody, StatusCode = statusCode };
                        }
                    }
                }
            }
            catch { /* Ignorar erros de parsing */ }
        }

        private static string? ExtractExampleFromResponse(JsonElement response)
        {
            try
            {
                if (!response.TryGetProperty("content", out var content)) return null;

                foreach (var contentTypeProp in content.EnumerateObject())
                {
                    if (!contentTypeProp.Value.TryGetProperty("examples", out var examples)) continue;
                    foreach (var ex in examples.EnumerateObject())
                    {
                        if (ex.Value.TryGetProperty("value", out var val))
                            return val.GetRawText();
                    }
                }

                foreach (var contentTypeProp in content.EnumerateObject())
                {
                    if (contentTypeProp.Value.TryGetProperty("example", out var ex))
                        return ex.GetRawText();
                }
            }
            catch { /* Ignorar */ }
            return null;
        }

        private static bool IsHttpMethod(string method)
            => method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "OPTIONS" or "HEAD";

        private static string BuildInstructions(string title, IReadOnlyList<MockRoute> routes)
            => $"Mock configuration generated for '{title}' with {routes.Count} route(s). " +
               "Configure your mock server (WireMock, MSW, Prism) with these stubs. " +
               "Each route defines the path, HTTP method, expected status code, response body, and content type.";
    }

    /// <summary>Rota de mock com path, método, status code, body e content type.</summary>
    public sealed record MockRoute(
        string Path,
        string Method,
        int StatusCode,
        string ResponseBody,
        string ContentType);

    /// <summary>Resposta da geração de configuração de mock.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string ContractTitle,
        IReadOnlyList<MockRoute> Routes,
        string Instructions);
}
