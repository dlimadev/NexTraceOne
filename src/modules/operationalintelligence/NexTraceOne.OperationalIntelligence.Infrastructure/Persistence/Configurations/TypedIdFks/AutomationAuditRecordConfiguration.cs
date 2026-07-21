using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a FK typed-id não descoberta pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class AutomationAuditRecordConfiguration : IEntityTypeConfiguration<AutomationAuditRecord>
{
    public void Configure(EntityTypeBuilder<AutomationAuditRecord> builder)
    {
        builder.Property(x => x.WorkflowId)
            .HasConversion(id => id.Value, value => new AutomationWorkflowRecordId(value));
        builder.HasIndex(x => x.WorkflowId);
    }
}
