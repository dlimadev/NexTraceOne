using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ContractDiffConfiguration : IEntityTypeConfiguration<ContractDiff>
{
    public void Configure(EntityTypeBuilder<ContractDiff> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractDiffId(value));
        builder.Property(x => x.ContractVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));
        builder.Property(x => x.BaseVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));
        builder.Property(x => x.TargetVersionId)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));
        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.Protocol).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ChangeLevel).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SuggestedSemVer).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Confidence).IsRequired();
        builder.Property(x => x.ComputedAt).IsRequired();

        // Store change entries as JSON documents
        builder.OwnsMany(x => x.BreakingChanges, c =>
        {
            c.ToJson("breaking_changes_json");
        });
        builder.OwnsMany(x => x.NonBreakingChanges, c =>
        {
            c.ToJson("non_breaking_changes_json");
        });
        builder.OwnsMany(x => x.AdditiveChanges, c =>
        {
            c.ToJson("additive_changes_json");
        });

        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.ApiAssetId);
    }
}
