using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class CopybookDiffRecordConfiguration : IEntityTypeConfiguration<CopybookDiffRecord>
{
    public void Configure(EntityTypeBuilder<CopybookDiffRecord> builder)
    {
        builder.Property(x => x.CopybookId)
            .HasConversion(id => id.Value, value => new CopybookId(value));
        builder.HasIndex(x => x.CopybookId);
        builder.Property(x => x.BaseVersionId)
            .HasConversion(id => id.Value, value => new CopybookVersionId(value));
        builder.HasIndex(x => x.BaseVersionId);
        builder.Property(x => x.TargetVersionId)
            .HasConversion(id => id.Value, value => new CopybookVersionId(value));
        builder.HasIndex(x => x.TargetVersionId);
    }
}
