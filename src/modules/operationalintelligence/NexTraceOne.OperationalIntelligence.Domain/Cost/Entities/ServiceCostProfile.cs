using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

/// <summary>
/// Aggregate Root que representa o perfil geral de custo de um serviço.
/// Mantém o orçamento mensal, custo corrente e limiar de alerta,
/// permitindo monitoramento contínuo e detecção de desvios orçamentários.
/// </summary>
public sealed class ServiceCostProfile : AuditableEntity<ServiceCostProfileId>
{
    private ServiceCostProfile() { }

    /// <summary>Nome do serviço monitorado.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Ambiente do serviço (dev, staging, prod).</summary>
    public string Environment { get; private set; } = string.Empty;

    /// <summary>Orçamento mensal definido para este serviço. Nulo indica sem orçamento definido.</summary>
    public decimal? MonthlyBudget { get; private set; }

    /// <summary>Custo acumulado no mês corrente.</summary>
    public decimal CurrentMonthCost { get; private set; }

    /// <summary>
    /// Percentual do orçamento a partir do qual alertas de custo são emitidos (0–100).
    /// Ex: 80 significa que alertas são disparados quando 80% do orçamento for atingido.
    /// </summary>
    public decimal AlertThresholdPercent { get; private set; }

    /// <summary>Indica se o custo corrente ultrapassou o orçamento mensal definido.</summary>
    public bool IsOverBudget => MonthlyBudget.HasValue && CurrentMonthCost > MonthlyBudget.Value;

    /// <summary>Data/hora UTC da última atualização de custo deste perfil.</summary>
    public DateTimeOffset LastUpdatedAt { get; private set; }

    /// <summary>
    /// Cria um novo perfil de custo para um serviço e ambiente.
    /// O limiar de alerta deve estar entre 0 e 100 (percentual).
    /// </summary>
    public static ServiceCostProfile Create(
        string serviceName,
        string environment,
        decimal alertThresholdPercent,
        DateTimeOffset now,
        decimal? monthlyBudget = null)
    {
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.NullOrWhiteSpace(environment);
        Guard.Against.OutOfRange(alertThresholdPercent, nameof(alertThresholdPercent), 0m, 100m);

        if (monthlyBudget.HasValue)
            Guard.Against.Negative(monthlyBudget.Value);

        return new ServiceCostProfile
        {
            Id = ServiceCostProfileId.New(),
            ServiceName = serviceName,
            Environment = environment,
            MonthlyBudget = monthlyBudget,
            CurrentMonthCost = 0m,
            AlertThresholdPercent = alertThresholdPercent,
            LastUpdatedAt = now
        };
    }

    /// <summary>
    /// Atualiza o custo acumulado do mês corrente.
    /// Retorna erro se o valor informado for negativo.
    /// </summary>
    public Result<Unit> UpdateCurrentCost(decimal newCost, DateTimeOffset now)
    {
        if (newCost < 0)
            return CostIntelligenceErrors.NegativeCost(newCost);

        CurrentMonthCost = newCost;
        LastUpdatedAt = now;
        return Unit.Value;
    }

    /// <summary>
    /// Define ou atualiza o orçamento mensal do serviço.
    /// Passar null remove o orçamento (serviço sem limite definido).
    /// Retorna erro se o valor informado for negativo.
    /// </summary>
    public Result<Unit> SetBudget(decimal? budget, DateTimeOffset now)
    {
        if (budget.HasValue && budget.Value < 0)
            return CostIntelligenceErrors.NegativeCost(budget.Value);

        MonthlyBudget = budget;
        LastUpdatedAt = now;
        return Unit.Value;
    }

    /// <summary>
    /// Verifica se o custo corrente atingiu ou ultrapassou o limiar de alerta do orçamento.
    /// Retorna erro de negócio <see cref="CostIntelligenceErrors.BudgetExceeded"/> quando o
    /// orçamento é excedido, permitindo ao handler decidir a ação (notificação, evento, etc.).
    /// Retorna sucesso se não houver orçamento definido ou se o limiar não foi atingido.
    /// </summary>
    public Result<Unit> CheckBudgetAlert()
    {
        if (!MonthlyBudget.HasValue)
            return Unit.Value;

        var thresholdAmount = MonthlyBudget.Value * (AlertThresholdPercent / 100m);

        if (CurrentMonthCost >= thresholdAmount)
            return CostIntelligenceErrors.BudgetExceeded(ServiceName, CurrentMonthCost, MonthlyBudget.Value);

        return Unit.Value;
    }

    /// <summary>
    /// Percentual do orçamento consumido no mês corrente.
    /// Retorna null se nenhum orçamento estiver definido.
    /// Permite visualização rápida do consumo relativo ao limite.
    /// </summary>
    public decimal? BudgetUsagePercent =>
        MonthlyBudget.HasValue && MonthlyBudget.Value > 0m
            ? Math.Round(CurrentMonthCost / MonthlyBudget.Value * 100m, 2)
            : null;

    /// <summary>
    /// Reseta o custo acumulado para um novo ciclo mensal.
    /// Deve ser chamado pelo job de virada de mês para limpar o acúmulo anterior.
    /// </summary>
    public void ResetMonthlyCycle(DateTimeOffset now)
    {
        CurrentMonthCost = 0m;
        LastUpdatedAt = now;
    }

    /// <summary>
    /// Atualiza o limiar de alerta de custo (percentual do orçamento).
    /// O valor deve estar entre 0 e 100.
    /// </summary>
    public Result<Unit> UpdateAlertThreshold(decimal newThresholdPercent, DateTimeOffset now)
    {
        if (newThresholdPercent < 0m || newThresholdPercent > 100m)
            return CostIntelligenceErrors.NegativeCost(newThresholdPercent);

        AlertThresholdPercent = newThresholdPercent;
        LastUpdatedAt = now;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de ServiceCostProfile.</summary>
public sealed record ServiceCostProfileId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceCostProfileId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceCostProfileId From(Guid id) => new(id);
}
