using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade RunbookRecord.</summary>
internal sealed class RunbookRecordConfiguration : IEntityTypeConfiguration<RunbookRecord>
{
    public void Configure(EntityTypeBuilder<RunbookRecord> builder)
    {
        builder.ToTable("ops_runbooks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RunbookRecordId.From(value));

        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.LinkedService).HasMaxLength(200);
        builder.Property(x => x.LinkedIncidentType).HasMaxLength(200);
        builder.Property(x => x.StepsJson).HasColumnType("jsonb");
        builder.Property(x => x.PrerequisitesJson).HasColumnType("jsonb");
        builder.Property(x => x.PostNotes).HasMaxLength(4000);
        builder.Property(x => x.MaintainedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PublishedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastReviewedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.LinkedService);
    }
}
