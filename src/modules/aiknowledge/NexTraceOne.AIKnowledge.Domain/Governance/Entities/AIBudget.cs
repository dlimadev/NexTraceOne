using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa um budget/quota de tokens e requisições de IA para um escopo específico.
/// Permite controlo granular de consumo por utilizador, grupo, equipa ou papel,
/// com períodos configuráveis (diário, semanal, mensal) e contadores acumulados.
///
/// Invariantes:
/// - MaxTokens e MaxRequests devem ser positivos.
/// - Contadores de uso são incrementais dentro do período.
/// - Reset do período zera os contadores acumulados.
/// - Budget inicia sempre ativo.
/// </summary>
public sealed class AIBudget : AuditableEntity<AIBudgetId>
{
    private AIBudget() { }

    /// <summary>Nome identificador do budget.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Tipo de escopo do budget (ex: "user", "group", "team", "role").</summary>
    public string Scope { get; private set; } = string.Empty;

    /// <summary>Valor do escopo — identifica a entidade à qual o budget se aplica.</summary>
    public string ScopeValue { get; private set; } = string.Empty;

    /// <summary>Período de contabilização do budget (diário, semanal, mensal).</summary>
    public BudgetPeriod Period { get; private set; }

    /// <summary>Limite máximo de tokens permitidos no período.</summary>
    public long MaxTokens { get; private set; }

    /// <summary>Limite máximo de requisições permitidas no período.</summary>
    public int MaxRequests { get; private set; }

    /// <summary>Total de tokens consumidos no período atual.</summary>
    public long CurrentTokensUsed { get; private set; }

    /// <summary>Total de requisições realizadas no período atual.</summary>
    public int CurrentRequestCount { get; private set; }

    /// <summary>Data/hora UTC de início do período atual de contabilização.</summary>
    public DateTimeOffset PeriodStartDate { get; private set; }

    /// <summary>Indica se o budget está ativo e sendo avaliado.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica se a quota de tokens ou requisições foi excedida no período atual.</summary>
    public bool IsQuotaExceeded => CurrentTokensUsed >= MaxTokens || CurrentRequestCount >= MaxRequests;

    /// <summary>
    /// Cria um novo budget de IA com validações de invariantes.
    /// O budget inicia ativo com contadores zerados.
    /// </summary>
    public static AIBudget Create(
        string name,
        string scope,
        string scopeValue,
        BudgetPeriod period,
        long maxTokens,
        int maxRequests,
        DateTimeOffset periodStartDate)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(scope);
        Guard.Against.NullOrWhiteSpace(scopeValue);
        Guard.Against.NegativeOrZero(maxTokens);
        Guard.Against.NegativeOrZero(maxRequests);

        return new AIBudget
        {
            Id = AIBudgetId.New(),
            Name = name,
            Scope = scope,
            ScopeValue = scopeValue,
            Period = period,
            MaxTokens = maxTokens,
            MaxRequests = maxRequests,
            CurrentTokensUsed = 0,
            CurrentRequestCount = 0,
            PeriodStartDate = periodStartDate,
            IsActive = true
        };
    }

    /// <summary>
    /// Regista o consumo de tokens de uma requisição.
    /// Incrementa os contadores de tokens e requisições.
    /// Retorna erro se a quota for excedida após o registo.
    /// </summary>
    public Result<Unit> RecordUsage(long tokensUsed)
    {
        Guard.Against.Negative(tokensUsed);

        if (IsQuotaExceeded)
            return AiGovernanceErrors.QuotaExceeded(Scope, ScopeValue);

        CurrentTokensUsed += tokensUsed;
        CurrentRequestCount++;

        return Unit.Value;
    }

    /// <summary>
    /// Reseta o período de contabilização, zerando todos os contadores acumulados.
    /// Utilizado no início de um novo período (diário, semanal ou mensal).
    /// </summary>
    public Result<Unit> ResetPeriod(DateTimeOffset newStart)
    {
        PeriodStartDate = newStart;
        CurrentTokensUsed = 0;
        CurrentRequestCount = 0;
        return Unit.Value;
    }

    /// <summary>
    /// Atualiza os limites e período do budget.
    /// Permite ajustar tokens máximos, requisições máximas e período de contabilização.
    /// </summary>
    public Result<Unit> Update(long maxTokens, int maxRequests, BudgetPeriod period)
    {
        Guard.Against.NegativeOrZero(maxTokens);
        Guard.Against.NegativeOrZero(maxRequests);

        MaxTokens = maxTokens;
        MaxRequests = maxRequests;
        Period = period;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de AIBudget.</summary>
public sealed record AIBudgetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AIBudgetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AIBudgetId From(Guid id) => new(id);
}
