using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// Representa uma estratégia de roteamento de IA configurada na plataforma.
/// Define como consultas devem ser direcionadas com base em persona, caso de uso,
/// tipo de cliente, sensibilidade e política de acesso.
///
/// Invariantes:
/// - Nome é obrigatório e único por convenção.
/// - Cada estratégia mapeia um caminho de roteamento preferencial.
/// - Estratégia inicia sempre ativa.
/// - Prioridade menor indica maior precedência na avaliação.
/// </summary>
public sealed class AIRoutingStrategy : AuditableEntity<AIRoutingStrategyId>
{
    private AIRoutingStrategy() { }

    /// <summary>Nome identificador da estratégia de roteamento.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e comportamento da estratégia.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Persona alvo (ou "*" para todas as personas).</summary>
    public string TargetPersona { get; private set; } = string.Empty;

    /// <summary>Caso de uso alvo (ou "*" para todos os casos).</summary>
    public string TargetUseCase { get; private set; } = string.Empty;

    /// <summary>Tipo de cliente alvo (ou "*" para todos os clientes).</summary>
    public string TargetClientType { get; private set; } = string.Empty;

    /// <summary>Caminho de roteamento preferencial definido pela estratégia.</summary>
    public AIRoutingPath PreferredPath { get; private set; }

    /// <summary>Nível máximo de sensibilidade permitido (1-5).</summary>
    public int MaxSensitivityLevel { get; private set; }

    /// <summary>Indica se escalonamento externo é permitido por esta estratégia.</summary>
    public bool AllowExternalEscalation { get; private set; }

    /// <summary>Indica se a estratégia está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Prioridade de avaliação — menor valor indica maior precedência.</summary>
    public int Priority { get; private set; }

    /// <summary>Data/hora UTC em que a estratégia foi registada.</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>
    /// Cria uma nova estratégia de roteamento com validações de invariantes.
    /// </summary>
    public static AIRoutingStrategy Create(
        string name,
        string description,
        string targetPersona,
        string targetUseCase,
        string targetClientType,
        AIRoutingPath preferredPath,
        int maxSensitivityLevel,
        bool allowExternalEscalation,
        int priority,
        DateTimeOffset registeredAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.NullOrWhiteSpace(targetPersona);
        Guard.Against.NullOrWhiteSpace(targetUseCase);
        Guard.Against.NullOrWhiteSpace(targetClientType);
        Guard.Against.OutOfRange(maxSensitivityLevel, nameof(maxSensitivityLevel), 1, 5);
        Guard.Against.Negative(priority);

        return new AIRoutingStrategy
        {
            Id = AIRoutingStrategyId.New(),
            Name = name,
            Description = description,
            TargetPersona = targetPersona,
            TargetUseCase = targetUseCase,
            TargetClientType = targetClientType,
            PreferredPath = preferredPath,
            MaxSensitivityLevel = maxSensitivityLevel,
            AllowExternalEscalation = allowExternalEscalation,
            IsActive = true,
            Priority = priority,
            RegisteredAt = registeredAt
        };
    }

    /// <summary>Ativa a estratégia. Operação idempotente.</summary>
    public Result<Unit> Activate()
    {
        IsActive = true;
        return Unit.Value;
    }

    /// <summary>Desativa a estratégia. Operação idempotente.</summary>
    public Result<Unit> Deactivate()
    {
        IsActive = false;
        return Unit.Value;
    }

    /// <summary>Atualiza a descrição e configurações da estratégia.</summary>
    public Result<Unit> Update(
        string description,
        AIRoutingPath preferredPath,
        int maxSensitivityLevel,
        bool allowExternalEscalation,
        int priority)
    {
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.OutOfRange(maxSensitivityLevel, nameof(maxSensitivityLevel), 1, 5);
        Guard.Against.Negative(priority);

        Description = description;
        PreferredPath = preferredPath;
        MaxSensitivityLevel = maxSensitivityLevel;
        AllowExternalEscalation = allowExternalEscalation;
        Priority = priority;
        return Unit.Value;
    }

    /// <summary>
    /// Verifica se a estratégia é aplicável ao contexto fornecido.
    /// Wildcard "*" indica que a estratégia se aplica a qualquer valor.
    /// </summary>
    public bool IsApplicable(string persona, string useCase, string clientType)
    {
        var personaMatch = TargetPersona == "*" || string.Equals(TargetPersona, persona, StringComparison.OrdinalIgnoreCase);
        var useCaseMatch = TargetUseCase == "*" || string.Equals(TargetUseCase, useCase, StringComparison.OrdinalIgnoreCase);
        var clientMatch = TargetClientType == "*" || string.Equals(TargetClientType, clientType, StringComparison.OrdinalIgnoreCase);
        return personaMatch && useCaseMatch && clientMatch;
    }
}

/// <summary>Identificador fortemente tipado de AIRoutingStrategy.</summary>
public sealed record AIRoutingStrategyId(Guid Value) : TypedIdBase(Value)
{
    public static AIRoutingStrategyId New() => new(Guid.NewGuid());
    public static AIRoutingStrategyId From(Guid id) => new(id);
}
