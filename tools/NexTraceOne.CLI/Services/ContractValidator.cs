using System.Text.RegularExpressions;
using NexTraceOne.CLI.Models;

namespace NexTraceOne.CLI.Services;

/// <summary>
/// Lógica de validação de manifestos de contrato para uso offline no CLI.
/// Usa modelos locais de validação alinhados com o domínio Catalog.
/// </summary>
public sealed partial class ContractValidator
{
    private static readonly HashSet<string> _validTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "rest-api",
        "soap",
        "event-contract",
        "background-service"
    };

    private static readonly string[] _validHttpMethods =
    [
        "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE"
    ];

    public static IReadOnlyList<ValidationIssue> Validate(ContractManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var issues = new List<ValidationIssue>();

        ValidateRequiredFields(manifest, issues);
        ValidateType(manifest, issues);
        ValidateVersion(manifest, issues);
        ValidateEndpoints(manifest, issues);
        ValidateSchema(manifest, issues);

        return issues;
    }

    private static void ValidateRequiredFields(ContractManifest manifest, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            issues.Add(CreateIssue(
                "CLI001", "required-field-name", ValidationSeverity.Error,
                "Field 'name' is required.", "$.name"));
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            issues.Add(CreateIssue(
                "CLI002", "required-field-version", ValidationSeverity.Error,
                "Field 'version' is required.", "$.version"));
        }

        if (string.IsNullOrWhiteSpace(manifest.Type))
        {
            issues.Add(CreateIssue(
                "CLI003", "required-field-type", ValidationSeverity.Error,
                "Field 'type' is required.", "$.type"));
        }
    }

    private static void ValidateType(ContractManifest manifest, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(manifest.Type))
            return;

        if (!_validTypes.Contains(manifest.Type))
        {
            var allowed = string.Join(", ", _validTypes.Order());
            issues.Add(CreateIssue(
                "CLI004", "invalid-type", ValidationSeverity.Error,
                $"Field 'type' has invalid value '{manifest.Type}'. Allowed: {allowed}.",
                "$.type",
                suggestedFix: $"Use one of: {allowed}"));
        }
    }

    private static void ValidateVersion(ContractManifest manifest, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(manifest.Version))
            return;

        if (!SemverRegex().IsMatch(manifest.Version))
        {
            issues.Add(CreateIssue(
                "CLI005", "invalid-semver", ValidationSeverity.Error,
                $"Field 'version' value '{manifest.Version}' does not follow semantic versioning (e.g. 1.0.0).",
                "$.version",
                suggestedFix: "Use semantic versioning format: MAJOR.MINOR.PATCH (e.g. 1.0.0, 2.1.0-beta)"));
        }
    }

    private static void ValidateEndpoints(ContractManifest manifest, List<ValidationIssue> issues)
    {
        if (manifest.Endpoints is null or { Length: 0 })
            return;

        for (var i = 0; i < manifest.Endpoints.Length; i++)
        {
            var endpoint = manifest.Endpoints[i];
            var basePath = $"$.endpoints[{i}]";

            if (string.IsNullOrWhiteSpace(endpoint.Path))
            {
                issues.Add(CreateIssue(
                    "CLI006", "endpoint-missing-path", ValidationSeverity.Error,
                    $"Endpoint at index {i} is missing required field 'path'.",
                    $"{basePath}.path"));
            }

            if (string.IsNullOrWhiteSpace(endpoint.Method))
            {
                issues.Add(CreateIssue(
                    "CLI007", "endpoint-missing-method", ValidationSeverity.Error,
                    $"Endpoint at index {i} is missing required field 'method'.",
                    $"{basePath}.method"));
            }
            else if (!_validHttpMethods.Contains(endpoint.Method, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(CreateIssue(
                    "CLI008", "endpoint-invalid-method", ValidationSeverity.Warning,
                    $"Endpoint at index {i} has non-standard HTTP method '{endpoint.Method}'.",
                    $"{basePath}.method",
                    suggestedFix: $"Use one of: {string.Join(", ", _validHttpMethods)}"));
            }
        }
    }

    private static void ValidateSchema(ContractManifest manifest, List<ValidationIssue> issues)
    {
        if (manifest.Schema is null)
            return;

        if (string.IsNullOrWhiteSpace(manifest.Schema.Format))
        {
            issues.Add(CreateIssue(
                "CLI009", "schema-missing-format", ValidationSeverity.Warning,
                "Schema is present but missing 'format' field.",
                "$.schema.format",
                suggestedFix: "Add a 'format' field (e.g. 'openapi-3.1', 'asyncapi-2.6', 'wsdl')"));
        }
    }

    private static ValidationIssue CreateIssue(
        string ruleId,
        string ruleName,
        ValidationSeverity severity,
        string message,
        string path,
        int? line = null,
        int? column = null,
        string? suggestedFix = null) =>
        new(ruleId, ruleName, severity, message, path, suggestedFix);

    [GeneratedRegex(@"^\d+\.\d+\.\d+(-[\w.]+)?(\+[\w.]+)?$")]
    private static partial Regex SemverRegex();
}
