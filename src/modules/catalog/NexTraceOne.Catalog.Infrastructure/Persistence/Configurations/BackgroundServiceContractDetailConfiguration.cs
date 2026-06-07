using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class BackgroundServiceContractDetailConfiguration : IEntityTypeConfiguration<BackgroundServiceContractDetail>
{
    public void Configure(EntityTypeBuilder<BackgroundServiceContractDetail> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BackgroundServiceContractDetailId(value));
        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));
        builder.Property(x => x.ServiceName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TriggerType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScheduleExpression).HasMaxLength(100);
        builder.Property(x => x.TimeoutExpression).HasMaxLength(50);
        builder.Property(x => x.MessagingRole).HasMaxLength(50).IsRequired();
    }
}
