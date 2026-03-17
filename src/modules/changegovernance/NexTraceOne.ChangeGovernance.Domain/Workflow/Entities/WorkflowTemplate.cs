using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

/// <summary>
/// Aggregate Root que representa um template reutilizável de workflow de aprovação.
/// Define as regras de governança (tipo de mudança, criticidade, ambiente, número mínimo de aprovadores).
/// </summary>
public sealed class WorkflowTemplate : AggregateRoot<WorkflowTemplateId>
{
    private WorkflowTemplate() { }

    /// <summary>Nome do template de workflow.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e escopo deste template.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Tipo de mudança ao qual este template se aplica (Breaking, NonBreaking, BugFix, Operational).</summary>
    public string ChangeType { get; private set; } = string.Empty;

    /// <summary>Criticidade da API a que se destina (Low, Medium, High, Critical).</summary>
    public string ApiCriticality { get; private set; } = string.Empty;

    /// <summary>Ambiente alvo do deployment (Development, PreProduction, Production).</summary>
    public string TargetEnvironment { get; private set; } = string.Empty;

    /// <summary>Número mínimo de aprovadores necessários para cada estágio.</summary>
    public int MinimumApprovers { get; private set; }

    /// <summary>Indica se o template está ativo e pode ser utilizado em novas instâncias.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação do template.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria um novo template de workflow com as regras de governança informadas.
    /// </summary>
    public static WorkflowTemplate Create(
        string name,
        string description,
        string changeType,
        string apiCriticality,
        string targetEnvironment,
        int minimumApprovers,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(description);
        Guard.Against.NullOrWhiteSpace(changeType);
        Guard.Against.NullOrWhiteSpace(apiCriticality);
        Guard.Against.NullOrWhiteSpace(targetEnvironment);
        Guard.Against.NegativeOrZero(minimumApprovers);

        return new WorkflowTemplate
        {
            Id = WorkflowTemplateId.New(),
            Name = name,
            Description = description,
            ChangeType = changeType,
            ApiCriticality = apiCriticality,
            TargetEnvironment = targetEnvironment,
            MinimumApprovers = minimumApprovers,
            IsActive = true,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Atualiza nome e descrição do template.
    /// Se ambos os valores forem idênticos aos atuais, a operação é um no-op silencioso.
    /// </summary>
    public void Update(string name, string description)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(description);

        Name = name;
        Description = description;
    }

    /// <summary>Ativa o template para uso em novas instâncias de workflow.</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Desativa o template, impedindo a criação de novas instâncias.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Define o número mínimo de aprovadores para os estágios deste template.
    /// O valor deve ser maior ou igual a 1.
    /// </summary>
    public void SetMinimumApprovers(int count)
    {
        Guard.Against.NegativeOrZero(count);
        MinimumApprovers = count;
    }
}

/// <summary>Identificador fortemente tipado de WorkflowTemplate.</summary>
public sealed record WorkflowTemplateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static WorkflowTemplateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static WorkflowTemplateId From(Guid id) => new(id);
}
