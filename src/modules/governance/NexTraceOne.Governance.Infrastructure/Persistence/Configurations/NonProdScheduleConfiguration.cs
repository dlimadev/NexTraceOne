using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade NonProdSchedule.</summary>
internal sealed class NonProdScheduleConfiguration : IEntityTypeConfiguration<NonProdSchedule>
{
    public void Configure(EntityTypeBuilder<NonProdSchedule> builder)
    {
        builder.ToTable("gov_nonprod_schedules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new NonProdScheduleId(value));

        builder.Property(x => x.EnvironmentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EnvironmentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Enabled).IsRequired();
        builder.Property(x => x.ActiveDaysOfWeekJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ActiveFromHour).IsRequired();
        builder.Property(x => x.ActiveToHour).IsRequired();
        builder.Property(x => x.Timezone).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EstimatedSavingPct).IsRequired();
        builder.Property(x => x.KeepActiveUntil).HasColumnType("timestamp with time zone");
        builder.Property(x => x.OverrideReason).HasMaxLength(1000);
        builder.Property(x => x.TenantId).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.EnvironmentId, x.TenantId }).IsUnique();
    }
}
