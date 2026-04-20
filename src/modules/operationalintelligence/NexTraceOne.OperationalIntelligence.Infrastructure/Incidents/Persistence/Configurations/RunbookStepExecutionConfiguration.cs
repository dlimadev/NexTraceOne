using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade RunbookStepExecution.</summary>
internal sealed class RunbookStepExecutionConfiguration : IEntityTypeConfiguration<RunbookStepExecution>
{
    public void Configure(EntityTypeBuilder<RunbookStepExecution> builder)
    {
        builder.ToTable("ops_inc_runbook_step_executions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RunbookStepExecutionId.From(value));

        builder.Property(x => x.RunbookId).IsRequired();
        builder.Property(x => x.StepKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExecutorUserId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExecutionStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.OutputSummary).HasMaxLength(4000);
        builder.Property(x => x.ErrorDetail).HasMaxLength(4000);
        builder.Property(x => x.TenantId);

        builder.HasIndex(x => x.RunbookId);
        builder.HasIndex(x => x.TenantId);
    }
}
