using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade EnvironmentAccess.
/// Define tabela, conversões de IDs fortemente tipados, constraints e índices.
/// Prefixo env_ — prepara o módulo para a futura baseline de Environment Management.
/// </summary>
internal sealed class EnvironmentAccessConfiguration : IEntityTypeConfiguration<EnvironmentAccess>
{
    public void Configure(EntityTypeBuilder<EnvironmentAccess> builder)
    {
        builder.ToTable("env_environment_accesses", t =>
        {
            t.HasCheckConstraint(
                "CK_env_environment_accesses_access_level",
                "\"AccessLevel\" IN ('read', 'write', 'admin', 'none')");
        });

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
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // FK: EnvironmentAccess → Environment
        builder.HasOne<Domain.Entities.Environment>()
            .WithMany()
            .HasForeignKey(x => x.EnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice composto para verificação rápida de acesso
        builder.HasIndex(x => new { x.UserId, x.TenantId, x.EnvironmentId })
            .HasDatabaseName("IX_env_environment_accesses_user_tenant_env");

        // Índice único: apenas um acesso ativo por utilizador/ambiente/tenant
        builder.HasIndex(x => new { x.UserId, x.EnvironmentId, x.TenantId })
            .IsUnique()
            .HasFilter("\"IsActive\" = true")
            .HasDatabaseName("IX_env_environment_accesses_unique_active");

        // Índice para processamento de expiração por jobs periódicos
        builder.HasIndex(x => new { x.IsActive, x.ExpiresAt })
            .HasDatabaseName("IX_env_environment_accesses_active_expires");

        // Índice por environment_id para consultas por ambiente
        builder.HasIndex(x => x.EnvironmentId)
            .HasDatabaseName("IX_env_environment_accesses_env_id");

        // Índice para acessos ativos por utilizador
        builder.HasIndex(x => new { x.UserId, x.IsActive })
            .HasDatabaseName("IX_env_environment_accesses_user_active");
    }
}
