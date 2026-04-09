using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="PlaybookExecution"/>.</summary>
internal sealed class PlaybookExecutionConfiguration : IEntityTypeConfiguration<PlaybookExecution>
{
    public void Configure(EntityTypeBuilder<PlaybookExecution> builder)
    {
        builder.ToTable("ops_playbook_executions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PlaybookExecutionId.From(value));

        builder.Property(x => x.PlaybookId).IsRequired();
        builder.Property(x => x.PlaybookName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IncidentId);
        builder.Property(x => x.ExecutedByUserId).HasMaxLength(200).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.StepResults).HasColumnType("jsonb");
        builder.Property(x => x.Evidence).HasColumnType("jsonb");
        builder.Property(x => x.Notes).HasMaxLength(5000);

        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

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
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_playbook_executions_tenant_id");
        builder.HasIndex(x => x.PlaybookId).HasDatabaseName("ix_ops_playbook_executions_playbook_id");
        builder.HasIndex(x => new { x.PlaybookId, x.StartedAt }).HasDatabaseName("ix_ops_playbook_executions_playbook_started");

        // ── Soft-delete global filter ────────────────────────────────────────
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
