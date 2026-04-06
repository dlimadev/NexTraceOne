using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para SecurityScanResult.</summary>
internal sealed class SecurityScanResultConfiguration : IEntityTypeConfiguration<SecurityScanResult>
{
    public void Configure(EntityTypeBuilder<SecurityScanResult> builder)
    {
        builder.ToTable("gov_security_scan_results");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(id => id.Value, v => new Domain.SecurityGate.SecurityScanResultId(v));
        builder.Property(x => x.TargetType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ScanProvider).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.OverallRisk).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ScannedAt).IsRequired();
        builder.OwnsOne(x => x.Summary, s =>
        {
            s.Property(p => p.TopCategories)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());
        });
        builder.HasMany(x => x.Findings).WithOne().HasForeignKey(f => f.ScanResultId).OnDelete(DeleteBehavior.Cascade);
    }
}
