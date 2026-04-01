using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Rascunho de contrato em edição no Contract Studio.
/// Representa um contrato em construção ou modificação antes de ser promovido a versão oficial.
/// Suporta edição manual, geração assistida por IA, e vinculação a serviço e equipa.
/// </summary>
public sealed class ContractDraft : AuditableEntity<ContractDraftId>
{
    private readonly List<ContractExample> _examples = [];

    private ContractDraft() { }

    /// <summary>Título descritivo do draft.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Descrição do objetivo ou contexto do draft.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Identificador do serviço vinculado ao contrato.</summary>
    public Guid? ServiceId { get; private set; }

    /// <summary>Tipo de contrato: REST, SOAP, Event, Background, Schema.</summary>
    public ContractType ContractType { get; private set; }

    /// <summary>Protocolo do contrato (OpenAPI, WSDL, AsyncAPI, etc.).</summary>
    public ContractProtocol Protocol { get; private set; }

    /// <summary>Conteúdo bruto do artefato (JSON, YAML, XML).</summary>
    public string SpecContent { get; private set; } = string.Empty;

    /// <summary>Formato do conteúdo: "json", "yaml" ou "xml".</summary>
    public string Format { get; private set; } = string.Empty;

    /// <summary>Versão semântica proposta para este draft.</summary>
    public string ProposedVersion { get; private set; } = string.Empty;

    /// <summary>Estado atual do draft no fluxo de edição.</summary>
    public DraftStatus Status { get; private set; }

    /// <summary>Autor do draft.</summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>Identificador do contrato base quando o draft é uma evolução de versão existente.</summary>
    public Guid? BaseContractVersionId { get; private set; }

    /// <summary>Indica se o draft foi gerado por IA.</summary>
    public bool IsAiGenerated { get; private set; }

    /// <summary>Prompt ou descrição usada para geração por IA, quando aplicável.</summary>
    public string? AiGenerationPrompt { get; private set; }

    /// <summary>Timestamp da última edição do conteúdo.</summary>
    public DateTimeOffset? LastEditedAt { get; private set; }

    /// <summary>Usuário que realizou a última edição.</summary>
    public string? LastEditedBy { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// Utilizado pelo EF Core para detetar conflitos de escrita concorrente.
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>Exemplos associados ao draft.</summary>
    public IReadOnlyList<ContractExample> Examples => _examples.AsReadOnly();

    /// <summary>
    /// Cria um novo draft de contrato.
    /// </summary>
    public static Result<ContractDraft> Create(
        string title,
        string author,
        ContractType contractType,
        ContractProtocol protocol,
        Guid? serviceId = null,
        string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(author);

        return new ContractDraft
        {
            Id = ContractDraftId.New(),
            Title = title,
            Author = author,
            ContractType = contractType,
            Protocol = protocol,
            ServiceId = serviceId,
            Description = description ?? string.Empty,
            Status = DraftStatus.Editing,
            Format = "yaml",
            SpecContent = string.Empty,
            ProposedVersion = "1.0.0"
        };
    }

    /// <summary>
    /// Cria um draft gerado por IA a partir de um prompt.
    /// </summary>
    public static Result<ContractDraft> CreateFromAi(
        string title,
        string author,
        ContractType contractType,
        ContractProtocol protocol,
        string aiPrompt,
        string generatedContent,
        string format,
        Guid? serviceId = null)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.NullOrWhiteSpace(author);
        Guard.Against.NullOrWhiteSpace(aiPrompt);
        Guard.Against.NullOrWhiteSpace(generatedContent);
        Guard.Against.NullOrWhiteSpace(format);

        return new ContractDraft
        {
            Id = ContractDraftId.New(),
            Title = title,
            Author = author,
            ContractType = contractType,
            Protocol = protocol,
            ServiceId = serviceId,
            Description = string.Empty,
            Status = DraftStatus.Editing,
            Format = format.ToLowerInvariant(),
            SpecContent = generatedContent,
            ProposedVersion = "1.0.0",
            IsAiGenerated = true,
            AiGenerationPrompt = aiPrompt
        };
    }

