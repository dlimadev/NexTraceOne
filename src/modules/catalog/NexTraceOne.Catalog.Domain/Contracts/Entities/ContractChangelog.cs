using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que regista entradas de changelog geradas a partir de verificações
/// ou criadas manualmente. Permite rastreabilidade completa da evolução contratual,
/// incluindo aprovação formal de alterações e correlação com verificações automáticas.
/// </summary>
public sealed class ContractChangelog : Entity<ContractChangelogId>
{
    private ContractChangelog() { }

    /// <summary>Identificador do tenant para isolamento multi-tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Identificador do ativo de API associado.</summary>
    public string ApiAssetId { get; private set; } = string.Empty;

    /// <summary>Nome do serviço associado ao changelog.</summary>
    public string ServiceName { get; private set; } = string.Empty;

    /// <summary>Versão de origem da alteração (nula se primeira versão).</summary>
    public string? FromVersion { get; private set; }

    /// <summary>Versão de destino da alteração.</summary>
    public string ToVersion { get; private set; } = string.Empty;

    /// <summary>Identificador da versão de contrato associada.</summary>
    public Guid ContractVersionId { get; private set; }

    /// <summary>Identificador da verificação que originou o changelog (nulo se manual).</summary>
    public Guid? VerificationId { get; private set; }

    /// <summary>Origem da entrada de changelog.</summary>
    public ChangelogSource Source { get; private set; }

    /// <summary>Entradas de changelog em formato JSON (JSONB de ChangelogEntry[]).</summary>
    public string Entries { get; private set; } = string.Empty;

    /// <summary>Sumário descritivo das alterações.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Conteúdo renderizado em Markdown (opcional).</summary>
    public string? MarkdownContent { get; private set; }

    /// <summary>Conteúdo renderizado em JSON (opcional).</summary>
    public string? JsonContent { get; private set; }

    /// <summary>Indica se o changelog foi aprovado formalmente.</summary>
    public bool IsApproved { get; private set; }

    /// <summary>Identificador do utilizador que aprovou o changelog (opcional).</summary>
    public string? ApprovedBy { get; private set; }

    /// <summary>Momento da aprovação formal (opcional).</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>SHA do commit associado ao changelog (opcional).</summary>
    public string? CommitSha { get; private set; }

    /// <summary>Momento de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Identificador do utilizador ou sistema que criou o registo.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Cria uma nova entrada de changelog de contrato.
    /// Valida os campos obrigatórios e inicializa o estado de aprovação como pendente.
    /// </summary>
    public static ContractChangelog Create(
        string tenantId,
        string apiAssetId,
        string serviceName,
        string? fromVersion,
        string toVersion,
        Guid contractVersionId,
        Guid? verificationId,
        ChangelogSource source,
        string entries,
        string summary,
        string? markdownContent,
        string? jsonContent,
        string? commitSha,
        DateTimeOffset createdAt,
        string createdBy)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(apiAssetId);
        Guard.Against.StringTooLong(apiAssetId, 200);
        Guard.Against.NullOrWhiteSpace(serviceName);
        Guard.Against.StringTooLong(serviceName, 300);
        Guard.Against.NullOrWhiteSpace(toVersion);
        Guard.Against.StringTooLong(toVersion, 50);
        Guard.Against.Default(contractVersionId);
        Guard.Against.EnumOutOfRange(source);
        Guard.Against.NullOrWhiteSpace(summary);
        Guard.Against.StringTooLong(summary, 2000);
        Guard.Against.NullOrWhiteSpace(createdBy);
        Guard.Against.StringTooLong(createdBy, 200);

        if (fromVersion is not null)
            Guard.Against.StringTooLong(fromVersion, 50);

        if (commitSha is not null)
            Guard.Against.StringTooLong(commitSha, 100);

        return new ContractChangelog
        {
            Id = ContractChangelogId.New(),
            TenantId = tenantId,
            ApiAssetId = apiAssetId,
            ServiceName = serviceName,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            ContractVersionId = contractVersionId,
            VerificationId = verificationId,
            Source = source,
            Entries = entries,
            Summary = summary,
            MarkdownContent = markdownContent,
            JsonContent = jsonContent,
            IsApproved = false,
            ApprovedBy = null,
            ApprovedAt = null,
            CommitSha = commitSha,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
        };
    }

    /// <summary>
    /// Aprova formalmente o changelog, registando quem aprovou e quando.
    /// </summary>
    public void Approve(string approvedBy, DateTimeOffset approvedAt)
    {
        Guard.Against.NullOrWhiteSpace(approvedBy);
        Guard.Against.StringTooLong(approvedBy, 200);

        IsApproved = true;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }
}
