using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade DelegatedAdministration.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// </summary>
internal sealed class DelegatedAdministrationConfiguration : IEntityTypeConfiguration<DelegatedAdministration>
{
    public void Configure(EntityTypeBuilder<DelegatedAdministration> builder)
    {
        builder.ToTable("gov_delegated_administrations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DelegatedAdministrationId(value));

        builder.Property(x => x.GranteeUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.GranteeDisplayName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasMaxLength(200);

        builder.Property(x => x.DomainId)
            .HasMaxLength(200);

        builder.Property(x => x.Reason)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.GrantedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RevokedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.GranteeUserId);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.DomainId);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
