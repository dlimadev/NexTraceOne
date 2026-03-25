using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
{
    public void Configure(EntityTypeBuilder<AIModel> builder)
    {
        builder.ToTable("aik_models", t =>
        {
            t.HasCheckConstraint(
                "CK_aik_models_Status",
                "\"Status\" IN ('Active','Inactive','Deprecated','Blocked')");
            t.HasCheckConstraint(
                "CK_aik_models_ModelType",
                "\"ModelType\" IN ('Chat','Completion','Embedding','CodeGeneration','Analysis')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIModelId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProviderId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : AiProviderId.From(value.Value));
        builder.Property(x => x.ExternalModelId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ModelType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Category).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.Capabilities).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DefaultUseCases).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ContextWindow);
        builder.Property(x => x.RecommendedRamGb).HasColumnType("numeric(5,1)");
        builder.Property(x => x.LicenseName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LicenseUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ComplianceStatus).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Provider);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ProviderId);
        builder.HasIndex(x => x.IsDefaultForChat);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
