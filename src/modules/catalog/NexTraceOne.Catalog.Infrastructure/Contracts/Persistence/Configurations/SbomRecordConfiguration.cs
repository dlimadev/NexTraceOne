using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade SbomRecord.
/// Os componentes SBOM são serializados como jsonb.
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance.
/// </summary>
internal sealed class SbomRecordConfiguration : IEntityTypeConfiguration<SbomRecord>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<SbomRecord> builder)
    {
        builder.ToTable("ctr_sbom_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RecordedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Components)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => (IReadOnlyList<SbomComponent>)(
                    JsonSerializer.Deserialize<List<SbomComponent>>(v, JsonOptions) ?? new List<SbomComponent>()));

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ServiceId });
    }
}
