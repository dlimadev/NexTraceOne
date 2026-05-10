using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions.IIDEUsageRepository;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core de IdeUsageRecord.
/// Wave AK.1 — IDE Context API.
/// </summary>
internal sealed class IdeUsageRecordConfiguration : IEntityTypeConfiguration<IdeUsageRecord>
{
    public void Configure(EntityTypeBuilder<IdeUsageRecord> builder)
    {
        builder.ToTable("dx_ide_usage_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EventType)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(x => x.ResourceName).HasMaxLength(500);
        builder.Property(x => x.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.UserId, x.OccurredAt });
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
    }
}
