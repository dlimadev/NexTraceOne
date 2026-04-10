using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ContractChangelog.
/// Prefixo ctr_ — alinhado com a baseline do módulo Catalog (Contracts).
/// </summary>
internal sealed class ContractChangelogConfiguration : IEntityTypeConfiguration<ContractChangelog>
{
    public void Configure(EntityTypeBuilder<ContractChangelog> builder)
    {
        builder.ToTable("ctr_contract_changelogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractChangelogId.From(value));

        builder.Property(x => x.TenantId)
            .HasMaxLength(100);

        builder.Property(x => x.ApiAssetId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ServiceName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.FromVersion)
            .HasMaxLength(50);

        builder.Property(x => x.ToVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ContractVersionId)
            .IsRequired();

        builder.Property(x => x.VerificationId);

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Entries)
            .HasColumnType("jsonb");

        builder.Property(x => x.Summary)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.MarkdownContent);

        builder.Property(x => x.JsonContent);

        builder.Property(x => x.IsApproved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ApprovedBy)
            .HasMaxLength(200);

        builder.Property(x => x.ApprovedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CommitSha)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.IsApproved);
        builder.HasIndex(x => new { x.ApiAssetId, x.IsApproved });
    }
}
