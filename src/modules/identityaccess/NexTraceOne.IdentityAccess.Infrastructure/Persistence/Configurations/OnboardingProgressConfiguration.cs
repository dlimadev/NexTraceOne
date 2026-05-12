using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core de OnboardingProgress.</summary>
internal sealed class OnboardingProgressConfiguration : IEntityTypeConfiguration<OnboardingProgress>
{
    public void Configure(EntityTypeBuilder<OnboardingProgress> builder)
    {
        builder.ToTable("iam_onboarding_progress");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => OnboardingProgressId.From(value));

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.CurrentStep)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Passos concluídos armazenados como JSONB
        builder.Property(x => x.CompletedStepsJson)
            .HasColumnName("completed_steps_json")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]")
            .IsRequired();

        builder.Property(x => x.SkippedAt);
        builder.Property(x => x.CompletedAt);

        // Cada tenant tem apenas um registo de progresso
        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
