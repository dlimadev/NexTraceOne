using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ServiceMaturityHistory.
/// Tabela append-only; sem actualizações após inserção.
/// Prefixo gov_ — alinhado com a baseline do módulo Governance.
/// </summary>
internal sealed class ServiceMaturityHistoryConfiguration : IEntityTypeConfiguration<ServiceMaturityHistory>
{
    public void Configure(EntityTypeBuilder<ServiceMaturityHistory> builder)
    {
        builder.ToTable("gov_service_maturity_history");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceMaturityHistoryId(value));

        builder.Property(x => x.ServiceId).IsRequired();

        builder.Property(x => x.ServiceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AssessmentId)
            .HasConversion(id => id.Value, value => new ServiceMaturityAssessmentId(value))
            .IsRequired();

        builder.Property(x => x.Level)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ReassessmentCount).IsRequired();

        builder.Property(x => x.RecordedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100);

        builder.HasIndex(x => x.AssessmentId);
        builder.HasIndex(x => x.ServiceId);
        builder.HasIndex(x => x.RecordedAt);
        builder.HasIndex(x => x.TenantId);
    }
}
