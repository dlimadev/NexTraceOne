using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core de EventConsumerDeadLetterRecord.
/// Persiste eventos que falharam o processamento para reprocessamento manual.
/// </summary>
internal sealed class EventConsumerDeadLetterRecordConfiguration
    : IEntityTypeConfiguration<EventConsumerDeadLetterRecord>
{
    public void Configure(EntityTypeBuilder<EventConsumerDeadLetterRecord> builder)
    {
        builder.ToTable("int_event_consumer_dead_letters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EventConsumerDeadLetterRecordId(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Topic).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PartitionKey).HasMaxLength(500);
        builder.Property(x => x.Payload).HasColumnType("text").IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.FirstAttemptAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.LastAttemptAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.IsResolved).IsRequired();
        builder.Property(x => x.ResolvedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.IsResolved);
        builder.HasIndex(x => new { x.TenantId, x.IsResolved });
    }
}
