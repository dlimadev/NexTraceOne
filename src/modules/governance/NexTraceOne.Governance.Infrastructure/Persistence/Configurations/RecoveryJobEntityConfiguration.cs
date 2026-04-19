using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade RecoveryJob.</summary>
internal sealed class RecoveryJobEntityConfiguration : IEntityTypeConfiguration<RecoveryJob>
{
    public void Configure(EntityTypeBuilder<RecoveryJob> builder)
    {
        builder.ToTable("gov_recovery_jobs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new RecoveryJobId(value));

        builder.Property(x => x.RestorePointId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SchemasJson)
            .HasMaxLength(4000);

        builder.Property(x => x.DryRun)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.InitiatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.InitiatedBy)
            .HasMaxLength(256);

        builder.HasIndex(x => x.InitiatedAt);
    }
}
