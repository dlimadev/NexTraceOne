using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento da entidade SmtpConfiguration para a tabela ntf_smtp_configurations.
/// Cada tenant pode ter exatamente uma configuração SMTP.
/// A coluna EncryptedPassword armazena a senha cifrada; a cifra é responsabilidade da infraestrutura.
/// </summary>
internal sealed class SmtpConfigurationEntityConfiguration
    : IEntityTypeConfiguration<SmtpConfiguration>
{
    public void Configure(EntityTypeBuilder<SmtpConfiguration> builder)
    {
        builder.ToTable("ntf_smtp_configurations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SmtpConfigurationId(value));

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.Host)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Port).IsRequired();
        builder.Property(x => x.UseSsl).IsRequired();

        builder.Property(x => x.Username)
            .HasMaxLength(500);

        builder.Property(x => x.EncryptedPassword)
            .HasMaxLength(2000);

        builder.Property(x => x.FromAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.FromName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .HasMaxLength(2000);

        builder.Property(x => x.IsEnabled).IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Unicidade: cada tenant tem no máximo uma configuração SMTP
        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
