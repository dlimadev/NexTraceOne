using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade PolicyDefinitionVersion.
/// </summary>
public sealed record PolicyDefinitionVersionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Snapshot imutável do conteúdo de uma PolicyAsCodeDefinition num determinado momento.
/// Criado sempre que o DefinitionContent ou Version é actualizado, preservando o histórico
/// completo de versões para rollback e auditoria.
/// Append-only — nunca é actualizado após inserção.
/// </summary>
public sealed class PolicyDefinitionVersion : Entity<PolicyDefinitionVersionId>
{
    /// <summary>Identificador da política à qual esta versão pertence.</summary>
    public PolicyAsCodeDefinitionId PolicyId { get; private init; } = null!;

    /// <summary>Identificador do tenant proprietário.</summary>
    public Guid TenantId { get; private init; }

    /// <summary>Label de versão semântica (ex.: "1.0.0", "2.3.1").</summary>
    public string Version { get; private init; } = string.Empty;

    /// <summary>Conteúdo completo da definição de política nesta versão.</summary>
    public string DefinitionContent { get; private init; } = string.Empty;

    /// <summary>Formato do conteúdo: YAML ou JSON.</summary>
    public PolicyDefinitionFormat Format { get; private init; }

    /// <summary>Utilizador que criou/guardou esta versão.</summary>
    public string CreatedBy { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC em que este snapshot foi capturado.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Nota opcional descrevendo as alterações desta versão.</summary>
    public string? ChangeNote { get; private init; }

    /// <summary>Construtor privado para EF Core.</summary>
    private PolicyDefinitionVersion() { }

    /// <summary>
    /// Cria um snapshot imutável a partir da definição de política actual.
    /// Tipicamente chamado antes de cada actualização ao DefinitionContent.
    /// </summary>
    public static PolicyDefinitionVersion Capture(
        PolicyAsCodeDefinition policy,
        string createdBy,
        DateTimeOffset now,
        string? changeNote = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        Guard.Against.NullOrWhiteSpace(createdBy, nameof(createdBy));

        return new PolicyDefinitionVersion
        {
            Id = new PolicyDefinitionVersionId(Guid.NewGuid()),
            PolicyId = policy.Id,
            TenantId = policy.TenantId,
            Version = policy.Version,
            DefinitionContent = policy.DefinitionContent,
            Format = policy.Format,
            CreatedBy = createdBy.Trim(),
            CreatedAt = now,
            ChangeNote = changeNote?.Trim()
        };
    }
}
