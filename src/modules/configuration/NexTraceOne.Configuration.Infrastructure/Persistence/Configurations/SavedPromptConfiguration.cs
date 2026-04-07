using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class SavedPromptConfiguration : IEntityTypeConfiguration<SavedPrompt>
{
    public void Configure(EntityTypeBuilder<SavedPrompt> builder)
    {
        builder.ToTable("cfg_saved_prompts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SavedPromptId(value));
        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptText).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ContextType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TagsCsv).HasMaxLength(500);
        builder.Property(x => x.IsShared).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.TenantId });
    }
}
