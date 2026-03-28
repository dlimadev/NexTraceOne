using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para EvidencePackage.
/// </summary>
internal sealed class EvidencePackageConfiguration : IEntityTypeConfiguration<EvidencePackage>
{
    public void Configure(EntityTypeBuilder<EvidencePackage> builder)
    {
        builder.ToTable("gov_evidence_packages", t =>
        {
            t.HasCheckConstraint(
                "CK_gov_evidence_packages_status",
                "\"Status\" IN ('Draft', 'Sealed', 'Exported')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new EvidencePackageId(value));

        builder.Property(x => x.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.SealedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasMany<EvidenceItem>("_items")
            .WithOne()
            .HasForeignKey(i => i.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.Status);
    }
}
