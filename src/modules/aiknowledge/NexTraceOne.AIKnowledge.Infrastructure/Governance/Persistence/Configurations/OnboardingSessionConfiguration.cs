using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade OnboardingSession.
/// Tabela: ai_onboarding_sessions.
/// </summary>
internal sealed class OnboardingSessionConfiguration : IEntityTypeConfiguration<OnboardingSession>
{
    public void Configure(EntityTypeBuilder<OnboardingSession> builder)
    {
        builder.ToTable("ai_onboarding_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => OnboardingSessionId.From(value));

        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TeamId).IsRequired();
        builder.Property(x => x.TeamName).HasMaxLength(300).IsRequired();

        builder.Property(x => x.ExperienceLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ChecklistItems).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CompletedItems).IsRequired();
        builder.Property(x => x.TotalItems).IsRequired();
        builder.Property(x => x.ProgressPercent).IsRequired();

        builder.Property(x => x.ServicesExplored).HasColumnType("jsonb");
        builder.Property(x => x.ContractsReviewed).HasColumnType("jsonb");
        builder.Property(x => x.RunbooksRead).HasColumnType("jsonb");

        builder.Property(x => x.AiInteractionCount).IsRequired();
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.StartedAt);
    }
}
