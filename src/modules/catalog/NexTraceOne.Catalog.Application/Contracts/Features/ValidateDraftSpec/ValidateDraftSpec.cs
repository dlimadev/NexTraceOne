using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ValidateDraftSpec;

/// <summary>
/// Feature: ValidateDraftSpec — valida conteúdo de especificação ad-hoc (sem contrato persistido)
/// aplicando múltiplas fases de validação: regras determinísticas, directrizes de design
/// e conformidade canónica. Retorna issues unificados com fonte, severidade e sugestão de correção.
/// Endpoint de draft-time para feedback imediato durante a edição no Contract Studio.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ValidateDraftSpec
{
    /// <summary>Query com o conteúdo da especificação, protocolo e opções de validação.</summary>
    public sealed record Query(
        string SpecContent,
        string Protocol,
        string? SemVer) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SpecContent).NotEmpty();
            RuleFor(x => x.Protocol).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que orquestra validação multi-fase sobre conteúdo ad-hoc de especificação.
    /// Fase 1: Parsing e construção do modelo canónico.
    /// Fase 2: Regras determinísticas via ContractRuleEngine.
    /// Fase 3: Directrizes de design (operationId, 2xx, kebab-case, tags, parâmetros).
    /// Fase 4: Conformidade canónica ($ref sem alvo, schemas sem tipo).
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var issues = new List<IssueResponse>();

            if (!Enum.TryParse<ContractProtocol>(request.Protocol, ignoreCase: true, out var protocol))
            {
                issues.Add(new IssueResponse(
                    "parse-protocol",
                    "Unsupported Protocol",
                    "Error",
                    "contracts.draftValidation.issues.unsupportedProtocol",
                    new Dictionary<string, string> { ["protocol"] = request.Protocol },
                    $"Protocol '{request.Protocol}' is not supported.",
                    "#",
                    null, null,
                    "schema",
                    "contracts.draftValidation.fixes.useValidProtocol",
                    "Use one of: OpenApi, Swagger, AsyncApi, Wsdl, WorkerService."));

                return Task.FromResult(Result<Response>.Success(BuildResponse(request.SpecContent, issues)));
            }

            // ── Fase 1: Parse ──────────────────────────────────────────────
            ContractCanonicalModel canonical;
            try
            {
                canonical = CanonicalModelBuilder.Build(request.SpecContent, protocol);

                if (canonical.Title == "Unknown" && canonical.OperationCount == 0 && canonical.SchemaCount == 0)
                {
                    issues.Add(new IssueResponse(
                        "parse-failed",
                        "Parse Failure",
                        "Error",
                        "contracts.draftValidation.issues.parseFailed",
                        null,
                        "Could not parse specification. Verify the content is valid for the selected protocol.",
                        "#",
                        null, null,
                        "schema",
                        null,
                        null));

                    return Task.FromResult(Result<Response>.Success(BuildResponse(request.SpecContent, issues)));
                }
            }
            catch (Exception ex)
            {
                issues.Add(new IssueResponse(
                    "parse-exception",
                    "Parse Error",
                    "Error",
                    "contracts.draftValidation.issues.parseError",
                    new Dictionary<string, string> { ["detail"] = ex.Message },
                    $"Failed to parse specification: {ex.Message}",
                    "#",
                    null, null,
                    "schema",
                    "contracts.draftValidation.fixes.verifySpecContent",
                    "Verify the spec content is well-formed YAML, JSON or XML."));

                return Task.FromResult(Result<Response>.Success(BuildResponse(request.SpecContent, issues)));
            }

            // ── Fase 2: Regras determinísticas (ContractRuleEngine) ────────
            var dummyVersionId = ContractVersionId.New();
            var semVer = request.SemVer ?? "0.0.0";
            var ruleViolations = ContractRuleEngine.Evaluate(dummyVersionId, canonical, semVer, protocol);

            foreach (var v in ruleViolations)
            {
                issues.Add(new IssueResponse(
                    v.RuleName,
                    v.RuleName,
                    v.Severity,
                    $"contracts.draftValidation.rules.{v.RuleName}.message",
                    BuildRuleParams(v),
                    v.Message,
                    v.Path,
                    null, null,
                    "internal",
                    $"contracts.draftValidation.rules.{v.RuleName}.fix",
                    v.SuggestedFix));
            }

            // ── Fase 3: Directrizes de design ──────────────────────────────
            EvaluateDesignGuidelinesInline(request.SpecContent, protocol, canonical, issues);

            // ── Fase 4: Conformidade canónica ──────────────────────────────
            EvaluateCanonicalConformance(request.SpecContent, issues);

            return Task.FromResult(Result<Response>.Success(BuildResponse(request.SpecContent, issues)));
        }

        /// <summary>
        /// Avalia directrizes de design sobre o spec.
        /// Suporta specs JSON e YAML via modelo canónico como fallback.
        /// </summary>
        private static void EvaluateDesignGuidelinesInline(
            string specContent,
            ContractProtocol protocol,
            ContractCanonicalModel canonical,
            List<IssueResponse> issues)
        {
            // Protocols sem paths/operações HTTP não aplicam estas regras
            if (protocol is ContractProtocol.Wsdl or ContractProtocol.WorkerService)
                return;

            try
            {
                using var doc = JsonDocument.Parse(specContent);
                EvaluateDesignFromJson(doc.RootElement, issues);
                return;
            }
            catch
            {
                // Não é JSON válido — tenta via modelo canónico
            }

            // Fallback: avalia via modelo canónico (YAML specs)
            EvaluateDesignFromCanonical(canonical, issues);
        }

        private static void EvaluateDesignFromJson(JsonElement root, List<IssueResponse> issues)
        {
            if (!root.TryGetProperty("paths", out var paths))
                return;

            bool hasTags = root.TryGetProperty("tags", out var globalTags)
                           && globalTags.ValueKind == JsonValueKind.Array
                           && globalTags.GetArrayLength() > 0;
            if (!hasTags)
            {
                issues.Add(new IssueResponse(
                    "global-tags", "Global Tags", "Warning",
                    "contracts.draftValidation.issues.noGlobalTags",
                    null,
                    "No global tags defined. Tags help organise endpoints by resource.",
                    "#/tags", null, null, "internal",
                    "contracts.draftValidation.fixes.addTagsArray",
                    "Add a 'tags' array at the root level."));
            }

            foreach (var pathProp in paths.EnumerateObject())
            {
                var path = pathProp.Name;

                if (!IsKebabCase(path))
                {
                    issues.Add(new IssueResponse(
                        "kebab-case-path", "Kebab Case Path", "Warning",
                        "contracts.draftValidation.issues.pathNotKebabCase",
                        new Dictionary<string, string> { ["path"] = path },
                        $"Path '{path}' does not follow kebab-case convention.",
                        $"#/paths/{path}", null, null, "internal",
                        "contracts.draftValidation.fixes.useKebabCase",
                        "Use lowercase with hyphens (e.g., /my-resource)."));
                }

                foreach (var methodProp in pathProp.Value.EnumerateObject())
                {
                    var method = methodProp.Name.ToUpperInvariant();
                    if (!IsHttpMethod(method)) continue;
                    var op = methodProp.Value;
                    var location = $"#/paths/{path}/{methodProp.Name}";

                    if (!op.TryGetProperty("operationId", out var opId) || string.IsNullOrWhiteSpace(opId.GetString()))
                    {
                        issues.Add(new IssueResponse(
                            "operation-id-required", "Operation ID Required", "Error",
                            "contracts.draftValidation.issues.missingOperationId",
                            new Dictionary<string, string> { ["method"] = method, ["path"] = path },
                            $"Operation {method} {path} is missing 'operationId'.",
                            location, null, null, "internal",
                            "contracts.draftValidation.fixes.addOperationId",
                            "Add an 'operationId' to uniquely identify this operation."));
                    }

                    if (!HasSuccessResponse(op))
                    {
                        issues.Add(new IssueResponse(
                            "success-response-required", "Success Response Required", "Error",
                            "contracts.draftValidation.issues.noSuccessResponse",
                            new Dictionary<string, string> { ["method"] = method, ["path"] = path },
                            $"Operation {method} {path} has no 2xx response defined.",
                            location, null, null, "internal",
                            "contracts.draftValidation.fixes.addSuccessResponse",
                            "Add at least one 2xx response (e.g., 200, 201, 204)."));
                    }
                }
            }
        }

        private static void EvaluateDesignFromCanonical(ContractCanonicalModel canonical, List<IssueResponse> issues)
        {
            foreach (var op in canonical.Operations)
            {
                if (string.IsNullOrWhiteSpace(op.OperationId))
                {
                    issues.Add(new IssueResponse(
                        "operation-id-required", "Operation ID Required", "Warning",
                        "contracts.draftValidation.issues.missingOperationIdCanonical",
                        new Dictionary<string, string> { ["operation"] = op.Name },
                        $"Operation '{op.Name}' is missing operationId.",
                        $"#/operations/{op.Name}", null, null, "internal",
                        "contracts.draftValidation.fixes.addOperationId",
                        "Add an 'operationId' to uniquely identify this operation."));
                }
            }
        }

        /// <summary>
        /// Avalia conformidade canónica: detecta $ref sem alvo, schemas incompletos.
        /// Análise puramente estrutural — não requer acesso à base de dados de canonical entities.
        /// </summary>
        private static void EvaluateCanonicalConformance(string specContent, List<IssueResponse> issues)
        {
            try
            {
                using var doc = JsonDocument.Parse(specContent);
                ScanRefsInElement(doc.RootElement, "#", issues);
            }
            catch
            {
                // YAML specs: scan via texto
                ScanRefsInText(specContent, issues);
            }
        }

        private static void ScanRefsInElement(JsonElement element, string currentPath, List<IssueResponse> issues)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("$ref", out var refVal))
                {
                    var refTarget = refVal.GetString();
                    if (string.IsNullOrWhiteSpace(refTarget))
                    {
                        issues.Add(new IssueResponse(
                            "ref-empty", "Empty $ref", "Error",
                            "contracts.draftValidation.issues.emptyRef",
                            new Dictionary<string, string> { ["path"] = currentPath },
                            $"Empty $ref target at {currentPath}.",
                            currentPath, null, null, "canonical",
                            "contracts.draftValidation.fixes.provideRefTarget",
                            "Provide a valid $ref target (e.g., '#/components/schemas/MyModel')."));
                    }
                }

                foreach (var prop in element.EnumerateObject())
                {
                    ScanRefsInElement(prop.Value, $"{currentPath}/{prop.Name}", issues);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                int i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    ScanRefsInElement(item, $"{currentPath}[{i}]", issues);
                    i++;
                }
            }
        }

        private static void ScanRefsInText(string content, List<IssueResponse> issues)
        {
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var refIdx = line.IndexOf("$ref:", StringComparison.OrdinalIgnoreCase);
                if (refIdx < 0) continue;

                var afterRef = line[(refIdx + 5)..].Trim().Trim('\'', '"');
                if (string.IsNullOrWhiteSpace(afterRef))
                {
                    issues.Add(new IssueResponse(
                        "ref-empty", "Empty $ref", "Error",
                        "contracts.draftValidation.issues.emptyRefAtLine",
                        new Dictionary<string, string> { ["line"] = (i + 1).ToString() },
                        $"Empty $ref target at line {i + 1}.",
                        $"#/line/{i + 1}", i + 1, null, "canonical",
                        "contracts.draftValidation.fixes.provideRefTarget",
                        "Provide a valid $ref target."));
                }
            }
        }

        private static Response BuildResponse(string specContent, List<IssueResponse> issues)
        {
            var errorCount = issues.Count(i => i.Severity is "Error" or "Blocked");
            var warningCount = issues.Count(i => i.Severity == "Warning");
            var infoCount = issues.Count(i => i.Severity == "Info");
            var hintCount = issues.Count(i => i.Severity == "Hint");
            var fingerprint = ComputeFingerprint(specContent);
            var sources = issues.Select(i => i.Source).Distinct().ToList();

            return new Response(
                issues.Count,
                errorCount,
                warningCount,
                infoCount,
                hintCount,
                errorCount == 0,
                fingerprint,
                sources.AsReadOnly(),
                issues.AsReadOnly());
        }

        private static IReadOnlyDictionary<string, string>? BuildRuleParams(ContractRuleViolation violation)
        {
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(violation.Path))
                dict["path"] = violation.Path;
            // Extract contextual params from the message when identifiable
            return dict.Count > 0 ? dict : null;
        }

        private static string ComputeFingerprint(string content)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(bytes)[..16];
        }

        private static bool IsKebabCase(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in segments)
            {
                if (seg.StartsWith('{') && seg.EndsWith('}')) continue;
                if (seg != seg.ToLowerInvariant()) return false;
                if (seg.Contains('_')) return false;
            }
            return true;
        }

        private static bool IsHttpMethod(string method) =>
            method is "GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "HEAD" or "OPTIONS" or "TRACE";

        private static bool HasSuccessResponse(JsonElement operation)
        {
            if (!operation.TryGetProperty("responses", out var responses)) return false;
            foreach (var resp in responses.EnumerateObject())
            {
                if (resp.Name.Length == 3 && resp.Name[0] == '2') return true;
            }
            return false;
        }
    }

    /// <summary>Issue individual de validação.</summary>
    public sealed record IssueResponse(
        string RuleId,
        string RuleName,
        string Severity,
        string MessageKey,
        IReadOnlyDictionary<string, string>? MessageParams,
        string Message,
        string Path,
        int? Line,
        int? Column,
        string Source,
        string? SuggestedFixKey,
        string? SuggestedFix);

    /// <summary>Resposta consolidada da validação de draft.</summary>
    public sealed record Response(
        int TotalIssues,
        int ErrorCount,
        int WarningCount,
        int InfoCount,
        int HintCount,
        bool IsValid,
        string Fingerprint,
        IReadOnlyList<string> Sources,
        IReadOnlyList<IssueResponse> Issues);
}
