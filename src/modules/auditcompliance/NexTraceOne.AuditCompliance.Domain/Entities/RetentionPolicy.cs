using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Audit.Domain.Entities;

/// <summary>
/// Política de retenção de eventos de auditoria configurável por tenant.
/// </summary>
public sealed class RetentionPolicy : Entity<RetentionPolicyId>
{
    private RetentionPolicy() { }

    /// <summary>Nome da política de retenção.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Dias de retenção dos eventos.</summary>
    public int RetentionDays { get; private set; }

    /// <summary>Indica se a política está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Cria uma nova política de retenção.</summary>
    public static RetentionPolicy Create(string name, int retentionDays)
    {
        if (retentionDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention days must be greater than zero.");
        }

        return new RetentionPolicy
        {
            Id = RetentionPolicyId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            RetentionDays = retentionDays,
            IsActive = true
        };
    }

    /// <summary>Atualiza o período de retenção.</summary>
    public void UpdateRetention(int retentionDays)
    {
        if (retentionDays <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention days must be greater than zero.");
        }

        RetentionDays = retentionDays;
    }

    /// <summary>Desativa a política.</summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>Identificador fortemente tipado de RetentionPolicy.</summary>
public sealed record RetentionPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RetentionPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RetentionPolicyId From(Guid id) => new(id);
}
