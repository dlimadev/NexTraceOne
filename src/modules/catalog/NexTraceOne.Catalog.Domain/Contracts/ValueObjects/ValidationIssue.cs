using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

/// <summary>
/// Resultado individual de validação — unifica issues de Spectral, regras internas e checks de canonical.
/// Imutável por design; representa um problema detectado num ponto específico do contrato.
/// </summary>
public sealed record ValidationIssue(
    /// <summary>Identificador da regra (ex: "oas3-api-servers", "naming-convention").</summary>
    string RuleId,
    /// <summary>Nome legível da regra.</summary>
    string RuleName,
    /// <summary>Severidade do issue.</summary>
    ValidationSeverity Severity,
    /// <summary>Mensagem descritiva do problema.</summary>
    string Message,
    /// <summary>Caminho no contrato onde o issue ocorre (JSON path, XPath ou campo).</summary>
    string Path,
    /// <summary>Número da linha no spec content, quando disponível (1-based).</summary>
    int? Line,
    /// <summary>Número da coluna, quando disponível (1-based).</summary>
    int? Column,
    /// <summary>Fonte da validação: "spectral", "internal", "canonical", "schema".</summary>
    string Source,
    /// <summary>Identificador do ruleset que gerou o issue, quando aplicável.</summary>
    Guid? RulesetId,
    /// <summary>Sugestão de correção, quando disponível.</summary>
    string? SuggestedFix = null);
