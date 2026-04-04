using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Templates.Persistence.Configurations;

/// <summary>
/// Configuração EF Core do ServiceTemplate.
/// Mapeamento de propriedades, índices e constraints da tabela tpl_service_templates.
/// </summary>
internal sealed class ServiceTemplateConfiguration : IEntityTypeConfiguration<ServiceTemplate>
{
    public void Configure(EntityTypeBuilder<ServiceTemplate> builder)
    {
        builder.ToTable("tpl_service_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new ServiceTemplateId(value))
            .HasColumnType("uuid");

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.Version)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(t => t.ServiceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Language)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.DefaultDomain)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.DefaultTeam)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Tags)
            .HasConversion(
                tags => System.Text.Json.JsonSerializer.Serialize(tags, (System.Text.Json.JsonSerializerOptions?)null),
                json => (IReadOnlyList<string>)(System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()))
            .HasColumnType("text");

        builder.Property(t => t.GovernancePolicyIds)
            .HasConversion(
                ids => System.Text.Json.JsonSerializer.Serialize(ids, (System.Text.Json.JsonSerializerOptions?)null),
                json => (IReadOnlyList<Guid>)(System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>()))
            .HasColumnType("text");

        builder.Property(t => t.BaseContractSpec)
            .HasColumnType("text");

        builder.Property(t => t.ScaffoldingManifestJson)
            .HasColumnType("text");

        builder.Property(t => t.RepositoryTemplateUrl)
            .HasMaxLength(1000);

        builder.Property(t => t.RepositoryTemplateBranch)
            .HasMaxLength(200);

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.UsageCount)
            .IsRequired();

        builder.Property(t => t.TenantId)
            .HasColumnType("uuid");

        // Auditable fields
        builder.Property(t => t.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(t => t.CreatedBy).HasMaxLength(200);
        builder.Property(t => t.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(t => t.UpdatedBy).HasMaxLength(200);
        builder.Property(t => t.IsDeleted).IsRequired();

        // Índice único em Slug para garantir unicidade do identificador do template
        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("IX_tpl_service_templates_slug");

        // Índice por TenantId para queries multi-tenancy e RLS
        builder.HasIndex(t => t.TenantId)
            .HasDatabaseName("IX_tpl_service_templates_tenant");

        // Índice composto para listagem por tipo de serviço e linguagem
        builder.HasIndex(t => new { t.ServiceType, t.Language })
            .HasDatabaseName("IX_tpl_service_templates_type_lang");

        // Filtro global: soft-delete
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
