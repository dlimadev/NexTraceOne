using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.RulesetGovernance.Domain.Entities;

namespace NexTraceOne.RulesetGovernance.Infrastructure.Persistence.Configurations;

internal sealed class RulesetBindingConfiguration : IEntityTypeConfiguration<RulesetBinding>
{
    /// <summary>Configura o mapeamento da entidade RulesetBinding para a tabela rg_ruleset_bindings.</summary>
    public void Configure(EntityTypeBuilder<RulesetBinding> builder)
    {
        builder.ToTable("rg_ruleset_bindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RulesetBindingId.From(value));

        builder.Property(x => x.RulesetId)
            .HasConversion(id => id.Value, value => RulesetId.From(value))
            .IsRequired();
        builder.Property(x => x.AssetType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BindingCreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.RulesetId, x.AssetType }).IsUnique();
    }
}
