using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a tabela cat_discovery_match_rules.
/// Regras de matching automático para associar service.name a ServiceAsset.
/// </summary>
internal sealed class DiscoveryMatchRuleConfiguration : IEntityTypeConfiguration<DiscoveryMatchRule>
{
    public void Configure(EntityTypeBuilder<DiscoveryMatchRule> builder)
    {
        builder.ToTable("cat_discovery_match_rules");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DiscoveryMatchRuleId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Pattern).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TargetServiceAssetId).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.IsActive, x.Priority });
    }
}
