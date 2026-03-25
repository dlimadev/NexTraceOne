using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade IntegrationConnector.
/// Define mapeamento de tabela, typed ID, enums e índices.
/// </summary>
internal sealed class IntegrationConnectorConfiguration : IEntityTypeConfiguration<IntegrationConnector>
{
    public void Configure(EntityTypeBuilder<IntegrationConnector> builder)
    {
        builder.ToTable("int_connectors", t =>
        {
            t.HasCheckConstraint("CK_int_connectors_status",
                "\"Status\" IN ('Pending','Active','Paused','Disabled','Failed','Configuring')");
            t.HasCheckConstraint("CK_int_connectors_health",
                "\"Health\" IN ('Unknown','Healthy','Degraded','Unhealthy','Critical')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IntegrationConnectorId(value));

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ConnectorType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Provider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Endpoint)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Health)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LastSuccessAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastErrorAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.FreshnessLagMinutes);

        builder.Property(x => x.TotalExecutions)
            .IsRequired();

        builder.Property(x => x.SuccessfulExecutions)
            .IsRequired();

        builder.Property(x => x.FailedExecutions)
            .IsRequired();

        builder.Property(x => x.Environment)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Production");

        builder.Property(x => x.AuthenticationMode)
            .HasMaxLength(200)
            .IsRequired()
            .HasDefaultValue("Not configured");

        builder.Property(x => x.PollingMode)
            .HasMaxLength(200)
            .IsRequired()
            .HasDefaultValue("Not configured");

        builder.Property(x => x.AllowedTeams)
            .HasColumnType("jsonb")
            .HasConversion(
                teams => System.Text.Json.JsonSerializer.Serialize(teams, (System.Text.Json.JsonSerializerOptions?)null),
                json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.ConnectorType);
        builder.HasIndex(x => x.Provider);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Health);
        builder.HasIndex(x => x.Environment);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
