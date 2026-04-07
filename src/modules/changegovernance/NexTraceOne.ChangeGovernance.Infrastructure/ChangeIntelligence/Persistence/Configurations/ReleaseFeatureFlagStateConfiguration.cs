using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade ReleaseFeatureFlagState.</summary>
internal sealed class ReleaseFeatureFlagStateConfiguration : IEntityTypeConfiguration<ReleaseFeatureFlagState>
{
    /// <summary>Configura o mapeamento da entidade ReleaseFeatureFlagState para a tabela chg_feature_flag_states.</summary>
    public void Configure(EntityTypeBuilder<ReleaseFeatureFlagState> builder)
    {
        builder.ToTable("chg_feature_flag_states");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ReleaseFeatureFlagStateId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.ActiveFlagCount).IsRequired();
        builder.Property(x => x.CriticalFlagCount).IsRequired();
        builder.Property(x => x.NewFeatureFlagCount).IsRequired();
        builder.Property(x => x.FlagProvider).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FlagsJson).HasColumnType("jsonb");
        builder.Property(x => x.RecordedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ReleaseId);
    }
}
