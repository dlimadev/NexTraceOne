using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Visão salva do grafo de engenharia com filtros, overlay e foco persistidos.
/// Permite que cada usuário salve configurações de visualização reutilizáveis,
/// compartilhe contextos via deep link e defina presets por persona
/// (exec, tech lead, SRE, security reviewer).
/// </summary>
public sealed class SavedGraphView : Entity<SavedGraphViewId>
{
    private SavedGraphView() { }

    /// <summary>Nome amigável da visão salva (ex: "My API Blast Radius", "Team Alpha Overview").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional explicando o contexto ou propósito da visão.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Identificador do usuário que criou a visão.</summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>Indica se a visão é compartilhável publicamente dentro do tenant.</summary>
    public bool IsShared { get; private set; }

    /// <summary>Filtros, overlay, foco e layout serializados como JSON para reprodução exata.</summary>
    public string FiltersJson { get; private set; } = string.Empty;

    /// <summary>Instante UTC em que a visão foi criada.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria uma visão salva do grafo com configuração de filtros e overlay.
    /// O FiltersJson deve conter todos os parâmetros necessários para
    /// reproduzir exatamente a mesma visualização quando carregada.
    /// </summary>
    public static SavedGraphView Create(
        string name,
        string description,
        string ownerId,
        bool isShared,
        string filtersJson,
        DateTimeOffset createdAt)
    {
        return new SavedGraphView
        {
            Id = SavedGraphViewId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Description = description ?? string.Empty,
            OwnerId = Guard.Against.NullOrWhiteSpace(ownerId),
            IsShared = isShared,
            FiltersJson = Guard.Against.NullOrWhiteSpace(filtersJson),
            CreatedAt = createdAt
        };
    }

    /// <summary>Atualiza nome, descrição e filtros da visão salva.</summary>
    public void Update(string name, string description, string filtersJson, bool isShared)
    {
        Name = Guard.Against.NullOrWhiteSpace(name);
        Description = description ?? string.Empty;
        FiltersJson = Guard.Against.NullOrWhiteSpace(filtersJson);
        IsShared = isShared;
    }
}

/// <summary>Identificador fortemente tipado de SavedGraphView.</summary>
public sealed record SavedGraphViewId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SavedGraphViewId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SavedGraphViewId From(Guid id) => new(id);
}
