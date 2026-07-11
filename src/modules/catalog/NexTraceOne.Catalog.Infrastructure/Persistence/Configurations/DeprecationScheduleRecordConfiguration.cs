using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class DeprecationScheduleRecordConfiguration : IEntityTypeConfiguration<DeprecationScheduleRecord>
{
    public void Configure(EntityTypeBuilder<DeprecationScheduleRecord> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ScheduledByUserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MigrationGuideUrl).HasMaxLength(1000);
        builder.Property(x => x.Reason).HasMaxLength(2000);

        // Garante o upsert por ContractId: um único agendamento de deprecação por contrato.
        builder.HasIndex(x => x.ContractId).IsUnique();
    }
}
