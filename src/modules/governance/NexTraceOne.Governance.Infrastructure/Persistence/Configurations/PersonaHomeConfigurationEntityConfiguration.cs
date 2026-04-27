using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para PersonaHomeConfiguration (V3.10 — Persona-first Experience).</summary>
internal sealed class PersonaHomeConfigurationEntityConfiguration : IEntityTypeConfiguration<PersonaHomeConfiguration>
{
    public void Configure(EntityTypeBuilder<PersonaHomeConfiguration> builder)
    {
        builder.ToTable("gov_persona_home_configurations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PersonaHomeConfigurationId(value));

        builder.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Persona).HasMaxLength(50).IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CardLayoutJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.QuickActionsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.DefaultScopeJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Persona, x.TenantId })
            .IsUnique()
            .HasDatabaseName("ix_gov_persona_home_user_persona_tenant");
    }
}
