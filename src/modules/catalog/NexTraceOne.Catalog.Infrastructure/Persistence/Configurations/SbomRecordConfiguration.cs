using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class SbomRecordConfiguration : IEntityTypeConfiguration<SbomRecord>
{
    public void Configure(EntityTypeBuilder<SbomRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).HasMaxLength(200);
        builder.Property(x => x.ServiceId).HasMaxLength(200);
        builder.Property(x => x.ServiceName).HasMaxLength(500);
        builder.Property(x => x.Version).HasMaxLength(100);

        // Store components as JSON document (no separate table, no primary key needed)
        builder.OwnsMany(x => x.Components, c =>
        {
            c.ToJson("components_json");
        });
    }
}
