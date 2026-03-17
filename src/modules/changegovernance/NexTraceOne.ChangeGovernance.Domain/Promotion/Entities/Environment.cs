using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

/// <summary>
/// Aggregate Root que representa um ambiente de deployment no pipeline de promoção.
/// Define a sequência de promoção e os requisitos de governança (aprovação, evidence pack).
/// Renomeado para DeploymentEnvironment para evitar colisão com System.Environment.
/// </summary>
public sealed class DeploymentEnvironment : AggregateRoot<DeploymentEnvironmentId>
{
    private DeploymentEnvironment() { }

    /// <summary>Nome do ambiente (ex: Development, Staging, Pre-Production, Production).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do propósito e características deste ambiente.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Ordem sequencial no caminho de promoção (menor = mais inicial).</summary>
    public int Order { get; private set; }

    /// <summary>Indica se a promoção para este ambiente requer aprovação explícita.</summary>
    public bool RequiresApproval { get; private set; }

    /// <summary>Indica se a promoção para este ambiente exige evidence pack completo.</summary>
    public bool RequiresEvidencePack { get; private set; }

    /// <summary>Indica se o ambiente está ativo e disponível para promoções.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Data/hora UTC de criação do ambiente.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria um novo ambiente de deployment com os parâmetros de governança informados.
    /// </summary>
    public static DeploymentEnvironment Create(
        string name,
        string description,
        int order,
        bool requiresApproval,
        bool requiresEvidencePack,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.StringTooLong(name, 200);
        Guard.Against.Null(description);
        Guard.Against.StringTooLong(description, 2000);
        Guard.Against.Negative(order);

        return new DeploymentEnvironment
        {
            Id = DeploymentEnvironmentId.New(),
            Name = name,
            Description = description,
            Order = order,
            RequiresApproval = requiresApproval,
            RequiresEvidencePack = requiresEvidencePack,
            IsActive = true,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Atualiza nome e descrição do ambiente.
    /// </summary>
    public void Update(string name, string description)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.StringTooLong(name, 200);
        Guard.Against.Null(description);
        Guard.Against.StringTooLong(description, 2000);

        Name = name;
        Description = description;
    }

    /// <summary>Ativa o ambiente, tornando-o disponível para promoções.</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Desativa o ambiente, impedindo novas promoções para ele.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Define a ordem sequencial deste ambiente no caminho de promoção.
    /// O valor não pode ser negativo.
    /// </summary>
    public void SetOrder(int order)
    {
        Guard.Against.Negative(order);
        Order = order;
    }
}

/// <summary>Identificador fortemente tipado de DeploymentEnvironment.</summary>
public sealed record DeploymentEnvironmentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DeploymentEnvironmentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DeploymentEnvironmentId From(Guid id) => new(id);
}
