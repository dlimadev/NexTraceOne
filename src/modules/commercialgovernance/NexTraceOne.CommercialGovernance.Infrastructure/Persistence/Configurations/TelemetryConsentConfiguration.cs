using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade TelemetryConsent.
/// Mapeia o consentimento de telemetria por licença, permitindo que cada tenant
/// controle individualmente a coleta de dados de uso (LGPD/GDPR compliance).
///
/// Decisão de design:
/// - Tabela separada de licenses para gestão independente do consentimento.
/// - FK para License via LicenseId para rastreabilidade.
/// - Status armazenado como inteiro para compatibilidade com enums.
/// - Reason é opcional (texto livre para justificativa da alteração).
/// </summary>
internal sealed class TelemetryConsentConfiguration : IEntityTypeConfiguration<TelemetryConsent>
{
    public void Configure(EntityTypeBuilder<TelemetryConsent> builder)
    {
        builder.ToTable("licensing_telemetry_consents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => TelemetryConsentId.From(value));

        builder.Property(x => x.LicenseId)
            .HasConversion(id => id.Value, value => LicenseId.From(value))
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.AllowUsageMetrics)
            .IsRequired();

        builder.Property(x => x.AllowPerformanceData)
            .IsRequired();

        builder.Property(x => x.AllowErrorDiagnostics)
            .IsRequired();

        builder.HasIndex(x => x.LicenseId)
            .IsUnique();
    }
}
