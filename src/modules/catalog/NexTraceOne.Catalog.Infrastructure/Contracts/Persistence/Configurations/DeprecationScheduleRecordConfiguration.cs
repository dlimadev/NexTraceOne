using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using static NexTraceOne.Catalog.Application.Contracts.Abstractions.IDeprecationScheduleRepository;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade DeprecationScheduleRecord.
/// Wave AV.3 — ScheduleContractDeprecation.
/// </summary>
internal sealed class DeprecationScheduleRecordConfiguration : IEntityTypeConfiguration<DeprecationScheduleRecord>
{
    public void Configure(EntityTypeBuilder<DeprecationScheduleRecord> builder)
    {
        builder.ToTable("ctr_deprecation_schedules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractId).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PlannedDeprecationDate)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(x => x.PlannedSunsetDate)
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.MigrationGuideUrl).HasMaxLength(1000);
        builder.Property(x => x.SuccessorVersionId);
        builder.Property(x => x.NotificationDraftMessage).HasMaxLength(4000);
        builder.Property(x => x.ScheduledByUserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.ScheduledAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.ContractId);
        builder.HasIndex(x => x.TenantId);
    }
}
