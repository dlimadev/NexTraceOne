using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Tenant.
/// Define tabela, índices e constraints para o aggregate root de organização.
/// O slug possui índice único para garantir identificação amigável sem colisões.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("identity_tenants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => TenantId.From(value));

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("IX_identity_tenants_slug");
    }
}
