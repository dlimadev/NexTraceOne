using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade GovernanceWaiver.
/// Define mapeamento de tabela, typed ID, enums e serialização JSON para EvidenceLinks.
/// </summary>
internal sealed class GovernanceWaiverConfiguration : IEntityTypeConfiguration<GovernanceWaiver>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void Configure(EntityTypeBuilder<GovernanceWaiver> builder)
    {
        builder.ToTable("gov_waivers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GovernanceWaiverId(value));

        builder.Property(x => x.PackId)
            .HasConversion(id => id.Value, value => new GovernancePackId(value))
            .IsRequired();

        builder.Property(x => x.RuleId)
            .HasMaxLength(100);

        builder.Property(x => x.Scope)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ScopeType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Justification)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RequestedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RequestedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ReviewedBy)
            .HasMaxLength(200);

        builder.Property(x => x.ReviewedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .HasColumnType("timestamp with time zone");

        // EvidenceLinks armazenados como JSON array
        builder.Property(x => x.EvidenceLinks)
            .HasColumnType("jsonb")
            .HasConversion(
                links => JsonSerializer.Serialize(links, JsonOptions),
                json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                    ?? new List<string>())
            .IsRequired();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.PackId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestedBy);
        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.ExpiresAt);
    }
}
