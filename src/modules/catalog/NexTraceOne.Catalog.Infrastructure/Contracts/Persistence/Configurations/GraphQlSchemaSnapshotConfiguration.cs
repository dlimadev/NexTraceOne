using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade GraphQlSchemaSnapshot.
/// Armazena snapshots analisados de schemas GraphQL para diff e auditoria.
/// Wave G.3 — GraphQL Schema Analysis.
/// </summary>
internal sealed class GraphQlSchemaSnapshotConfiguration : IEntityTypeConfiguration<GraphQlSchemaSnapshot>
{
    public void Configure(EntityTypeBuilder<GraphQlSchemaSnapshot> builder)
    {
        builder.ToTable("ctr_graphql_schema_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => GraphQlSchemaSnapshotId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ContractVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SchemaContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.TypeCount).IsRequired();
        builder.Property(x => x.FieldCount).IsRequired();
        builder.Property(x => x.OperationCount).IsRequired();
        builder.Property(x => x.TypeNamesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.OperationsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.FieldsByTypeJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.HasQueryType).IsRequired();
        builder.Property(x => x.HasMutationType).IsRequired();
        builder.Property(x => x.HasSubscriptionType).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CapturedAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.ApiAssetId, x.TenantId, x.CapturedAt })
            .HasDatabaseName("ix_ctr_graphql_snapshots_api_tenant_captured");

        builder.HasIndex(x => new { x.TenantId, x.CapturedAt })
            .HasDatabaseName("ix_ctr_graphql_snapshots_tenant_captured");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
