using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class ExternalDataSourceConfiguration : IEntityTypeConfiguration<ExternalDataSource>
{
    public void Configure(EntityTypeBuilder<ExternalDataSource> builder)
    {
        builder.ToTable("aik_external_data_sources");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalDataSourceId.From(value));

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.ConnectorType)
            .HasMaxLength(100)
            .HasConversion<string>()
            .IsRequired();

        // ConnectorConfigJson may contain credentials — encryption at the app level is recommended.
        builder.Property(x => x.ConnectorConfigJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.SyncIntervalMinutes)
            .IsRequired();

        builder.Property(x => x.LastSyncedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.LastSyncStatus)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.LastSyncError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.RegisteredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.ConnectorType);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
