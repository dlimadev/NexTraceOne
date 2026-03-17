using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Exemplo associado a um contrato ou draft.
/// Permite documentar cenários de uso com request/response ou payloads de exemplo.
/// </summary>
public sealed class ContractExample : Entity<ContractExampleId>
{
    private ContractExample() { }

    /// <summary>Identificador do draft ao qual o exemplo pertence (quando vinculado a draft).</summary>
    public ContractDraftId? DraftId { get; private set; }

    /// <summary>Identificador da versão de contrato ao qual o exemplo pertence (quando vinculado a versão publicada).</summary>
    public ContractVersionId? ContractVersionId { get; private set; }

    /// <summary>Nome descritivo do exemplo.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição do cenário que o exemplo representa.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Conteúdo do exemplo (JSON, YAML, XML).</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Formato do conteúdo: "json", "yaml" ou "xml".</summary>
    public string ContentFormat { get; private set; } = string.Empty;

    /// <summary>Tipo do exemplo (request, response, event, payload).</summary>
    public string ExampleType { get; private set; } = string.Empty;

    /// <summary>Timestamp de criação do exemplo.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Autor do exemplo.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// Cria um novo exemplo de contrato vinculado a um draft.
    /// </summary>
    public static ContractExample CreateForDraft(
        ContractDraftId draftId,
        string name,
        string content,
        string contentFormat,
        string exampleType,
        string createdBy,
        DateTimeOffset createdAt,
        string? description = null)
    {
        Guard.Against.Null(draftId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(contentFormat);
        Guard.Against.NullOrWhiteSpace(exampleType);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new ContractExample
        {
            Id = ContractExampleId.New(),
            DraftId = draftId,
            Name = name,
            Description = description ?? string.Empty,
            Content = content,
            ContentFormat = contentFormat.ToLowerInvariant(),
            ExampleType = exampleType,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Cria um novo exemplo de contrato vinculado a uma versão publicada.
    /// </summary>
    public static ContractExample CreateForVersion(
        ContractVersionId contractVersionId,
        string name,
        string content,
        string contentFormat,
        string exampleType,
        string createdBy,
        DateTimeOffset createdAt,
        string? description = null)
    {
        Guard.Against.Null(contractVersionId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(contentFormat);
        Guard.Against.NullOrWhiteSpace(exampleType);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new ContractExample
        {
            Id = ContractExampleId.New(),
            ContractVersionId = contractVersionId,
            Name = name,
            Description = description ?? string.Empty,
            Content = content,
            ContentFormat = contentFormat.ToLowerInvariant(),
            ExampleType = exampleType,
            CreatedAt = createdAt,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Atualiza o conteúdo do exemplo.
    /// </summary>
    public void Update(string? name, string? content, string? contentFormat, string? description)
    {
        if (name is not null) Name = name;
        if (content is not null) Content = content;
        if (contentFormat is not null) ContentFormat = contentFormat.ToLowerInvariant();
        if (description is not null) Description = description;
    }
}

/// <summary>Identificador fortemente tipado de ContractExample.</summary>
public sealed record ContractExampleId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractExampleId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractExampleId From(Guid id) => new(id);
}
