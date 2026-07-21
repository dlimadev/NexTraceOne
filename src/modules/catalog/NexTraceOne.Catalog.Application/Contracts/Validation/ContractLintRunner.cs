using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Validation;

/// <summary>
/// Motor de linting NATIVO de contratos do NexTraceOne. Aplica um conjunto de regras
/// estruturais determinísticas ao conteúdo do spec (sem dependência de um engine Spectral
/// externo) e produz issues + um resumo consolidado.
///
/// A origem dos issues é <c>"internal"</c>. Suporta specs JSON (OpenAPI/AsyncAPI); para
/// formatos não-JSON (WSDL/GraphQL SDL) devolve apenas um aviso informativo.
/// </summary>
public static class ContractLintRunner
{
    /// <summary>Resumo consolidado de validação (forma consumida pelo frontend).</summary>
    public sealed record ValidationSummaryDto(
        int TotalIssues,
        int ErrorCount,
        int WarningCount,
        int InfoCount,
        int HintCount,
        int BlockedCount,
        bool IsPublishReady,
        bool IsReviewReady,
        IReadOnlyList<string> Sources,
        DateTimeOffset ValidatedAt,
        string Fingerprint,
        string OverallStatus);

    /// <summary>Resultado completo do linting: issues + resumo.</summary>
    public sealed record LintResult(
        IReadOnlyList<ValidationIssue> Issues,
        ValidationSummaryDto Summary);

    /// <summary>Executa o linting nativo sobre o spec e devolve issues + resumo.</summary>
    public static LintResult Run(string specContent, string format, DateTimeOffset validatedAt)
    {
        var issues = Lint(specContent, format);
        var summary = Summarize(issues, specContent, validatedAt);
        return new LintResult(issues, summary);
    }

    /// <summary>Constrói apenas o resumo (reutiliza <see cref="Run"/> internamente).</summary>
    public static ValidationSummaryDto Summarize(string specContent, string format, DateTimeOffset validatedAt)
        => Summarize(Lint(specContent, format), specContent, validatedAt);

    private static IReadOnlyList<ValidationIssue> Lint(string specContent, string format)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(specContent))
        {
            issues.Add(Issue("spec-not-empty", "Spec content present", ValidationSeverity.Error,
                "O conteúdo do contrato está vazio.", "$"));
            return issues;
        }

        // Formatos não-JSON: linting estrutural não aplicável.
        var normalizedFormat = (format ?? string.Empty).ToLowerInvariant();
        if (normalizedFormat is "wsdl" or "xml" or "graphql" or "sdl" or "proto")
        {
            issues.Add(Issue("lint-format", "Structural lint availability", ValidationSeverity.Info,
                $"Linting estrutural nativo não está disponível para o formato '{format}'.", "$"));
            return issues;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(specContent);
        }
        catch (JsonException)
        {
            issues.Add(Issue("spec-valid-json", "Spec is valid JSON", ValidationSeverity.Error,
                "O conteúdo do contrato não é JSON válido.", "$"));
            return issues;
        }

        using (doc)
        {
            var root = doc.RootElement;

            var info = TryGet(root, "info");
            if (info is null || TryGetString(info.Value, "title") is null or "")
                issues.Add(Issue("info-title", "API title is defined", ValidationSeverity.Error,
                    "Falta 'info.title' — todo o contrato deve declarar um título.", "$.info.title",
                    suggestedFix: "Adicione um título descritivo em info.title."));

            if (info is null || TryGetString(info.Value, "version") is null or "")
                issues.Add(Issue("info-version", "API version is defined", ValidationSeverity.Warning,
                    "Falta 'info.version' — declare a versão do contrato.", "$.info.version"));

            if (info is not null && TryGetString(info.Value, "description") is null or "")
                issues.Add(Issue("info-description", "API has a description", ValidationSeverity.Info,
                    "Recomenda-se 'info.description' para documentar o propósito da API.", "$.info.description"));

            // Servidores / host.
            var servers = TryGet(root, "servers");
            var host = TryGet(root, "host");
            if (servers is null && host is null)
            {
                issues.Add(Issue("servers-defined", "Servers are declared", ValidationSeverity.Hint,
                    "Nenhum servidor declarado — considere adicionar 'servers'.", "$.servers"));
            }
            else if (servers is { ValueKind: JsonValueKind.Array })
            {
                foreach (var server in servers.Value.EnumerateArray())
                {
                    var url = TryGetString(server, "url");
                    if (url is not null && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                        issues.Add(Issue("no-http-server", "Servers use HTTPS", ValidationSeverity.Warning,
                            $"O servidor '{url}' usa HTTP inseguro — prefira HTTPS.", "$.servers[*].url",
                            suggestedFix: "Use uma URL https://."));
                }
            }

            // Operações / canais.
            var paths = TryGet(root, "paths");
            var channels = TryGet(root, "channels");
            if (paths is null && channels is null)
                issues.Add(Issue("operations-defined", "Operations or channels declared", ValidationSeverity.Warning,
                    "O contrato não declara 'paths' (REST) nem 'channels' (eventos).", "$"));

            // Schemas reutilizáveis.
            var components = TryGet(root, "components");
            var hasSchemas = components is not null && TryGet(components.Value, "schemas") is not null;
            var hasDefinitions = TryGet(root, "definitions") is not null;
            if (!hasSchemas && !hasDefinitions)
                issues.Add(Issue("reusable-schemas", "Reusable schemas declared", ValidationSeverity.Hint,
                    "Nenhum schema reutilizável declarado (components.schemas / definitions).", "$.components.schemas"));
        }

        return issues;
    }

    private static ValidationSummaryDto Summarize(
        IReadOnlyList<ValidationIssue> issues, string specContent, DateTimeOffset validatedAt)
    {
        var errorCount = issues.Count(i => i.Severity == ValidationSeverity.Error);
        var warningCount = issues.Count(i => i.Severity == ValidationSeverity.Warning);
        var infoCount = issues.Count(i => i.Severity == ValidationSeverity.Info);
        var hintCount = issues.Count(i => i.Severity == ValidationSeverity.Hint);
        var blockedCount = issues.Count(i => i.Severity == ValidationSeverity.Blocked);

        var isPublishReady = errorCount == 0 && blockedCount == 0;
        var isReviewReady = blockedCount == 0;
        var overallStatus = (errorCount + blockedCount) > 0 ? "Invalid"
            : (warningCount > 0 ? "Partial" : "Valid");

        return new ValidationSummaryDto(
            issues.Count, errorCount, warningCount, infoCount, hintCount, blockedCount,
            isPublishReady, isReviewReady, ["internal"], validatedAt,
            Fingerprint(specContent), overallStatus);
    }

    private static ValidationIssue Issue(
        string ruleId, string ruleName, ValidationSeverity severity, string message, string path,
        string? suggestedFix = null)
        => new(ruleId, ruleName, severity, message, path, Line: null, Column: null,
            Source: "internal", RulesetId: null, SuggestedFix: suggestedFix);

    private static JsonElement? TryGet(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
            ? value
            : null;

    private static string? TryGetString(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(property, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Fingerprint(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
