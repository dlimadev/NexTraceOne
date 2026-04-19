using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade SamlSsoConfiguration.</summary>
internal sealed class SamlSsoConfigurationEntityConfiguration : IEntityTypeConfiguration<SamlSsoConfiguration>
{
    public void Configure(EntityTypeBuilder<SamlSsoConfiguration> builder)
    {
        builder.ToTable("gov_saml_sso_configurations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SamlSsoConfigurationId(value));

        builder.Property(x => x.EntityId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SsoUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.SloUrl).HasMaxLength(2000);
        builder.Property(x => x.IdpCertificate).HasColumnType("text");
        builder.Property(x => x.JitProvisioningEnabled).IsRequired();
        builder.Property(x => x.DefaultRole).HasMaxLength(100);
        builder.Property(x => x.AttributeMappingsJson).HasColumnType("jsonb");
        builder.Property(x => x.TenantId);
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
