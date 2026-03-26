using Ardalis.GuardClauses;

using MediatR;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Portal.Enums;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Domain.Portal.Entities;

/// <summary>
/// Entidade que representa a entrada de publicação de um contrato no Developer Portal / Publication Center.
/// Separa explicitamente o conceito de "versão de contrato aprovada internamente" (ContractVersion)
/// de "contrato publicado e visível no portal para consumidores" (ContractPublicationEntry).
/// Permite governança granular: uma versão aprovada pode existir sem estar publicada no portal,
/// e a publicação pode ser retirada sem afetar o lifecycle interno do contrato.
/// Reside no DeveloperPortalDbContext — cross-module por valor (ContractVersionId como Guid).
/// </summary>
public sealed class ContractPublicationEntry : AuditableEntity<ContractPublicationEntryId>
{
    private ContractPublicationEntry() { }

    /// <summary>Identificador da versão de contrato publicada. Referência por valor (cross-module).</summary>
    public Guid ContractVersionId { get; private set; }

    /// <summary>Identificador do ativo de API ao qual o contrato pertence. Referência por valor (cross-module).</summary>
    public Guid ApiAssetId { get; private set; }

    /// <summary>Título do contrato publicado (desnormalizado para consulta eficiente no portal).</summary>
    public string ContractTitle { get; private set; } = string.Empty;

    /// <summary>Versão semântica do contrato publicado (ex: "2.1.0").</summary>
    public string SemVer { get; private set; } = string.Empty;

    /// <summary>Estado atual da publicação no portal.</summary>
    public ContractPublicationStatus Status { get; private set; }

    /// <summary>Escopo de visibilidade — quem pode ver o contrato no catálogo.</summary>
    public PublicationVisibility Visibility { get; private set; }

    /// <summary>Identificador de quem publicou o contrato no portal.</summary>
    public string PublishedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC em que o contrato foi publicado no portal.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Notas de release opcionais visíveis no portal (changelog público).</summary>
    public string? ReleaseNotes { get; private set; }

    /// <summary>Identificador de quem retirou a publicação, quando aplicável.</summary>
    public string? WithdrawnBy { get; private set; }

    /// <summary>Data/hora UTC da retirada de publicação, quando aplicável.</summary>
    public DateTimeOffset? WithdrawnAt { get; private set; }

    /// <summary>Motivo da retirada de publicação, quando aplicável.</summary>
    public string? WithdrawalReason { get; private set; }

    /// <summary>
    /// Cria uma entrada de publicação no estado PendingPublication.
    /// A publicação efetiva acontece ao chamar Publish().
    /// </summary>
    public static Result<ContractPublicationEntry> Create(
        Guid contractVersionId,
        Guid apiAssetId,
        string contractTitle,
        string semVer,
        string publishedBy,
        PublicationVisibility visibility = PublicationVisibility.Internal,
        string? releaseNotes = null)
    {
        Guard.Against.Default(contractVersionId);
        Guard.Against.Default(apiAssetId);
        Guard.Against.NullOrWhiteSpace(contractTitle);
        Guard.Against.NullOrWhiteSpace(semVer);
        Guard.Against.NullOrWhiteSpace(publishedBy);

        return new ContractPublicationEntry
        {
            Id = ContractPublicationEntryId.New(),
            ContractVersionId = contractVersionId,
            ApiAssetId = apiAssetId,
            ContractTitle = contractTitle,
            SemVer = semVer,
            Status = ContractPublicationStatus.PendingPublication,
            Visibility = visibility,
            PublishedBy = publishedBy,
            ReleaseNotes = releaseNotes
        };
    }

    /// <summary>
    /// Publica o contrato no portal — transição PendingPublication → Published.
    /// </summary>
    public Result<Unit> Publish(DateTimeOffset publishedAt)
    {
        if (Status != ContractPublicationStatus.PendingPublication)
            return DeveloperPortalErrors.PublicationInvalidTransition(Status.ToString(), ContractPublicationStatus.Published.ToString());

        Status = ContractPublicationStatus.Published;
        PublishedAt = publishedAt;
        return Unit.Value;
    }

    /// <summary>
    /// Retira a publicação do contrato do portal — transição Published → Withdrawn.
    /// </summary>
    public Result<Unit> Withdraw(string withdrawnBy, string? reason, DateTimeOffset withdrawnAt)
    {
        Guard.Against.NullOrWhiteSpace(withdrawnBy);

        if (Status != ContractPublicationStatus.Published)
            return DeveloperPortalErrors.PublicationInvalidTransition(Status.ToString(), ContractPublicationStatus.Withdrawn.ToString());

        Status = ContractPublicationStatus.Withdrawn;
        WithdrawnBy = withdrawnBy;
        WithdrawnAt = withdrawnAt;
        WithdrawalReason = reason;
        return Unit.Value;
    }

    /// <summary>
    /// Marca a publicação como Deprecated — válido de Published ou Withdrawn.
    /// </summary>
    public Result<Unit> MarkAsDeprecated()
    {
        if (Status is ContractPublicationStatus.PendingPublication or ContractPublicationStatus.Deprecated)
            return DeveloperPortalErrors.PublicationInvalidTransition(Status.ToString(), ContractPublicationStatus.Deprecated.ToString());

        Status = ContractPublicationStatus.Deprecated;
        return Unit.Value;
    }
}

/// <summary>Identificador fortemente tipado de ContractPublicationEntry.</summary>
public sealed record ContractPublicationEntryId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractPublicationEntryId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractPublicationEntryId From(Guid id) => new(id);
}
