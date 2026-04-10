using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade ContractVerification.
/// Prefixo ctr_ — alinhado com a baseline do módulo Catalog (Contracts).
/// </summary>
internal sealed class ContractVerificationConfiguration : IEntityTypeConfiguration<ContractVerification>
{
    public void Configure(EntityTypeBuilder<ContractVerification> builder)
    {
        builder.ToTable("ctr_contract_verifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractVerificationId.From(value));

        builder.Property(x => x.ApiAssetId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ServiceName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.ContractVersionId);

        builder.Property(x => x.SpecContentHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BreakingChangesCount)
            .IsRequired();

        builder.Property(x => x.NonBreakingChangesCount)
            .IsRequired();

        builder.Property(x => x.AdditiveChangesCount)
            .IsRequired();

        builder.Property(x => x.DiffDetails)
            .HasColumnType("jsonb");

        builder.Property(x => x.ComplianceViolations)
            .HasColumnType("jsonb");

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SourceBranch)
            .HasMaxLength(500);

        builder.Property(x => x.CommitSha)
            .HasMaxLength(100);

        builder.Property(x => x.PipelineId)
            .HasMaxLength(200);

        builder.Property(x => x.EnvironmentName)
            .HasMaxLength(200);

        builder.Property(x => x.VerifiedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200);

        builder.Property(x => x.TenantId)
            .HasMaxLength(100);

        builder.Property(x => x.RowVersion).IsRowVersion();

        // Indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.ApiAssetId, x.Status });
    }
}
