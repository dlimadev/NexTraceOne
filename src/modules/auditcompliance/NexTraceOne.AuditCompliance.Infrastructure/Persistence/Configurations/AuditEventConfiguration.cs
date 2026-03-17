using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    /// <summary>Configura o mapeamento da entidade AuditEvent para a tabela aud_audit_events.</summary>
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("aud_audit_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AuditEventId.From(value));

        builder.Property(x => x.SourceModule).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActionType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ResourceType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PerformedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("text");

        builder.HasOne(x => x.ChainLink)
            .WithOne()
            .HasForeignKey<AuditChainLink>("AuditEventId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.SourceModule);
        builder.HasIndex(x => x.ActionType);
        builder.HasIndex(x => x.PerformedBy);
    }
}
