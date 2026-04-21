using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>
/// Entidade que representa um token de acesso de plataforma para agentes autónomos.
/// Usado pelo protocolo Agent-to-Agent (Wave D.4) para autenticação e auditoria de agentes.
/// Cada token tem um escopo (Read/ReadWrite/Admin) e pode ser revogado.
/// Prefixo de tabela: iam_
/// </summary>
public sealed class PlatformApiToken : AggregateRoot<PlatformApiTokenId>
{
    private PlatformApiToken() { }

    /// <summary>Tenant ao qual o token pertence.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nome descritivo do token (ex: "CI-pipeline-prod", "monitoring-agent-v2").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Hash SHA-256 do token (o valor real é apresentado apenas uma vez na criação).</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Prefixo do token visível para identificação (primeiros 8 chars).</summary>
    public string TokenPrefix { get; private set; } = string.Empty;

    /// <summary>Escopo de acesso do token.</summary>
    public PlatformApiTokenScope Scope { get; private set; }

    /// <summary>Data de expiração opcional. Null = não expira.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>Data de criação do token.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Utilizador que criou o token.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Data de revogação, se revogado.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Motivo de revogação.</summary>
    public string? RevokedReason { get; private set; }

    /// <summary>Data do último uso do token.</summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>Indica se o token está activo (não revogado e não expirado).</summary>
    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);

    public static PlatformApiToken Create(
        Guid tenantId,
        string name,
        string tokenHash,
        string tokenPrefix,
        PlatformApiTokenScope scope,
        string createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset? expiresAt = null)
    {
        Guard.Against.Default(tenantId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(tokenHash);
        Guard.Against.NullOrWhiteSpace(tokenPrefix);
        Guard.Against.NullOrWhiteSpace(createdBy);

        return new PlatformApiToken
        {
            Id = PlatformApiTokenId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            Scope = scope,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            ExpiresAt = expiresAt,
        };
    }

    public void Revoke(string reason, DateTimeOffset revokedAt)
    {
        Guard.Against.NullOrWhiteSpace(reason);
        RevokedAt = revokedAt;
        RevokedReason = reason;
    }

    public void RecordUsage(DateTimeOffset usedAt) => LastUsedAt = usedAt;
}

/// <summary>Escopo de acesso do token de plataforma.</summary>
public enum PlatformApiTokenScope
{
    /// <summary>Acesso de leitura — consulta catálogo, mudanças, contratos.</summary>
    Read = 0,
    /// <summary>Acesso de leitura e escrita — ingestão de dados + leitura.</summary>
    ReadWrite = 1,
    /// <summary>Acesso administrativo — configuração do produto.</summary>
    Admin = 2,
}
