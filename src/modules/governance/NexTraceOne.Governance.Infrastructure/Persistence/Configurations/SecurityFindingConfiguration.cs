using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.SecurityGate;
using NexTraceOne.Governance.Domain.SecurityGate.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para SecurityFinding.</summary>
internal sealed class SecurityFindingConfiguration : IEntityTypeConfiguration<SecurityFinding>
{
    public void Configure(EntityTypeBuilder<SecurityFinding> builder)
    {
        builder.ToTable("gov_security_findings");
        builder.HasKey(x => x.FindingId);
        builder.Property(x => x.ScanResultId)
            .HasConversion(id => id.Value, value => new SecurityScanResultId(value))
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(x => x.RuleId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Remediation).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CweId).HasMaxLength(30);
        builder.Property(x => x.OwaspCategory).HasMaxLength(50);
        builder.HasIndex(x => x.ScanResultId);
        builder.HasIndex(x => x.Severity);
    }
}
