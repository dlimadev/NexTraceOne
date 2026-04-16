using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class WorkItemAssociationConfiguration : IEntityTypeConfiguration<WorkItemAssociation>
{
    /// <summary>Configura o mapeamento da entidade WorkItemAssociation para a tabela chg_workitem_associations.</summary>
    public void Configure(EntityTypeBuilder<WorkItemAssociation> builder)
    {
        builder.ToTable("chg_workitem_associations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => WorkItemAssociationId.From(value));

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.ExternalWorkItemId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExternalSystem).HasColumnType("integer").IsRequired()
            .HasDefaultValue(ExternalWorkItemSystem.Custom);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.WorkItemType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExternalStatus).HasMaxLength(100);
        builder.Property(x => x.ExternalUrl).HasMaxLength(1000);
        builder.Property(x => x.AddedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AddedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RemovedBy).HasMaxLength(500);
        builder.Property(x => x.RemovedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.ReleaseId).HasDatabaseName("ix_chg_workitem_assoc_release_id");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_workitem_assoc_tenant_id");
        builder.HasIndex(x => new { x.ReleaseId, x.ExternalWorkItemId, x.IsActive })
            .HasDatabaseName("ix_chg_workitem_assoc_release_item_active");
    }
}
