using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ConfigurationAuditEntry.
/// Define mapeamento de tabela, typed ID, FK, índices para consultas de auditoria.
/// A entidade é imutável — registos de auditoria nunca são atualizados após criação.
/// </summary>
internal sealed class ConfigurationAuditEntryConfiguration : IEntityTypeConfiguration<ConfigurationAuditEntry>
{
    public void Configure(EntityTypeBuilder<ConfigurationAuditEntry> builder)
    {
        builder.ToTable("cfg_audit_entries");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ConfigurationAuditEntryId(value));

        builder.Property(x => x.EntryId)
            .HasConversion(id => id.Value, value => new ConfigurationEntryId(value))
            .IsRequired();

        builder.Property(x => x.Key)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeReferenceId)
            .HasMaxLength(256);

        builder.Property(x => x.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PreviousValue)
            .HasMaxLength(4000);

        builder.Property(x => x.NewValue)
            .HasMaxLength(4000);

        builder.Property(x => x.ChangedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ChangedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ChangeReason)
            .HasMaxLength(500);

        // FK: ConfigurationAuditEntry → ConfigurationEntry
        builder.HasOne<ConfigurationEntry>()
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para consultas de auditoria frequentes
        builder.HasIndex(x => x.Key);
        builder.HasIndex(x => x.ChangedAt);
        builder.HasIndex(x => x.EntryId);
        builder.HasIndex(x => x.ChangedBy);
    }
}
