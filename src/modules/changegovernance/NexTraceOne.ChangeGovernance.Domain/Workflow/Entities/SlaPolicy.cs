using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Entidade que representa a política de SLA de um estágio de workflow.
/// Define o tempo máximo permitido e as regras de escalação em caso de violação.
/// </summary>
public sealed class SlaPolicy : AuditableEntity<SlaPolicyId>
{
    private SlaPolicy() { }

    /// <summary>Identificador do template de workflow ao qual esta política pertence.</summary>
    public WorkflowTemplateId WorkflowTemplateId { get; private set; } = null!;

    /// <summary>Nome do estágio ao qual esta política de SLA se aplica.</summary>
    public string StageName { get; private set; } = string.Empty;

    /// <summary>Duração máxima do SLA em horas.</summary>
    public int MaxDurationHours { get; private set; }

    /// <summary>Indica se a escalação automática está habilitada ao violar o SLA.</summary>
    public bool EscalationEnabled { get; private set; }

    /// <summary>Role-alvo para escalação em caso de violação (nulo se escalação desabilitada).</summary>
    public string? EscalationTargetRole { get; private set; }

    /// <summary>
    /// Cria uma nova política de SLA para um estágio de workflow.
    /// </summary>
    public static SlaPolicy Create(
        WorkflowTemplateId workflowTemplateId,
        string stageName,
        int maxDurationHours,
        bool escalationEnabled,
        string? escalationTargetRole)
    {
        Guard.Against.Null(workflowTemplateId);
        Guard.Against.NullOrWhiteSpace(stageName);
        Guard.Against.NegativeOrZero(maxDurationHours);

        return new SlaPolicy
        {
            Id = SlaPolicyId.New(),
            WorkflowTemplateId = workflowTemplateId,
            StageName = stageName,
            MaxDurationHours = maxDurationHours,
            EscalationEnabled = escalationEnabled,
            EscalationTargetRole = escalationTargetRole
        };
    }

    /// <summary>
    /// Atualiza as configurações de SLA e escalação desta política.
    /// </summary>
    public void Update(int maxDurationHours, bool escalationEnabled, string? escalationTargetRole)
    {
        Guard.Against.NegativeOrZero(maxDurationHours);

        MaxDurationHours = maxDurationHours;
        EscalationEnabled = escalationEnabled;
        EscalationTargetRole = escalationTargetRole;
    }
}

/// <summary>Identificador fortemente tipado de SlaPolicy.</summary>
public sealed record SlaPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SlaPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SlaPolicyId From(Guid id) => new(id);
}
