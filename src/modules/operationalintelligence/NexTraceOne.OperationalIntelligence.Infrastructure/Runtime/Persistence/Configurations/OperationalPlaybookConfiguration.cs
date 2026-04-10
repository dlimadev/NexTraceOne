using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="OperationalPlaybook"/>.</summary>
internal sealed class OperationalPlaybookConfiguration : IEntityTypeConfiguration<OperationalPlaybook>
{
    public void Configure(EntityTypeBuilder<OperationalPlaybook> builder)
    {
        builder.ToTable("ops_operational_playbooks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => OperationalPlaybookId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.Steps).HasColumnType("jsonb").IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LinkedServiceIds).HasColumnType("jsonb");
        builder.Property(x => x.LinkedRunbookIds).HasColumnType("jsonb");
        builder.Property(x => x.Tags).HasColumnType("jsonb");

        builder.Property(x => x.ApprovedByUserId).HasMaxLength(200);
        builder.Property(x => x.ApprovedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeprecatedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExecutionCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LastExecutedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // ── Auditoria ────────────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();

        // ── Índices ──────────────────────────────────────────────────────────
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_operational_playbooks_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.Status }).HasDatabaseName("ix_ops_operational_playbooks_tenant_status");

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
