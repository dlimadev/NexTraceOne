using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiProviderConfiguration : IEntityTypeConfiguration<AiProvider>
{
    public void Configure(EntityTypeBuilder<AiProvider> builder)
    {
        builder.ToTable("AiProviders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiProviderId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProviderType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BaseUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AuthenticationMode).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.SupportedCapabilities).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.HealthStatus).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.TimeoutSeconds).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.ProviderType);
        builder.HasIndex(x => x.IsEnabled);
    }
}
