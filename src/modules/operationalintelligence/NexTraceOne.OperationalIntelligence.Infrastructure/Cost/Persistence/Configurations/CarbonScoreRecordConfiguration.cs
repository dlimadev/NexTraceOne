using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence.Configurations;

/// <summary>Configuração EF Core de CarbonScoreRecord.</summary>
internal sealed class CarbonScoreRecordConfiguration : IEntityTypeConfiguration<CarbonScoreRecord>
{
    public void Configure(EntityTypeBuilder<CarbonScoreRecord> builder)
    {
        builder.ToTable("oi_carbon_score_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CarbonScoreRecordId.From(value));

        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.CpuHours).IsRequired();
        builder.Property(x => x.MemoryGbHours).IsRequired();
        builder.Property(x => x.NetworkGb).IsRequired();
        builder.Property(x => x.CarbonGrams).IsRequired();
        builder.Property(x => x.IntensityFactor).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.Date }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Date });
    }
}
