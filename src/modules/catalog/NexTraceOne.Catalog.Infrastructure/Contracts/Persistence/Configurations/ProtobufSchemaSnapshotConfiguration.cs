using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ProtobufSchemaSnapshot.
/// Armazena snapshots analisados de schemas Protobuf para diff e auditoria.
/// Wave H.1 — Protobuf Schema Analysis.
/// </summary>
internal sealed class ProtobufSchemaSnapshotConfiguration : IEntityTypeConfiguration<ProtobufSchemaSnapshot>
{
    public void Configure(EntityTypeBuilder<ProtobufSchemaSnapshot> builder)
    {
        builder.ToTable("ctr_protobuf_schema_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ProtobufSchemaSnapshotId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ContractVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SchemaContent).HasColumnType("text").IsRequired();
        builder.Property(x => x.MessageCount).IsRequired();
        builder.Property(x => x.FieldCount).IsRequired();
        builder.Property(x => x.ServiceCount).IsRequired();
        builder.Property(x => x.RpcCount).IsRequired();
        builder.Property(x => x.MessageNamesJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.FieldsByMessageJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.RpcsByServiceJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.Syntax).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CapturedAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => new { x.ApiAssetId, x.TenantId, x.CapturedAt })
            .HasDatabaseName("ix_ctr_protobuf_snapshots_api_tenant_captured");

        builder.HasIndex(x => new { x.TenantId, x.CapturedAt })
            .HasDatabaseName("ix_ctr_protobuf_snapshots_tenant_captured");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
