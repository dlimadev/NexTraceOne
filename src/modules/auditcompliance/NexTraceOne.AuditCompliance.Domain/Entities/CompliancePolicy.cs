using Ardalis.GuardClauses;

using NexTraceOne.AuditCompliance.Domain.Enums;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AuditCompliance.Domain.Entities;

/// <summary>
/// Entidade que representa uma política de compliance avaliável pela plataforma.
/// Cada política define critérios de avaliação, severidade e categoria.
/// </summary>
public sealed class CompliancePolicy : Entity<CompliancePolicyId>
{
    private CompliancePolicy() { }

    /// <summary>Nome interno da política.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de exibição da política.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição opcional da política.</summary>
    public string? Description { get; private set; }

    /// <summary>Categoria da política (e.g. Security, DataProtection, Operational, Governance).</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Severidade da política.</summary>
    public ComplianceSeverity Severity { get; private set; }

    /// <summary>Indica se a política está ativa.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Critérios de avaliação em formato JSON.</summary>
    public string? EvaluationCriteria { get; private set; }

    /// <summary>Tenant proprietário da política.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Data/hora de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Cria uma nova política de compliance.</summary>
    public static CompliancePolicy Create(
        string name,
        string displayName,
        string? description,
        string category,
        ComplianceSeverity severity,
        string? evaluationCriteria,
        Guid tenantId,
        DateTimeOffset createdAt)
    {
        return new CompliancePolicy
        {
            Id = CompliancePolicyId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            DisplayName = Guard.Against.NullOrWhiteSpace(displayName),
            Description = description,
            Category = Guard.Against.NullOrWhiteSpace(category),
            Severity = severity,
            IsActive = true,
            EvaluationCriteria = evaluationCriteria,
            TenantId = tenantId,
            CreatedAt = createdAt
        };
    }

    /// <summary>Atualiza os dados da política.</summary>
    public void Update(
        string displayName,
        string? description,
        string category,
        ComplianceSeverity severity,
        string? evaluationCriteria,
        DateTimeOffset updatedAt)
    {
        DisplayName = Guard.Against.NullOrWhiteSpace(displayName);
        Description = description;
        Category = Guard.Against.NullOrWhiteSpace(category);
        Severity = severity;
        EvaluationCriteria = evaluationCriteria;
        UpdatedAt = updatedAt;
    }

    /// <summary>Ativa a política.</summary>
    public void Activate(DateTimeOffset updatedAt)
    {
        IsActive = true;
        UpdatedAt = updatedAt;
    }

    /// <summary>Desativa a política.</summary>
    public void Deactivate(DateTimeOffset updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }
}

/// <summary>Identificador fortemente tipado de CompliancePolicy.</summary>
public sealed record CompliancePolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CompliancePolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CompliancePolicyId From(Guid id) => new(id);
}
