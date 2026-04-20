using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configura o mapeamento da entidade ExternalChangeRequest para a tabela cg_external_change_requests.</summary>
internal sealed class ExternalChangeRequestConfiguration : IEntityTypeConfiguration<ExternalChangeRequest>
{
    public void Configure(EntityTypeBuilder<ExternalChangeRequest> builder)
    {
        builder.ToTable("cg_external_change_requests");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalChangeRequestId.From(value));

        builder.Property(x => x.ExternalSystem)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.ChangeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RequestedBy)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ScheduledStart)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ScheduledEnd)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ServiceId);

        builder.Property(x => x.Environment)
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValue(ExternalChangeRequestStatus.Pending);

        builder.Property(x => x.IngestedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LinkedReleaseId);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        // Índice único para garantir idempotência por chave natural
        builder.HasIndex(x => new { x.ExternalSystem, x.ExternalId })
            .IsUnique()
            .HasDatabaseName("ix_cg_external_change_requests_external_key");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_cg_external_change_requests_status");

        builder.HasIndex(x => x.ServiceId)
            .HasDatabaseName("ix_cg_external_change_requests_service_id")
            .HasFilter("\"ServiceId\" IS NOT NULL");
    }
}