    /// <summary>
    /// Atualiza o conteúdo do artefato do draft.
    /// Apenas drafts no estado Editing podem ser editados.
    /// </summary>
    public Result<MediatR.Unit> UpdateContent(string specContent, string format, string editedBy, DateTimeOffset editedAt)
    {
        Guard.Against.NullOrWhiteSpace(editedBy);

        if (Status != DraftStatus.Editing)
            return ContractsErrors.DraftNotEditable(Id.Value.ToString());

        SpecContent = specContent ?? string.Empty;
        Format = format?.ToLowerInvariant() ?? Format;
        LastEditedAt = editedAt;
        LastEditedBy = editedBy;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Atualiza os metadados do draft.
    /// </summary>
    public Result<MediatR.Unit> UpdateMetadata(
        string? title,
        string? description,
        string? proposedVersion,
        Guid? serviceId,
        string editedBy,
        DateTimeOffset editedAt)
    {
        Guard.Against.NullOrWhiteSpace(editedBy);

        if (Status != DraftStatus.Editing)
            return ContractsErrors.DraftNotEditable(Id.Value.ToString());

        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (proposedVersion is not null) ProposedVersion = proposedVersion;
        if (serviceId.HasValue) ServiceId = serviceId;
        LastEditedAt = editedAt;
        LastEditedBy = editedBy;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Submete o draft para revisão.
    /// </summary>
    public Result<MediatR.Unit> SubmitForReview(DateTimeOffset submittedAt)
    {
        if (Status != DraftStatus.Editing)
            return ContractsErrors.DraftNotEditable(Id.Value.ToString());

        if (string.IsNullOrWhiteSpace(SpecContent))
            return ContractsErrors.EmptySpecContent();

        Status = DraftStatus.InReview;
        LastEditedAt = submittedAt;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Aprova o draft para publicação.
    /// </summary>
    public Result<MediatR.Unit> Approve(string approvedBy, DateTimeOffset approvedAt)
    {
        Guard.Against.NullOrWhiteSpace(approvedBy);

        if (Status != DraftStatus.InReview)
            return ContractsErrors.InvalidDraftTransition(Status.ToString(), DraftStatus.Approved.ToString());

        Status = DraftStatus.Approved;
        LastEditedAt = approvedAt;
        LastEditedBy = approvedBy;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Rejeita o draft, retornando para edição.
    /// </summary>
    public Result<MediatR.Unit> Reject(string rejectedBy, DateTimeOffset rejectedAt)
    {
        Guard.Against.NullOrWhiteSpace(rejectedBy);

        if (Status != DraftStatus.InReview)
            return ContractsErrors.InvalidDraftTransition(Status.ToString(), DraftStatus.Editing.ToString());

        Status = DraftStatus.Editing;
        LastEditedAt = rejectedAt;
        LastEditedBy = rejectedBy;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Marca o draft como publicado após criar a versão oficial.
    /// </summary>
    public Result<MediatR.Unit> MarkAsPublished(DateTimeOffset publishedAt)
    {
        if (Status != DraftStatus.Approved)
            return ContractsErrors.InvalidDraftTransition(Status.ToString(), DraftStatus.Published.ToString());

        Status = DraftStatus.Published;
        LastEditedAt = publishedAt;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Descarta o draft.
    /// </summary>
    public Result<MediatR.Unit> Discard(DateTimeOffset discardedAt)
    {
        if (Status == DraftStatus.Published)
            return ContractsErrors.InvalidDraftTransition(Status.ToString(), DraftStatus.Discarded.ToString());

        Status = DraftStatus.Discarded;
        LastEditedAt = discardedAt;
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Adiciona um exemplo ao draft.
    /// Apenas drafts nos estados Editing ou InReview permitem adição de exemplos.
    /// </summary>
    public Result<MediatR.Unit> AddExample(ContractExample example)
    {
        Guard.Against.Null(example);

        if (Status is DraftStatus.Published or DraftStatus.Discarded)
            return ContractsErrors.DraftNotEditable(Id.Value.ToString());

        _examples.Add(example);
        return MediatR.Unit.Value;
    }

    /// <summary>
    /// Remove um exemplo do draft.
    /// Apenas drafts nos estados Editing ou InReview permitem remoção de exemplos.
    /// Retorna erro se o exemplo não for encontrado.
    /// </summary>
    public Result<MediatR.Unit> RemoveExample(ContractExampleId exampleId)
    {
        Guard.Against.Null(exampleId);

        if (Status is DraftStatus.Published or DraftStatus.Discarded)
            return ContractsErrors.DraftNotEditable(Id.Value.ToString());

        var example = _examples.Find(e => e.Id == exampleId);
        if (example is null)
            return ContractsErrors.ExampleNotFound(exampleId.Value.ToString());

        _examples.Remove(example);
        return MediatR.Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de ContractDraft.</summary>
public sealed record ContractDraftId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractDraftId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractDraftId From(Guid id) => new(id);
}
