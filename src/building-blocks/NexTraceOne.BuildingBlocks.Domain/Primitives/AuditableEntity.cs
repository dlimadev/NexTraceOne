namespace NexTraceOne.BuildingBlocks.Domain.Primitives;

/// <summary>
/// Extensão de Entity com campos de auditoria automáticos.
/// Toda entidade que herdar desta classe terá CreatedAt, CreatedBy,
/// UpdatedAt e UpdatedBy preenchidos automaticamente pelo AuditInterceptor
/// do DbContext, antes de cada SaveChanges.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId> where TId : ITypedId
{
    /// <summary>Data/hora UTC de criação do registro.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Id do usuário que criou o registro.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Id do usuário que realizou a última atualização.</summary>
    public string UpdatedBy { get; private set; } = string.Empty;

    /// <summary>Indica se o registro foi removido logicamente (soft-delete).</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Chamado pelo AuditInterceptor ao criar. Não chamar diretamente.</summary>
    public void SetCreated(DateTimeOffset at, string by) { CreatedAt = at; CreatedBy = by; }

    /// <summary>Chamado pelo AuditInterceptor ao atualizar. Não chamar diretamente.</summary>
    public void SetUpdated(DateTimeOffset at, string by) { UpdatedAt = at; UpdatedBy = by; }

    /// <summary>Marca o registro como removido logicamente. Não deleta do banco.</summary>
    public void SoftDelete() => IsDeleted = true;
}
