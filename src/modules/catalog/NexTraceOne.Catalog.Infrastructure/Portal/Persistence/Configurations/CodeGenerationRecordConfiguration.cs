using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade CodeGenerationRecord.
/// Mapeia registos de geração de código a partir de contratos OpenAPI.
/// </summary>
internal sealed class CodeGenerationRecordConfiguration : IEntityTypeConfiguration<CodeGenerationRecord>
{
    public void Configure(EntityTypeBuilder<CodeGenerationRecord> builder)
    {
        builder.ToTable("dp_code_generation_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => CodeGenerationRecordId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ApiName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContractVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RequestedById).IsRequired();
        builder.Property(x => x.Language).HasMaxLength(50).IsRequired();
        builder.Property(x => x.GenerationType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.GeneratedCode).HasColumnType("text").IsRequired();
        builder.Property(x => x.IsAiGenerated).IsRequired();
        builder.Property(x => x.TemplateId).HasMaxLength(200);
        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => x.RequestedById);
    }
}
