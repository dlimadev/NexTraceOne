using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para ComplianceGap.
/// </summary>
internal sealed class ComplianceGapConfiguration : IEntityTypeConfiguration<ComplianceGap>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void Configure(EntityTypeBuilder<ComplianceGap> builder)
    {
        builder.ToTable("gov_compliance_gaps");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ComplianceGapId(value));

        builder.Property(x => x.ServiceId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ServiceName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Team)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Domain)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ViolatedPolicyIds)
            .HasColumnType("jsonb")
            .HasConversion(
                links => JsonSerializer.Serialize(links, JsonOptions),
                json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                    ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.ViolationCount)
            .IsRequired();

        builder.Property(x => x.DetectedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.Team);
        builder.HasIndex(x => x.Domain);
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.Severity);
        builder.HasIndex(x => x.DetectedAt);
    }
}
