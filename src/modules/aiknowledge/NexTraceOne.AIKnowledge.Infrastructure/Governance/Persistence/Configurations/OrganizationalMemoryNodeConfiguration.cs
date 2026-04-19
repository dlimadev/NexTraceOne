using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

public sealed class OrganizationalMemoryNodeConfiguration : IEntityTypeConfiguration<OrganizationalMemoryNode>
{
    public void Configure(EntityTypeBuilder<OrganizationalMemoryNode> builder)
    {
        builder.ToTable("aik_memory_nodes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => OrganizationalMemoryNodeId.From(value));

        builder.Property(e => e.NodeType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Content).HasColumnType("text");
        builder.Property(e => e.Context).HasMaxLength(2000);
        builder.Property(e => e.ActorId).HasMaxLength(200);
        builder.Property(e => e.TagsJson).HasColumnType("text");
        builder.Property(e => e.LinkedNodeIdsJson).HasColumnType("text");
        builder.Property(e => e.SourceType).HasMaxLength(100);
        builder.Property(e => e.SourceId).HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.Subject, e.TenantId });
        builder.HasIndex(e => e.NodeType);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
