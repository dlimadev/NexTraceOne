using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade SlaDefinition.</summary>
internal sealed class SlaDefinitionConfiguration : IEntityTypeConfiguration<SlaDefinition>
{
    public void Configure(EntityTypeBuilder<SlaDefinition> builder)
    {
        builder.ToTable("ops_sla_definitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SlaDefinitionId.From(value));

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SloDefinitionId)
            .HasConversion(id => id.Value, value => SloDefinitionId.From(value))
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ContractualTargetPercent).HasPrecision(8, 4).IsRequired();
        builder.Property(x => x.Status).HasColumnType("integer").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EffectiveTo).HasColumnType("timestamp with time zone");
        builder.Property(x => x.HasPenaltyClauses).IsRequired();
        builder.Property(x => x.PenaltyNotes).HasMaxLength(2000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(x => x.SloDefinition)
            .WithMany()
            .HasForeignKey(x => x.SloDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.SloDefinitionId });
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
