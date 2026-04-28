using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class AgentRegistrationConfiguration : IEntityTypeConfiguration<AgentRegistration>
{
    public void Configure(EntityTypeBuilder<AgentRegistration> builder)
    {
        builder.ToTable("iam_agent_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AgentRegistrationId.From(v))
            .HasColumnType("uuid");
        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.HostUnitId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.HostName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AgentVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DeploymentMode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CpuCores).IsRequired();
        builder.Property(x => x.RamGb).HasColumnType("numeric(8,2)").IsRequired();
        builder.Property(x => x.HostUnits).HasColumnType("numeric(6,1)").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastHeartbeatAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.HostUnitId })
            .HasDatabaseName("uix_iam_agent_registrations_tenant_host")
            .IsUnique();
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_iam_agent_registrations_tenant");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_iam_agent_registrations_status");
    }
}
