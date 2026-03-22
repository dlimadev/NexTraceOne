using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AiExternalInferenceRecordConfiguration : IEntityTypeConfiguration<AiExternalInferenceRecord>
{
    public void Configure(EntityTypeBuilder<AiExternalInferenceRecord> builder)
    {
        builder.ToTable("AiExternalInferenceRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiExternalInferenceRecordId.From(value));

        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OriginalPrompt).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.AdditionalContext).HasMaxLength(8000);
        builder.Property(x => x.Response).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.SensitivityClassification).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PromotionStatus).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReviewedBy).HasMaxLength(200);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PromotionStatus);
    }
}
