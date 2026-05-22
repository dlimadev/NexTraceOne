using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade CodeQualityRecord.
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
internal sealed class CodeQualityRecordConfiguration : IEntityTypeConfiguration<CodeQualityRecord>
{
    public void Configure(EntityTypeBuilder<CodeQualityRecord> builder)
    {
        builder.ToTable("ctr_code_quality_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(400).IsRequired();
        builder.Property(x => x.ProjectKey).HasMaxLength(400).IsRequired();
        builder.Property(x => x.QualityGateStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Coverage).IsRequired();
        builder.Property(x => x.Bugs).IsRequired();
        builder.Property(x => x.Vulnerabilities).IsRequired();
        builder.Property(x => x.CodeSmells).IsRequired();
        builder.Property(x => x.DuplicatedLinesDensity).IsRequired();
        builder.Property(x => x.Branch).HasMaxLength(200);
        builder.Property(x => x.AnalyzedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ServiceId });
        builder.HasIndex(x => x.AnalyzedAt);
    }
}
