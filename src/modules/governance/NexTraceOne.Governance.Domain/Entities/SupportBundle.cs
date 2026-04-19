using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para SupportBundle.</summary>
public sealed record SupportBundleId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que representa um bundle de suporte gerado pela plataforma.
/// Contém informação de sistema, dados de governança e configuração (sem segredos)
/// exportados para diagnóstico e suporte.
/// </summary>
public sealed class SupportBundle : Entity<SupportBundleId>
{
    private SupportBundle() { }

    /// <summary>Estado do bundle: Pending, Generating, Ready, Failed.</summary>
    public string Status { get; private set; } = "Pending";

    /// <summary>Data/hora em que o bundle foi solicitado.</summary>
    public DateTimeOffset RequestedAt { get; private init; }

    /// <summary>Data/hora em que o bundle ficou pronto ou falhou.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Tamanho do bundle em MB (após geração).</summary>
    public double? SizeMb { get; private set; }

    /// <summary>Conteúdo binário do ZIP gerado (armazenado inline para bundles ≤ 2MB).</summary>
    public byte[]? ZipContent { get; private set; }

    /// <summary>Indica se deve incluir logs de sistema.</summary>
    public bool IncludesLogs { get; private init; }

    /// <summary>Indica se deve incluir resumo de configuração.</summary>
    public bool IncludesConfig { get; private init; }

    /// <summary>Indica se deve incluir dump de esquema de base de dados.</summary>
    public bool IncludesDb { get; private init; }

    /// <summary>Identificador do tenant.</summary>
    public Guid? TenantId { get; private init; }

    /// <summary>Data de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Cria um novo SupportBundle com estado Pending.</summary>
    public static SupportBundle Create(
        bool includesLogs,
        bool includesConfig,
        bool includesDb,
        Guid? tenantId,
        DateTimeOffset now) =>
        new()
        {
            Id = new SupportBundleId(Guid.NewGuid()),
            Status = "Pending",
            RequestedAt = now,
            IncludesLogs = includesLogs,
            IncludesConfig = includesConfig,
            IncludesDb = includesDb,
            TenantId = tenantId,
            CreatedAt = now
        };

    /// <summary>Marca o bundle como em geração.</summary>
    public void MarkGenerating(DateTimeOffset now)
    {
        Status = "Generating";
    }

    /// <summary>Marca o bundle como pronto com o conteúdo ZIP gerado.</summary>
    public void MarkReady(byte[] zipContent, DateTimeOffset now)
    {
        Guard.Against.Null(zipContent);
        Guard.Against.NegativeOrZero(zipContent.Length);

        Status = "Ready";
        ZipContent = zipContent;
        SizeMb = Math.Round(zipContent.Length / 1_048_576.0, 3);
        CompletedAt = now;
    }

    /// <summary>Marca o bundle como falhado.</summary>
    public void MarkFailed(DateTimeOffset now)
    {
        Status = "Failed";
        CompletedAt = now;
    }
}
