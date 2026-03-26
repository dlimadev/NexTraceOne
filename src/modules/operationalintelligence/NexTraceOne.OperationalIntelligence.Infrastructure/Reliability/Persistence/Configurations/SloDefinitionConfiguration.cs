using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade SloDefinition.</summary>
internal sealed class SloDefinitionConfiguration : IEntityTypeConfiguration<SloDefinition>
{
    public void Configure(EntityTypeBuilder<SloDefinition> builder)
    {
        builder.ToTable("ops_slo_definitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SloDefinitionId.From(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Type).HasColumnType("integer").IsRequired();
        builder.Property(x => x.TargetPercent).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.AlertThresholdPercent).HasPrecision(8, 4);
        builder.Property(x => x.WindowDays).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.Environment });
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
