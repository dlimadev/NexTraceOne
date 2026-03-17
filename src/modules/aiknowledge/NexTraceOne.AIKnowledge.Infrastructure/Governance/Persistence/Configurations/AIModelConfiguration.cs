using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
{
    public void Configure(EntityTypeBuilder<AIModel> builder)
    {
        builder.ToTable("ai_gov_models");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIModelId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Status).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Capabilities).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DefaultUseCases).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Provider);
        builder.HasIndex(x => x.Status);
    }
}
