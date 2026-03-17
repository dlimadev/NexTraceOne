using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para SecurityEvent.
/// Persiste eventos de segurança e anomalias detectadas pelo motor de Session Intelligence.
/// Metadados adicionais são armazenados como jsonb para flexibilidade de contexto expandido.
/// Índices otimizados para consulta por tenant, tipo de evento e score de risco.
/// </summary>
internal sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("identity_security_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SecurityEventId.From(value));

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(x => x.SessionId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? SessionId.From(value.Value) : null);

        builder.Property(x => x.EventType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.RiskScore).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.IsReviewed).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ReviewedAt);

        builder.Property(x => x.ReviewedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.HasIndex(x => new { x.TenantId, x.EventType });
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
        builder.HasIndex(x => x.RiskScore);
        builder.HasIndex(x => x.UserId);
    }
}
