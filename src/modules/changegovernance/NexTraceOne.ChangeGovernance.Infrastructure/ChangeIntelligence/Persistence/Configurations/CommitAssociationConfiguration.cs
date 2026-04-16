using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

internal sealed class CommitAssociationConfiguration : IEntityTypeConfiguration<CommitAssociation>
{
    /// <summary>Configura o mapeamento da entidade CommitAssociation para a tabela chg_commit_associations.</summary>
    public void Configure(EntityTypeBuilder<CommitAssociation> builder)
    {
        builder.ToTable("chg_commit_associations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CommitAssociationId.From(value));

        builder.Property(x => x.CommitSha).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CommitMessage).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CommitAuthor).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CommittedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.BranchName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AssignmentStatus).HasColumnType("integer").IsRequired()
            .HasDefaultValue(CommitAssignmentStatus.Unassigned);
        builder.Property(x => x.AssignedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.AssignedBy).HasMaxLength(500);
        builder.Property(x => x.AssignmentSource).HasMaxLength(100);
        builder.Property(x => x.ExtractedWorkItemRefs).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.ReleaseId)
            .HasConversion(
                id => id != null ? (Guid?)id.Value : null,
                value => value.HasValue ? ReleaseId.From(value.Value) : null);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_commit_assoc_tenant_id");
        builder.HasIndex(x => new { x.ServiceName, x.AssignmentStatus }).HasDatabaseName("ix_chg_commit_assoc_service_status");
        builder.HasIndex(x => x.ReleaseId).HasDatabaseName("ix_chg_commit_assoc_release_id");
        builder.HasIndex(x => new { x.CommitSha, x.ServiceName, x.TenantId })
            .IsUnique()
            .HasDatabaseName("uix_chg_commit_assoc_sha_service_tenant");
    }
}
