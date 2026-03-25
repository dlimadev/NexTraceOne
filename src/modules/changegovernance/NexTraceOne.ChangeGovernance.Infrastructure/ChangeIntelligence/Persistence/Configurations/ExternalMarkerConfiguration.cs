using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade ExternalMarker.</summary>
internal sealed class ExternalMarkerConfiguration : IEntityTypeConfiguration<ExternalMarker>
{
    /// <summary>Configura o mapeamento da entidade ExternalMarker para a tabela ci_external_markers.</summary>
    public void Configure(EntityTypeBuilder<ExternalMarker> builder)
    {
        builder.ToTable("chg_external_markers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalMarkerId.From(value));

        builder.Property(x => x.ReleaseId)
            .HasConversion(id => id.Value, value => ReleaseId.From(value))
            .IsRequired();
        builder.Property(x => x.MarkerType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceSystem).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReceivedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.ReleaseId);
    }
}
