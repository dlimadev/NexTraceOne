using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade StorageBucket.
/// Tabela: int_storage_buckets
/// </summary>
internal sealed class StorageBucketConfiguration : IEntityTypeConfiguration<StorageBucket>
{
    public void Configure(EntityTypeBuilder<StorageBucket> builder)
    {
        builder.ToTable("int_storage_buckets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new StorageBucketId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BucketName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.BackendType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RetentionDays)
            .IsRequired();

        builder.Property(x => x.FilterJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.IsFallback)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsEnabled, x.Priority });
        builder.HasIndex(x => new { x.TenantId, x.BucketName }).IsUnique();
    }
}
