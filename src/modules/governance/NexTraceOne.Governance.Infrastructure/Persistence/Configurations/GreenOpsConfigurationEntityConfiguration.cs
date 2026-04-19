using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade GreenOpsConfiguration.</summary>
internal sealed class GreenOpsConfigurationEntityConfiguration : IEntityTypeConfiguration<GreenOpsConfiguration>
{
    public void Configure(EntityTypeBuilder<GreenOpsConfiguration> builder)
    {
        builder.ToTable("gov_greenops_configurations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GreenOpsConfigurationId(value));

        builder.Property(x => x.IntensityFactorKgPerKwh).IsRequired();
        builder.Property(x => x.EsgTargetKgCo2PerMonth).IsRequired();
        builder.Property(x => x.DatacenterRegion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TenantId);
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
