using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class ChangeEventConfiguration : IEntityTypeConfiguration<ChangeEvent>
{
    /// <summary>Configura o mapeamento da entidade ChangeEvent para a tabela ci_change_events.</summary>
    public void Configure(EntityTypeBuilder<ChangeEvent> builder)
    {
        builder.ToTable("chg_change_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ChangeEventId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Source).HasMaxLength(500).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}
