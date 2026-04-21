using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.FinOps.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ServiceCostAllocationRecord"/>.</summary>
internal sealed class ServiceCostAllocationRecordConfiguration : IEntityTypeConfiguration<ServiceCostAllocationRecord>
{
    public void Configure(EntityTypeBuilder<ServiceCostAllocationRecord> builder)
    {
        builder.ToTable("ops_service_cost_allocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ServiceCostAllocationRecordId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TeamId).HasMaxLength(100);
        builder.Property(x => x.DomainName).HasMaxLength(200);
        builder.Property(x => x.Category).HasColumnType("integer").IsRequired();
        builder.Property(x => x.AmountUsd)
            .HasColumnType("numeric(14,4)")
            .HasPrecision(14, 4)
            .IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired().HasDefaultValue("USD");
        builder.Property(x => x.OriginalAmount)
            .HasColumnType("numeric(14,4)")
            .HasPrecision(14, 4)
            .IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.TagsJson).HasMaxLength(2000);
        builder.Property(x => x.Source).HasMaxLength(100);

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.TenantId, x.ServiceName, x.PeriodStart })
            .HasDatabaseName("ix_ops_cost_alloc_tenant_service_period");
        builder.HasIndex(x => new { x.TenantId, x.Environment })
            .HasDatabaseName("ix_ops_cost_alloc_tenant_environment");
        builder.HasIndex(x => x.Category)
            .HasDatabaseName("ix_ops_cost_alloc_category");

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
