using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para BreakGlassRequest.
/// Persiste solicitações de acesso emergencial com trilha imutável de auditoria.
/// Índice por (RequestedBy, Status) permite consulta eficiente de contagem trimestral.
/// </summary>
internal sealed class BreakGlassRequestConfiguration : IEntityTypeConfiguration<BreakGlassRequest>
{
    public void Configure(EntityTypeBuilder<BreakGlassRequest> builder)
    {
        builder.ToTable("iam_break_glass_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => BreakGlassRequestId.From(value));

        builder.Property(x => x.RequestedBy)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.Justification)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.RevokedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(x => x.PostMortemNotes).HasMaxLength(4000);
        builder.Property(x => x.PostMortemAt);

        builder.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(512).IsRequired();

        builder.HasIndex(x => new { x.RequestedBy, x.Status });
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
