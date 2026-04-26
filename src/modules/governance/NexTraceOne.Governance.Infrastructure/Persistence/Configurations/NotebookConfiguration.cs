using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para Notebook (V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode).
/// Notebook = lista ordenada de células (Markdown, Query, Widget, Action, AI).
/// </summary>
internal sealed class NotebookConfiguration : IEntityTypeConfiguration<Notebook>
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public void Configure(EntityTypeBuilder<Notebook> builder)
    {
        builder.ToTable("gov_notebooks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new NotebookId(value));

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .HasMaxLength(100);

        builder.Property(x => x.Persona)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // SharingPolicy persisted as JSON (same pattern as CustomDashboard)
        builder.Property(x => x.SharingPolicy)
            .HasColumnName("SharingPolicyJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _json),
                v => JsonSerializer.Deserialize<SharingPolicy>(v, _json) ?? SharingPolicy.Private)
            .IsRequired();

        // Cells persisted as JSONB array
        builder.Property(x => x.Cells)
            .HasColumnName("CellsJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _json),
                v => (IReadOnlyList<NotebookCell>)(JsonSerializer.Deserialize<List<NotebookCell>>(v, _json) ?? []))
            .IsRequired();

        builder.Property(x => x.CurrentRevisionNumber)
            .IsRequired();

        builder.Property(x => x.LinkedDashboardId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new CustomDashboardId(value.Value));

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Optimistic concurrency via xmin (PostgreSQL row version)
        builder.UseXminAsConcurrencyToken();

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_gov_notebooks_tenant_status");

        builder.HasIndex(x => new { x.TenantId, x.Persona })
            .HasDatabaseName("ix_gov_notebooks_tenant_persona");

        builder.HasIndex(x => x.CreatedByUserId)
            .HasDatabaseName("ix_gov_notebooks_created_by");
    }
}
