using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class ScheduledReportConfiguration : IEntityTypeConfiguration<ScheduledReport>
{
    public void Configure(EntityTypeBuilder<ScheduledReport> builder)
    {
        builder.ToTable("cfg_scheduled_reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ScheduledReportId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReportType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FiltersJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Schedule).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RecipientsJson).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(10).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.LastSentAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}
