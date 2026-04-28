using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class TenantLicenseConfiguration : IEntityTypeConfiguration<TenantLicense>
{
    public void Configure(EntityTypeBuilder<TenantLicense> builder)
    {
        builder.ToTable("iam_tenant_licenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => TenantLicenseId.From(v))
            .HasColumnType("uuid");
        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Plan).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IncludedHostUnits).IsRequired();
        builder.Property(x => x.CurrentHostUnits).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ValidUntil).HasColumnType("timestamp with time zone");
        builder.Property(x => x.BillingCycleStart).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId).HasDatabaseName("uix_iam_tenant_licenses_tenant").IsUnique();
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_iam_tenant_licenses_status");
    }
}
