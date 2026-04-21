using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade ReleaseCalendarEntry.
/// Tabela: chg_release_calendar_entries
/// Wave F.1 — Release Calendar.
/// </summary>
internal sealed class ReleaseCalendarEntryConfiguration : IEntityTypeConfiguration<ReleaseCalendarEntry>
{
    /// <summary>Configura o mapeamento da entidade ReleaseCalendarEntry.</summary>
    public void Configure(EntityTypeBuilder<ReleaseCalendarEntry> builder)
    {
        builder.ToTable("chg_release_calendar_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseCalendarEntryId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.WindowType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.EnvironmentFilter).HasMaxLength(100);
        builder.Property(x => x.StartsAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RecurrenceTag).HasMaxLength(100);
        builder.Property(x => x.ClosedByUserId).HasMaxLength(200);
        builder.Property(x => x.ClosedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_chg_release_calendar_tenant_id");

        builder.HasIndex(x => new { x.TenantId, x.WindowType, x.Status })
            .HasDatabaseName("ix_chg_release_calendar_tenant_type_status");

        builder.HasIndex(x => new { x.TenantId, x.StartsAt, x.EndsAt })
            .HasDatabaseName("ix_chg_release_calendar_tenant_period");
    }
}
