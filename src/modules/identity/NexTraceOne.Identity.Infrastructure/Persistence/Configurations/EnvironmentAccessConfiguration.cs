using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade EnvironmentAccess.
/// Define tabela, conversões de IDs fortemente tipados, constraints e índices.
/// Índice composto em (UserId, TenantId, EnvironmentId) otimiza a consulta principal
/// de verificação de acesso a um ambiente específico.
/// </summary>
internal sealed class EnvironmentAccessConfiguration : IEntityTypeConfiguration<EnvironmentAccess>
{
    public void Configure(EntityTypeBuilder<EnvironmentAccess> builder)
    {
        builder.ToTable("identity_environment_accesses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(e => e.Value, v => EnvironmentAccessId.From(v));

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.EnvironmentId)
            .HasConversion(id => id.Value, value => EnvironmentId.From(value))
            .IsRequired();

        builder.Property(x => x.GrantedBy)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.AccessLevel)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.GrantedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.TenantId, x.EnvironmentId })
            .HasDatabaseName("IX_identity_environment_accesses_user_tenant_env");

        // Índice para processamento de expiração por jobs periódicos (ListExpiredAccessesAsync).
        builder.HasIndex(x => new { x.IsActive, x.ExpiresAt })
            .HasDatabaseName("IX_identity_environment_accesses_active_expires");
    }
}
