using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade GovernancePackVersion.
/// Define mapeamento de tabela, typed ID, enums e serialização JSON para Rules.
/// </summary>
internal sealed class GovernancePackVersionConfiguration : IEntityTypeConfiguration<GovernancePackVersion>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void Configure(EntityTypeBuilder<GovernancePackVersion> builder)
    {
        builder.ToTable("gov_pack_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GovernancePackVersionId(value));

        builder.Property(x => x.PackId)
            .HasConversion(id => id.Value, value => new GovernancePackId(value))
            .IsRequired();

        builder.Property(x => x.Version)
            .HasMaxLength(50)
            .IsRequired();

        // Rules armazenadas como JSON
        builder.Property(x => x.Rules)
            .HasColumnType("jsonb")
            .HasConversion(
                rules => JsonSerializer.Serialize(rules, JsonOptions),
                json => JsonSerializer.Deserialize<List<GovernanceRuleBinding>>(json, JsonOptions)
                    ?? new List<GovernanceRuleBinding>())
            .IsRequired();

        builder.Property(x => x.DefaultEnforcementMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ChangeDescription)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PublishedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.PackId);
        builder.HasIndex(x => new { x.PackId, x.Version }).IsUnique();
        builder.HasIndex(x => x.PublishedAt);
    }
}
