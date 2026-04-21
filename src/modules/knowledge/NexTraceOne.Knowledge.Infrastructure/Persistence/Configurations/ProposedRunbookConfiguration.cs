using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Infrastructure.Persistence.Configurations;

internal sealed class ProposedRunbookConfiguration : IEntityTypeConfiguration<ProposedRunbook>
{
    public void Configure(EntityTypeBuilder<ProposedRunbook> builder)
    {
        builder.ToTable("knw_proposed_runbooks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => ProposedRunbookId.From(v))
            .HasColumnName("id");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentMarkdown).HasColumnName("content_markdown").IsRequired();
        builder.Property(x => x.SourceIncidentId).HasColumnName("source_incident_id").IsRequired();
        builder.Property(x => x.ServiceName).HasColumnName("service_name").HasMaxLength(200);
        builder.Property(x => x.TeamName).HasColumnName("team_name").HasMaxLength(200);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired().HasConversion<string>();
        builder.Property(x => x.ProposedAt).HasColumnName("proposed_at").IsRequired();
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasMaxLength(200);
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(x => x.ReviewNote).HasColumnName("review_note").HasMaxLength(1000);
        builder.HasIndex(x => x.SourceIncidentId).IsUnique().HasDatabaseName("uix_knw_proposed_runbooks_incident");
    }
}
