using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.EvaluateDesignGuidelines;

/// <summary>
/// Feature: EvaluateDesignGuidelines — avalia o spec de uma versão de contrato
/// contra directrizes de design de API pré-definidas.
/// Cobre: operationId, respostas 2xx, kebab-case nos paths, parâmetros documentados e uso de tags.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class EvaluateDesignGuidelines
{
    /// <summary>Query de avaliação de directrizes de design.</summary>
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
    /// Handler que avalia o spec contra directrizes de design e calcula um score de 0 a 100.
    /// Suporta specs JSON (OpenAPI/Swagger). Para YAML ou specs malformadas, retorna score zero.
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

            var violations = new List<GuidelineViolation>();

            if (string.IsNullOrWhiteSpace(version.SpecContent))
            {
                violations.Add(new GuidelineViolation(
                    "spec-not-empty", "Error", "#", "Spec content is empty — cannot evaluate design guidelines."));
                return new Response(request.ContractVersionId, 0, violations.AsReadOnly());
            }

            try
            {
                using var doc = JsonDocument.Parse(version.SpecContent);
                EvaluateSpec(doc.RootElement, violations);
            }
            catch
            {
                // YAML ou spec malformada: tenta via modelo canónico
                var canonical = CanonicalModelBuilder.Build(version.SpecContent, version.Protocol);
                EvaluateCanonical(canonical, violations);
            }

            var score = CalculateScore(violations);
            return new Response(request.ContractVersionId, score, violations.AsReadOnly());
        }

        private static void EvaluateSpec(JsonElement root, List<GuidelineViolation> violations)
        {
            if (!root.TryGetProperty("paths", out var paths))
            {
                violations.Add(new GuidelineViolation("paths-exist", "Error", "#", "No 'paths' section found in spec."));
                return;
            }

            bool hasTags = root.TryGetProperty("tags", out var globalTags) && globalTags.ValueKind == JsonValueKind.Array && globalTags.GetArrayLength() > 0;
            if (!hasTags)
                violations.Add(new GuidelineViolation("global-tags", "Warning", "#/tags", "No global tags defined. Tags help organise endpoints by resource."));

            foreach (var pathProp in paths.EnumerateObject())
            {
                var path = pathProp.Name;

                // Regra 3: Paths usam kebab-case
                if (!IsKebabCase(path))
                    violations.Add(new GuidelineViolation(
                        "kebab-case-path", "Warning", $"#/paths/{path}", $"Path '{path}' does not follow kebab-case convention (lowercase with hyphens)."));

                foreach (var methodProp in pathProp.Value.EnumerateObject())
                {
                    var method = methodProp.Name.ToUpperInvariant();
                    if (!IsHttpMethod(method)) continue;
                    var op = methodProp.Value;
                    var location = $"#/paths/{path}/{methodProp.Name}";

                    // Regra 1: Todos os endpoints têm operationId
                    if (!op.TryGetProperty("operationId", out var opId) || string.IsNullOrWhiteSpace(opId.GetString()))
                        violations.Add(new GuidelineViolation(
                            "operation-id-required", "Error", location, $"Operation {method} {path} is missing 'operationId'."));

                    // Regra 2: Todos os endpoints têm ao menos uma resposta 2xx
                    if (!HasSuccessResponse(op))
                        violations.Add(new GuidelineViolation(
                            "success-response-required", "Error", location, $"Operation {method} {path} has no 2xx response defined."));

                    // Regra 5: Parâmetros têm descrição
                    if (op.TryGetProperty("parameters", out var parameters))
                        CheckParameterDescriptions(parameters, path, method, violations);

                    // Regra 6: Tags são usadas para agrupamento
                    if (!op.TryGetProperty("tags", out var tags) || tags.GetArrayLength() == 0)
                        violations.Add(new GuidelineViolation(
                            "operation-tags-required", "Info", location, $"Operation {method} {path} has no tags. Tags improve discoverability."));
                }
            }

            // Regra 4: Response bodies com estrutura consistente (verifica se há schemas definidos)
            CheckResponseBodyConsistency(root, violations);
        }

        private static void EvaluateCanonical(
            NexTraceOne.Catalog.Domain.Contracts.ValueObjects.ContractCanonicalModel canonical,
            List<GuidelineViolation> violations)
        {
            if (canonical.Operations.Count == 0)
            {
                violations.Add(new GuidelineViolation("operations-exist", "Warning", "#", "No operations found in spec."));
                return;
            }

            foreach (var op in canonical.Operations)
            {
                var location = $"{op.Method} {op.Path}";

                if (string.IsNullOrWhiteSpace(op.OperationId))
                    violations.Add(new GuidelineViolation("operation-id-required", "Error", location, $"Operation {location} is missing operationId."));

                if (!IsKebabCase(op.Path))
                    violations.Add(new GuidelineViolation("kebab-case-path", "Warning", location, $"Path '{op.Path}' does not follow kebab-case convention."));

                if (op.Tags == null || op.Tags.Count == 0)
                    violations.Add(new GuidelineViolation("operation-tags-required", "Info", location, $"Operation {location} has no tags."));

                foreach (var param in op.InputParameters)
                {
                    if (string.IsNullOrWhiteSpace(param.Description))
                        violations.Add(new GuidelineViolation(
                            "parameter-description-required", "Info", $"{location}/parameters/{param.Name}", $"Parameter '{param.Name}' has no description."));
                }
            }

            if (!canonical.HasDescriptions)
                violations.Add(new GuidelineViolation("descriptions-recommended", "Info", "#", "No descriptions found in operations. Descriptions improve documentation quality."));
        }

        private static bool HasSuccessResponse(JsonElement operation)
        {
            if (!operation.TryGetProperty("responses", out var responses)) return false;
            foreach (var r in responses.EnumerateObject())
            {
                if (int.TryParse(r.Key, out var code) && code >= 200 && code < 300) return true;
                if (r.Key == "2XX" || r.Key == "default") return true;
            }
            return false;
        }

        private static void CheckParameterDescriptions(
            JsonElement parameters, string path, string method, List<GuidelineViolation> violations)
        {
            foreach (var param in parameters.EnumerateArray())
            {
                if (!param.TryGetProperty("name", out var name)) continue;
                if (!param.TryGetProperty("description", out var desc) || string.IsNullOrWhiteSpace(desc.GetString()))
                    violations.Add(new GuidelineViolation(
                        "parameter-description-required", "Info",
                        $"#/paths/{path}/{method.ToLowerInvariant()}/parameters/{name.GetString()}",
                        $"Parameter '{name.GetString()}' in {method} {path} has no description."));
            }
        }

        private static void CheckResponseBodyConsistency(JsonElement root, List<GuidelineViolation> violations)
        {
            if (!root.TryGetProperty("components", out var components)) return;
            if (!components.TryGetProperty("schemas", out var schemas)) return;
            if (!schemas.EnumerateObject().Any())
                violations.Add(new GuidelineViolation(
                    "schemas-defined", "Info", "#/components/schemas", "No reusable schemas defined in components. Defining schemas promotes consistency."));
        }

        private static bool IsKebabCase(string path)
        {
            // Ignora path params como {id} e query strings
            var segments = path.Split('/').Where(s => !s.StartsWith('{') && !string.IsNullOrEmpty(s));
            return segments.All(s => s == s.ToLowerInvariant() && !s.Contains('_') && !s.Any(char.IsUpper));
        }

        private static bool IsHttpMethod(string m)
            => m is "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "OPTIONS" or "HEAD";

        private static int CalculateScore(List<GuidelineViolation> violations)
        {
            if (violations.Count == 0) return 100;

            var errorWeight = violations.Count(v => v.Severity == "Error") * 15;
            var warnWeight = violations.Count(v => v.Severity == "Warning") * 5;
            var infoWeight = violations.Count(v => v.Severity == "Info") * 2;

            var deduction = errorWeight + warnWeight + infoWeight;
            return Math.Max(0, 100 - deduction);
        }
    }

    /// <summary>Violação de directriz de design detectada.</summary>
    public sealed record GuidelineViolation(
        string Rule,
        string Severity,
        string Location,
        string Message);

    /// <summary>Resposta da avaliação de directrizes de design.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        int Score,
        IReadOnlyList<GuidelineViolation> Violations);
}
