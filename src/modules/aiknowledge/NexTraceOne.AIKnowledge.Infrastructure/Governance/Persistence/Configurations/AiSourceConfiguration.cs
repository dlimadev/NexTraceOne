using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiSourceConfiguration : IEntityTypeConfiguration<AiSource>
{
    public void Configure(EntityTypeBuilder<AiSource> builder)
    {
        builder.ToTable("aik_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiSourceId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.SourceType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ConnectionInfo).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AccessPolicyScope).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Classification).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerTeam).HasMaxLength(300).IsRequired();
        builder.Property(x => x.HealthStatus).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.IsEnabled);
        builder.HasIndex(x => x.OwnerTeam);
    }
}
