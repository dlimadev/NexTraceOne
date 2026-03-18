using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.ExternalAI.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence.Configurations;

internal sealed class ExternalAiConsultationConfiguration : IEntityTypeConfiguration<ExternalAiConsultation>
{
    public void Configure(EntityTypeBuilder<ExternalAiConsultation> builder)
    {
        builder.ToTable("ext_ai_consultations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalAiConsultationId.From(value));

        builder.Property(x => x.ProviderId)
            .HasConversion(id => id.Value, value => ExternalAiProviderId.From(value))
            .IsRequired();

        builder.Property(x => x.Context).HasMaxLength(10000).IsRequired();
        builder.Property(x => x.Query).HasMaxLength(10000).IsRequired();
        builder.Property(x => x.Response).HasMaxLength(50000);
        builder.Property(x => x.TokensUsed).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Confidence).HasColumnType("numeric(5,4)").IsRequired();
        builder.Property(x => x.RequestedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ProviderId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedAt);
        builder.HasIndex(x => x.RequestedBy);
    }
}
