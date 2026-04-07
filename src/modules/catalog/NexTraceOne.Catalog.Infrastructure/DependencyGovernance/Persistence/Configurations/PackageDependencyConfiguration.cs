using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.DependencyGovernance;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;
using NexTraceOne.Catalog.Domain.DependencyGovernance.ValueObjects;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence.Configurations;

internal sealed class PackageDependencyConfiguration : IEntityTypeConfiguration<PackageDependency>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void Configure(EntityTypeBuilder<PackageDependency> builder)
    {
        builder.ToTable("dep_package_dependencies");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnType("uuid");
        builder.Property(d => d.ProfileId)
            .HasConversion(id => id.Value, value => new ServiceDependencyProfileId(value))
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(d => d.PackageName).IsRequired().HasMaxLength(500);
        builder.Property(d => d.Version).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Ecosystem).HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.IsDirect);
        builder.Property(d => d.License).HasMaxLength(200);
        builder.Property(d => d.LicenseRisk).HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.LatestStableVersion).HasMaxLength(200);
        builder.Property(d => d.IsOutdated);
        builder.Property(d => d.DeprecationNotice).HasMaxLength(1000);

        builder.Property(d => d.Vulnerabilities)
            .HasConversion(
                v => JsonSerializer.Serialize(v, SerializerOptions),
                json => (IReadOnlyList<PackageVulnerability>)(JsonSerializer.Deserialize<List<PackageVulnerability>>(json, SerializerOptions) ?? new()))
            .HasColumnType("text");

        builder.HasIndex(d => d.ProfileId).HasDatabaseName("IX_dep_dependencies_profile");
        builder.HasIndex(d => new { d.PackageName, d.Ecosystem }).HasDatabaseName("IX_dep_dependencies_package");
    }
}
