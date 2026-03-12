using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("licensing_licenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LicenseId.From(value));
        builder.Property(x => x.LicenseKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.MaxActivations).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.LicenseKey).IsUnique();

        builder.HasMany(x => x.Capabilities)
            .WithOne()
            .HasForeignKey("LicenseId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Activations)
            .WithOne()
            .HasForeignKey("LicenseId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.UsageQuotas)
            .WithOne()
            .HasForeignKey("LicenseId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HardwareBinding)
            .WithOne()
            .HasForeignKey<HardwareBinding>("LicenseId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LicenseCapabilityConfiguration : IEntityTypeConfiguration<LicenseCapability>
{
    public void Configure(EntityTypeBuilder<LicenseCapability> builder)
    {
        builder.ToTable("licensing_capabilities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LicenseCapabilityId.From(value));
        builder.Property(x => x.Code).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
    }
}

internal sealed class HardwareBindingConfiguration : IEntityTypeConfiguration<HardwareBinding>
{
    public void Configure(EntityTypeBuilder<HardwareBinding> builder)
    {
        builder.ToTable("licensing_hardware_bindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HardwareBindingId.From(value));
        builder.Property(x => x.Fingerprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BoundAt).IsRequired();
        builder.Property(x => x.LastValidatedAt).IsRequired();
    }
}

internal sealed class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> builder)
    {
        builder.ToTable("licensing_activations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => LicenseActivationId.From(value));
        builder.Property(x => x.HardwareFingerprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActivatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ActivatedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
    }
}

internal sealed class UsageQuotaConfiguration : IEntityTypeConfiguration<UsageQuota>
{
    public void Configure(EntityTypeBuilder<UsageQuota> builder)
    {
        builder.ToTable("licensing_usage_quotas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UsageQuotaId.From(value));
        builder.Property(x => x.MetricCode).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Limit).IsRequired();
        builder.Property(x => x.CurrentUsage).IsRequired();
        builder.Property(x => x.AlertThresholdPercentage).HasPrecision(5, 4).IsRequired();
    }
}
