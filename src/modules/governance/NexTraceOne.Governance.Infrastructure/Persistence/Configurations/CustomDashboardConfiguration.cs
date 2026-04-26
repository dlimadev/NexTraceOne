using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade CustomDashboard.
/// Define mapeamento de tabela, typed ID, JSONB para Widgets (posição+config), concorrência otimista e índices.
/// </summary>
internal sealed class CustomDashboardConfiguration : IEntityTypeConfiguration<CustomDashboard>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<CustomDashboard> builder)
    {
        builder.ToTable("gov_custom_dashboards");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CustomDashboardId(value));

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Layout)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Persona)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Widgets)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<DashboardWidget>>(v, _jsonOptions) ?? new List<DashboardWidget>(),
                new ValueComparer<IReadOnlyList<DashboardWidget>>(
                    (a, b) => a != null && b != null &&
                              a.Count == b.Count &&
                              a.Zip(b).All(pair => pair.First.WidgetId == pair.Second.WidgetId),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.WidgetId.GetHashCode())),
                    c => c.ToList()))
            .IsRequired();

        // SharingPolicy substituiu IsShared (bool) em V3.1 — backward-compat via computed property
        builder.Property(x => x.SharingPolicy)
            .HasColumnName("SharingPolicyJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<SharingPolicy>(v, _jsonOptions) ?? SharingPolicy.Private)
            .IsRequired();

        // Variables (tokens): lista de DashboardVariable serializada como JSONB
        builder.Property(x => x.Variables)
            .HasColumnName("VariablesJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<DashboardVariable>>(v, _jsonOptions)
                     ?? new List<DashboardVariable>(),
                new ValueComparer<IReadOnlyList<DashboardVariable>>(
                    (a, b) => a != null && b != null && a.Count == b.Count &&
                              a.Zip(b).All(pair => pair.First.Key == pair.Second.Key),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode())),
                    c => c.ToList()))
            .IsRequired();

        builder.Property(x => x.CurrentRevisionNumber)
            .IsRequired()
            .HasDefaultValue(0);

        // IsShared é computed de SharingPolicy — ignorar persistência direta
        builder.Ignore(x => x.IsShared);

        builder.Property(x => x.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.TeamId)
            .HasMaxLength(100);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // WidgetCount é calculado — ignorar persistência
        builder.Ignore(x => x.WidgetCount);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Persona);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
