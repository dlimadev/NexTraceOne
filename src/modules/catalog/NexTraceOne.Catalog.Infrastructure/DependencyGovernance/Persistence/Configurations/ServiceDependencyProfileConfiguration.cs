using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.DependencyGovernance;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Entities;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence.Configurations;

internal sealed class ServiceDependencyProfileConfiguration : IEntityTypeConfiguration<ServiceDependencyProfile>
{
    public void Configure(EntityTypeBuilder<ServiceDependencyProfile> builder)
    {
        builder.ToTable("dep_service_dependency_profiles");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new ServiceDependencyProfileId(value))
            .HasColumnType("uuid");

        builder.Property(p => p.ServiceId).HasColumnType("uuid").IsRequired();
        builder.Property(p => p.TemplateId).HasColumnType("uuid");
        builder.Property(p => p.LastScanAt).HasColumnType("timestamp with time zone");
        builder.Property(p => p.SbomFormat).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.SbomContent).HasColumnType("text");
        builder.Property(p => p.HealthScore).IsRequired();
        builder.Property(p => p.TotalDependencies).IsRequired();
        builder.Property(p => p.DirectDependencies).IsRequired();
        builder.Property(p => p.TransitiveDependencies).IsRequired();

        builder.Property(p => p.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(p => p.CreatedBy).HasMaxLength(200);
        builder.Property(p => p.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(p => p.UpdatedBy).HasMaxLength(200);
        builder.Property(p => p.IsDeleted).IsRequired();

        builder.HasMany(p => p.Dependencies)
            .WithOne()
            .HasForeignKey(d => d.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Dependencies)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.ServiceId).HasDatabaseName("IX_dep_profiles_service");
        builder.HasIndex(p => p.TemplateId).HasDatabaseName("IX_dep_profiles_template");
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
