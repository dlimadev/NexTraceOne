using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.DeveloperExperience.Entities;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade DeveloperSurvey.</summary>
internal sealed class DeveloperSurveyConfiguration : IEntityTypeConfiguration<DeveloperSurvey>
{
    public void Configure(EntityTypeBuilder<DeveloperSurvey> builder)
    {
        builder.ToTable("dx_developer_surveys");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DeveloperSurveyId.From(value));

        builder.Property(x => x.TeamId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.RespondentId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Period).HasMaxLength(20).IsRequired();
        builder.Property(x => x.NpsScore).IsRequired();
        builder.Property(x => x.ToolSatisfaction).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.ProcessSatisfaction).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.PlatformSatisfaction).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.NpsCategory).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Comments).HasMaxLength(2000);
        builder.Property(x => x.SubmittedAt).IsRequired();

        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.Period);
        builder.HasIndex(x => x.SubmittedAt);
        builder.HasIndex(x => new { x.TeamId, x.Period });
    }
}
