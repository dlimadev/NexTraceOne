using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Catalog.Domain.SourceOfTruth.Enums;

namespace NexTraceOne.Catalog.Domain.SourceOfTruth.Entities;

/// <summary>
/// Referência vinculada a um ativo no Source of Truth.
/// Permite associar documentação, runbooks, notas operacionais, links,
/// changelogs, tópicos de eventos e referências a incidentes a serviços e contratos.
/// Elemento fundamental para consolidar o NexTraceOne como fonte de verdade.
/// </summary>
public sealed class LinkedReference : AuditableEntity<LinkedReferenceId>
{
    private LinkedReference() { }

    /// <summary>Identificador do ativo ao qual esta referência está vinculada.</summary>
    public Guid AssetId { get; private set; }

    /// <summary>Tipo de ativo ao qual a referência está vinculada.</summary>
    public LinkedAssetType AssetType { get; private set; }

    /// <summary>Tipo da referência (documentação, runbook, nota, link, etc.).</summary>
    public LinkedReferenceType ReferenceType { get; private set; }

    /// <summary>Título da referência.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descrição ou conteúdo resumido da referência.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>URL ou URI da referência, quando aplicável.</summary>
    public string? Url { get; private set; }

    /// <summary>Conteúdo textual da referência, para notas e changelogs inline.</summary>
    public string? Content { get; private set; }

    /// <summary>Metadados adicionais em formato JSON, para extensibilidade.</summary>
    public string? Metadata { get; private set; }

    /// <summary>Indica se a referência está ativa e visível.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Cria uma nova referência vinculada a um ativo.
    /// </summary>
    public static LinkedReference Create(
        Guid assetId,
        LinkedAssetType assetType,
        LinkedReferenceType referenceType,
        string title,
        string? description = null,
        string? url = null,
        string? content = null,
        string? metadata = null)
    {
        Guard.Against.Default(assetId);
        Guard.Against.NullOrWhiteSpace(title);

        return new LinkedReference
        {
            Id = LinkedReferenceId.New(),
            AssetId = assetId,
            AssetType = assetType,
            ReferenceType = referenceType,
            Title = title,
            Description = description ?? string.Empty,
            Url = url,
            Content = content,
            Metadata = metadata,
            IsActive = true
        };
    }

    /// <summary>Atualiza os dados da referência.</summary>
    public void Update(string title, string? description, string? url, string? content, string? metadata)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Title = title;
        Description = description ?? string.Empty;
        Url = url;
        Content = content;
        Metadata = metadata;
    }

    /// <summary>Desativa a referência sem removê-la.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa a referência.</summary>
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de LinkedReference.</summary>
public sealed record LinkedReferenceId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LinkedReferenceId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LinkedReferenceId From(Guid id) => new(id);
}
