using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class MqMessageContractConfiguration : IEntityTypeConfiguration<MqMessageContract>
{
    public void Configure(EntityTypeBuilder<MqMessageContract> builder)
    {
        builder.ToTable("cat_mq_contracts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MqMessageContractId.From(value));

        // ── FK para MainframeSystem ───────────────────────────────────
        builder.Property(x => x.SystemId)
            .HasConversion(id => id.Value, value => MainframeSystemId.From(value));
        builder.HasOne<MainframeSystem>()
            .WithMany()
            .HasForeignKey(x => x.SystemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK opcional para Copybook ─────────────────────────────────
        builder.Property(x => x.CopybookReference)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : CopybookId.From(value.Value));

        // ── Propriedades ──────────────────────────────────────────────
        builder.Property(x => x.QueueName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MessageFormat).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PayloadSchema).HasMaxLength(4000);
        builder.Property(x => x.HeaderFormat).HasMaxLength(200);
        builder.Property(x => x.EncodingScheme).HasMaxLength(50);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.QueueName, x.SystemId }).IsUnique();
        builder.HasIndex(x => x.SystemId);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
