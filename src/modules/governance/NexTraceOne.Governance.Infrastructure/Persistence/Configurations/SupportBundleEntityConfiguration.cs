using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade SupportBundle.</summary>
internal sealed class SupportBundleEntityConfiguration : IEntityTypeConfiguration<SupportBundle>
{
    public void Configure(EntityTypeBuilder<SupportBundle> builder)
    {
        builder.ToTable("gov_support_bundles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SupportBundleId(value));

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RequestedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.SizeMb);

        builder.Property(x => x.ZipContent);

        builder.Property(x => x.IncludesLogs).IsRequired();
        builder.Property(x => x.IncludesConfig).IsRequired();
        builder.Property(x => x.IncludesDb).IsRequired();

        builder.Property(x => x.TenantId);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RequestedAt);
    }
}
