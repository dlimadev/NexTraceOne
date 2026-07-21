using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class RulesetBindingConfiguration : IEntityTypeConfiguration<RulesetBinding>
{
    public void Configure(EntityTypeBuilder<RulesetBinding> builder)
    {
        builder.Property(x => x.RulesetId)
            .HasConversion(id => id.Value, value => new RulesetId(value));
        builder.HasIndex(x => x.RulesetId);
    }
}
