using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="KnowledgeGraphSnapshot"/>.</summary>
internal sealed class KnowledgeGraphSnapshotConfiguration : IEntityTypeConfiguration<KnowledgeGraphSnapshot>
{
    public void Configure(EntityTypeBuilder<KnowledgeGraphSnapshot> builder)
    {
        builder.ToTable("knw_knowledge_graph_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => KnowledgeGraphSnapshotId.From(value));

        builder.Property(x => x.TotalNodes).IsRequired();
        builder.Property(x => x.TotalEdges).IsRequired();
        builder.Property(x => x.ConnectedComponents).IsRequired();
        builder.Property(x => x.IsolatedNodes).IsRequired();
        builder.Property(x => x.CoverageScore).IsRequired();

        builder.Property(x => x.NodeTypeDistribution).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.EdgeTypeDistribution).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TopConnectedEntities).HasColumnType("jsonb");
        builder.Property(x => x.OrphanEntities).HasColumnType("jsonb");
        builder.Property(x => x.Recommendations).HasColumnType("jsonb");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReviewComment).HasMaxLength(2000);
        builder.Property(x => x.TenantId);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_knw_knowledge_graph_snapshots_tenant_id");
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.GeneratedAt);
    }
}
