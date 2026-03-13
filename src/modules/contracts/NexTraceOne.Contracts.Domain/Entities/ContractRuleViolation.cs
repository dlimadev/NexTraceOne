using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Entidade que registra uma violação de ruleset detectada em uma versão de contrato.
/// Cada violação representa uma regra organizacional não cumprida, com severidade,
/// localização no contrato e sugestão de correção quando aplicável.
/// Armazenada como coleção filha do ContractVersion.
/// </summary>
public sealed class ContractRuleViolation : Entity<ContractRuleViolationId>
{
    private ContractRuleViolation() { }

    /// <summary>Identificador da versão de contrato onde a violação foi encontrada.</summary>
    public ContractVersionId ContractVersionId { get; private set; } = null!;

    /// <summary>Identificador do ruleset que gerou a violação.</summary>
    public Guid RulesetId { get; private set; }

    /// <summary>Nome da regra violada (ex: "naming-convention", "required-examples").</summary>
    public string RuleName { get; private set; } = string.Empty;

    /// <summary>Severidade da violação: Error, Warning, Info, Hint.</summary>
    public string Severity { get; private set; } = string.Empty;

    /// <summary>Mensagem descritiva da violação (em inglês para logs e processamento).</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Caminho no contrato onde a violação ocorre (ex: "/paths/~1users/get").</summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>Sugestão de correção, quando disponível.</summary>
    public string? SuggestedFix { get; private set; }

    /// <summary>Timestamp de quando a violação foi detectada.</summary>
    public DateTimeOffset DetectedAt { get; private set; }

    /// <summary>
    /// Cria uma nova violação de ruleset para uma versão de contrato.
    /// </summary>
    public static ContractRuleViolation Create(
        ContractVersionId contractVersionId,
        Guid rulesetId,
        string ruleName,
        string severity,
        string message,
        string path,
        DateTimeOffset detectedAt,
        string? suggestedFix = null)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.Default(rulesetId);
        Guard.Against.NullOrWhiteSpace(ruleName);
        Guard.Against.NullOrWhiteSpace(severity);
        Guard.Against.NullOrWhiteSpace(message);

        return new ContractRuleViolation
        {
            Id = ContractRuleViolationId.New(),
            ContractVersionId = contractVersionId,
            RulesetId = rulesetId,
            RuleName = ruleName,
            Severity = severity,
            Message = message,
            Path = path ?? string.Empty,
            SuggestedFix = suggestedFix,
            DetectedAt = detectedAt
        };
    }
}

/// <summary>Identificador fortemente tipado de ContractRuleViolation.</summary>
public sealed record ContractRuleViolationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractRuleViolationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractRuleViolationId From(Guid id) => new(id);
}
