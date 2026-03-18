using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Configurations;

internal sealed class ExternalAiProviderConfiguration : IEntityTypeConfiguration<ExternalAiProvider>
{
    public void Configure(EntityTypeBuilder<ExternalAiProvider> builder)
    {
        builder.ToTable("ext_ai_providers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalAiProviderId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MaxTokensPerRequest).IsRequired();
        builder.Property(x => x.CostPerToken).HasColumnType("numeric(18,8)").IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Priority);
    }
}
