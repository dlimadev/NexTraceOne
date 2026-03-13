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
