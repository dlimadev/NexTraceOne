using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para DashboardComment (V3.7 — Real-time Collaboration).</summary>
internal sealed class DashboardCommentConfiguration : IEntityTypeConfiguration<DashboardComment>
{
    public void Configure(EntityTypeBuilder<DashboardComment> builder)
    {
        builder.ToTable("gov_dashboard_comments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DashboardCommentId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AuthorUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WidgetId).HasMaxLength(100);
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.MentionsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(100);
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EditedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.DashboardId, x.TenantId })
            .HasDatabaseName("ix_gov_dash_comments_dashboard_tenant");

        builder.HasIndex(x => new { x.DashboardId, x.WidgetId })
            .HasDatabaseName("ix_gov_dash_comments_widget");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_gov_dash_comments_created_at");
    }
}
