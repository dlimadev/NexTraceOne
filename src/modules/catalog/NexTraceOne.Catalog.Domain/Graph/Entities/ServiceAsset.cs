using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Serviço proprietário de um ativo de API no grafo de engenharia.
/// </summary>
public sealed class ServiceAsset : Entity<ServiceAssetId>
{
    private ServiceAsset() { }

    /// <summary>Nome único do serviço.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Domínio de negócio ao qual o serviço pertence.</summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>Equipe responsável pelo serviço.</summary>
    public string TeamName { get; private set; } = string.Empty;

    /// <summary>Cria um novo serviço proprietário.</summary>
    public static ServiceAsset Create(string name, string domain, string teamName)
        => new()
        {
            Id = ServiceAssetId.New(),
            Name = Guard.Against.NullOrWhiteSpace(name),
            Domain = Guard.Against.NullOrWhiteSpace(domain),
            TeamName = Guard.Against.NullOrWhiteSpace(teamName)
        };
}

/// <summary>Identificador fortemente tipado de ServiceAsset.</summary>
public sealed record ServiceAssetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceAssetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceAssetId From(Guid id) => new(id);
}
