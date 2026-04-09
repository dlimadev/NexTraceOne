using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="EnvironmentDriftReport"/>.</summary>
internal sealed class EnvironmentDriftReportConfiguration : IEntityTypeConfiguration<EnvironmentDriftReport>
{
    public void Configure(EntityTypeBuilder<EnvironmentDriftReport> builder)
    {
        builder.ToTable("ops_environment_drift_reports");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EnvironmentDriftReportId.From(value));

        builder.Property(x => x.SourceEnvironment).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetEnvironment).HasMaxLength(100).IsRequired();

        builder.Property(x => x.AnalyzedDimensions).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ServiceVersionDrifts).HasColumnType("jsonb");
        builder.Property(x => x.ConfigurationDrifts).HasColumnType("jsonb");
        builder.Property(x => x.ContractVersionDrifts).HasColumnType("jsonb");
        builder.Property(x => x.DependencyDrifts).HasColumnType("jsonb");
        builder.Property(x => x.PolicyDrifts).HasColumnType("jsonb");
        builder.Property(x => x.Recommendations).HasColumnType("jsonb");

        builder.Property(x => x.TotalDriftItems).IsRequired();
        builder.Property(x => x.CriticalDriftItems).IsRequired();

        builder.Property(x => x.OverallSeverity)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReviewComment).HasMaxLength(2000);
        builder.Property(x => x.TenantId);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_ops_environment_drift_reports_tenant_id");
        builder.HasIndex(x => new { x.SourceEnvironment, x.TargetEnvironment, x.GeneratedAt });
        builder.HasIndex(x => x.Status);
    }
}
