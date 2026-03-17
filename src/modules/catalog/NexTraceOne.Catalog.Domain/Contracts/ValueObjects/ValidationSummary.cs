using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Resumo consolidado de uma execução de validação sobre um contrato.
/// Agrega contagens por severidade e indica se a publicação está bloqueada.
/// Usado como snapshot rápido no studio, portal e governança.
/// </summary>
public sealed record ValidationSummary(
    /// <summary>Total de issues detectados.</summary>
    int TotalIssues,
    /// <summary>Número de erros.</summary>
    int ErrorCount,
    /// <summary>Número de avisos.</summary>
    int WarningCount,
    /// <summary>Número de informações.</summary>
    int InfoCount,
    /// <summary>Número de dicas.</summary>
    int HintCount,
    /// <summary>Número de issues bloqueantes.</summary>
    int BlockedCount,
    /// <summary>Indica se o contrato está pronto para publicação (sem issues bloqueantes).</summary>
    bool IsPublishReady,
    /// <summary>Indica se o contrato está pronto para revisão (sem issues bloqueantes de review).</summary>
    bool IsReviewReady,
    /// <summary>Fontes de validação que contribuíram (spectral, internal, canonical).</summary>
    IReadOnlyList<string> Sources,
    /// <summary>Timestamp da última execução de validação.</summary>
    DateTimeOffset ValidatedAt)
{
    /// <summary>Cria um summary vazio — sem issues detectados.</summary>
    public static ValidationSummary Empty() => new(0, 0, 0, 0, 0, 0, true, true, [], DateTimeOffset.UtcNow);

    /// <summary>Cria um summary a partir de uma lista de issues.</summary>
    public static ValidationSummary FromIssues(IReadOnlyList<ValidationIssue> issues, DateTimeOffset validatedAt)
    {
        var errors = issues.Count(i => i.Severity == ValidationSeverity.Error);
        var warnings = issues.Count(i => i.Severity == ValidationSeverity.Warning);
        var infos = issues.Count(i => i.Severity == ValidationSeverity.Info);
        var hints = issues.Count(i => i.Severity == ValidationSeverity.Hint);
        var blocked = issues.Count(i => i.Severity == ValidationSeverity.Blocked);
        var sources = issues.Select(i => i.Source).Distinct().ToList();

        return new ValidationSummary(
            issues.Count, errors, warnings, infos, hints, blocked,
            IsPublishReady: blocked == 0 && errors == 0,
            IsReviewReady: blocked == 0,
            sources,
            validatedAt);
    }
}
