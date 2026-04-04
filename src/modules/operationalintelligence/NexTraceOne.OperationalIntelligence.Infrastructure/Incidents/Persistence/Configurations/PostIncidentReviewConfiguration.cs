using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade PostIncidentReview.</summary>
internal sealed class PostIncidentReviewConfiguration : IEntityTypeConfiguration<PostIncidentReview>
{
    public void Configure(EntityTypeBuilder<PostIncidentReview> builder)
    {
        builder.ToTable("ops_post_incident_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PostIncidentReviewId(value));

        builder.Property(x => x.IncidentId).IsRequired();
        builder.Property(x => x.CurrentPhase)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Outcome)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.RootCauseAnalysis).HasMaxLength(5000);
        builder.Property(x => x.PreventiveActionsJson).HasColumnType("jsonb");
        builder.Property(x => x.TimelineNarrative).HasMaxLength(10000);
        builder.Property(x => x.ResponsibleTeam).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Facilitator).HasMaxLength(200);
        builder.Property(x => x.Summary).HasMaxLength(5000);
        builder.Property(x => x.IsCompleted).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.IncidentId).IsUnique();
    }
}
