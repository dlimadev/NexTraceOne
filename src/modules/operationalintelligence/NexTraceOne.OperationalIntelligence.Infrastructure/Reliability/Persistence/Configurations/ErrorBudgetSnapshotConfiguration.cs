using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade ErrorBudgetSnapshot.</summary>
internal sealed class ErrorBudgetSnapshotConfiguration : IEntityTypeConfiguration<ErrorBudgetSnapshot>
{
    public void Configure(EntityTypeBuilder<ErrorBudgetSnapshot> builder)
    {
        builder.ToTable("ops_error_budget_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ErrorBudgetSnapshotId.From(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SloDefinitionId)
            .HasConversion(id => id.Value, value => SloDefinitionId.From(value))
            .IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TotalBudgetMinutes).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ConsumedBudgetMinutes).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.RemainingBudgetMinutes).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.ConsumedPercent).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired();
        builder.Property(x => x.ComputedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne(x => x.SloDefinition)
            .WithMany()
            .HasForeignKey(x => x.SloDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.ComputedAt });
        builder.HasIndex(x => new { x.TenantId, x.SloDefinitionId, x.ComputedAt });

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
