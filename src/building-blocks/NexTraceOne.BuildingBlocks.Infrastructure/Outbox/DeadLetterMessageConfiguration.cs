using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

internal sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        builder.ToTable("bb_dead_letter_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MessageType).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.LastException).HasMaxLength(4000);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExhaustedAt);
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
