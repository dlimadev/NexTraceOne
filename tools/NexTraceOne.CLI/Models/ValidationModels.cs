using System.Text.Json.Serialization;

namespace NexTraceOne.CLI.Models;

/// <summary>
/// Severidade de um issue de validação para uso local no CLI.
/// Alinhado com o domínio Catalog mas sem dependência direta.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ValidationSeverity
{
    Info = 0,
    Hint = 1,
    Warning = 2,
    Error = 3,
    Blocked = 4
}

/// <summary>
/// Resultado individual de validação de um manifesto de contrato.
/// </summary>
public sealed record ValidationIssue(
    string RuleId,
    string RuleName,
    ValidationSeverity Severity,
    string Message,
    string Path,
    string? SuggestedFix = null);

/// <summary>
/// Resumo consolidado de uma execução de validação.
/// </summary>
public sealed record ValidationSummary(
    int TotalIssues,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    int HintCount,
    int BlockedCount)
{
    public static ValidationSummary FromIssues(IReadOnlyList<ValidationIssue> issues) => new(
        issues.Count,
        issues.Count(i => i.Severity == ValidationSeverity.Error),
        issues.Count(i => i.Severity == ValidationSeverity.Warning),
        issues.Count(i => i.Severity == ValidationSeverity.Info),
        issues.Count(i => i.Severity == ValidationSeverity.Hint),
        issues.Count(i => i.Severity == ValidationSeverity.Blocked));
}
