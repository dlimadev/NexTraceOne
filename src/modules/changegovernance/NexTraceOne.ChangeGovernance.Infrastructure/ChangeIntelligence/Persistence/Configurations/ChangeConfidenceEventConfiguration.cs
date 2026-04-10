using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade ChangeConfidenceEvent para a tabela chg_change_confidence_events.</summary>
internal sealed class ChangeConfidenceEventConfiguration : IEntityTypeConfiguration<ChangeConfidenceEvent>
{
    public void Configure(EntityTypeBuilder<ChangeConfidenceEvent> builder)
    {
        builder.ToTable("chg_change_confidence_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ChangeConfidenceEventId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ConfidenceBefore).IsRequired();
        builder.Property(x => x.ConfidenceAfter).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Details).HasColumnType("jsonb");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Source).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.ReleaseId);
        builder.HasIndex(x => x.OccurredAt);
    }
}
