using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class ReleaseBaselineConfiguration : IEntityTypeConfiguration<ReleaseBaseline>
{
    public void Configure(EntityTypeBuilder<ReleaseBaseline> builder)
    {
        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => new ReleaseId(value));
        builder.HasIndex(x => x.ReleaseId);
    }
}
