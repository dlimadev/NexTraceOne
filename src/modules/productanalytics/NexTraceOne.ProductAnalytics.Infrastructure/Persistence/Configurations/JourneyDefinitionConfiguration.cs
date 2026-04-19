using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.ProductAnalytics.Domain.Entities;

namespace NexTraceOne.ProductAnalytics.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para JourneyDefinition.
/// Definições de jornadas configuráveis — globais (tenant null) e por tenant.
/// </summary>
internal sealed class JourneyDefinitionConfiguration : IEntityTypeConfiguration<JourneyDefinition>
{
    public void Configure(EntityTypeBuilder<JourneyDefinition> builder)
    {
        builder.ToTable("pan_journey_definitions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new JourneyDefinitionId(value));

        builder.Property(x => x.TenantId)
            .HasColumnType("uuid");

        builder.Property(x => x.Key)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.StepsJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        // Unique constraint: one definition per key+scope (tenant or global)
        builder.HasIndex(x => new { x.TenantId, x.Key })
            .IsUnique()
            .HasDatabaseName("UX_pan_journey_definitions_TenantId_Key");

        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasDatabaseName("IX_pan_journey_definitions_TenantId_IsActive");
    }
}
