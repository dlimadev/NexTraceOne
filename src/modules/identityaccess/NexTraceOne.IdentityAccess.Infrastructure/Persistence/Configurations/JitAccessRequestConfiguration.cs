using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para JitAccessRequest.
/// Persiste solicitações de acesso privilegiado temporário com ciclo de vida completo.
/// Índices otimizados para consulta de pendentes (aprovação) e ativos (grant vigente).
/// </summary>
internal sealed class JitAccessRequestConfiguration : IEntityTypeConfiguration<JitAccessRequest>
{
    public void Configure(EntityTypeBuilder<JitAccessRequest> builder)
    {
        builder.ToTable("iam_jit_access_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => JitAccessRequestId.From(value));

        builder.Property(x => x.RequestedBy)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.PermissionCode)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Justification)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.ApprovalDeadline).IsRequired();

        builder.Property(x => x.DecidedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(x => x.DecidedAt);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.GrantedFrom);
        builder.Property(x => x.GrantedUntil);
        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.RevokedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.RequestedBy);
        builder.HasIndex(x => x.Status);
    }
}
