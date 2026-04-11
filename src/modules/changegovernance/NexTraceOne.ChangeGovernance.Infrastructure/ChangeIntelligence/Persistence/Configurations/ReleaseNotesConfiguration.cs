using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade ReleaseNotes para a tabela chg_release_notes.</summary>
internal sealed class ReleaseNotesConfiguration : IEntityTypeConfiguration<ReleaseNotes>
{
    public void Configure(EntityTypeBuilder<ReleaseNotes> builder)
    {
        builder.ToTable("chg_release_notes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseNotesId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();

        builder.Property(x => x.TechnicalSummary).IsRequired();
        builder.Property(x => x.ExecutiveSummary);
        builder.Property(x => x.NewEndpointsSection);
        builder.Property(x => x.BreakingChangesSection);
        builder.Property(x => x.AffectedServicesSection);
        builder.Property(x => x.ConfidenceMetricsSection);
        builder.Property(x => x.EvidenceLinksSection);

        builder.Property(x => x.ModelUsed).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TokensUsed).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.LastRegeneratedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RegenerationCount).IsRequired();

        builder.HasIndex(x => x.ReleaseId).IsUnique();
        builder.HasIndex(x => x.GeneratedAt);
    }
}
