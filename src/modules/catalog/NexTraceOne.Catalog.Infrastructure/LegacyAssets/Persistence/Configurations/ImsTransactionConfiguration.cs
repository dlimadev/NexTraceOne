using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Configurations;

internal sealed class ImsTransactionConfiguration : IEntityTypeConfiguration<ImsTransaction>
{
    public void Configure(EntityTypeBuilder<ImsTransaction> builder)
    {
        builder.ToTable("cat_ims_transactions", t =>
        {
            t.HasCheckConstraint(
                "CK_cat_ims_transactions_transaction_type",
                "\"TransactionType\" IN ('MPP', 'BMP', 'FastPath', 'IFP')");
            t.HasCheckConstraint(
                "CK_cat_ims_transactions_criticality",
                "\"Criticality\" IN ('Critical', 'High', 'Medium', 'Low')");
            t.HasCheckConstraint(
                "CK_cat_ims_transactions_lifecycle_status",
                "\"LifecycleStatus\" IN ('Planning', 'Development', 'Staging', 'Active', 'Deprecating', 'Deprecated', 'Retired')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ImsTransactionId.From(value));

        // ── Identidade ────────────────────────────────────────────────
        builder.Property(x => x.TransactionCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).HasDefaultValue(string.Empty);
        builder.Property(x => x.Description).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── FK para MainframeSystem ───────────────────────────────────
        builder.Property(x => x.SystemId)
            .HasConversion(id => id.Value, value => MainframeSystemId.From(value));
        builder.HasOne<MainframeSystem>()
            .WithMany()
            .HasForeignKey(x => x.SystemId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Execução ──────────────────────────────────────────────────
        builder.Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(50).HasDefaultValue(ImsTransactionType.MPP);
        builder.Property(x => x.PsbName).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.DbdName).HasMaxLength(200).HasDefaultValue(string.Empty);

        // ── Classificação ─────────────────────────────────────────────
        builder.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(50).HasDefaultValue(Criticality.Medium);
        builder.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(50).HasDefaultValue(LifecycleStatus.Active);

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => new { x.TransactionCode, x.SystemId }).IsUnique();
        builder.HasIndex(x => x.SystemId);
        builder.HasIndex(x => x.Criticality);
        builder.HasIndex(x => x.LifecycleStatus);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
