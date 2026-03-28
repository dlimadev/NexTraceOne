using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para EvidenceItem.
/// </summary>
internal sealed class EvidenceItemConfiguration : IEntityTypeConfiguration<EvidenceItem>
{
    public void Configure(EntityTypeBuilder<EvidenceItem> builder)
    {
        builder.ToTable("gov_evidence_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EvidenceItemId(value));

        builder.Property(x => x.PackageId)
            .HasConversion(id => id.Value, value => new EvidencePackageId(value))
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.SourceModule)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ReferenceId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RecordedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RecordedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.PackageId);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.RecordedAt);
    }
}
