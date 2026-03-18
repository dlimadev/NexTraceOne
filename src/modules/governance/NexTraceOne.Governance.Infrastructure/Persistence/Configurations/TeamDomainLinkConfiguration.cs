using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade TeamDomainLink.
/// Define mapeamento de tabela, typed IDs, enums e índices.
/// </summary>
internal sealed class TeamDomainLinkConfiguration : IEntityTypeConfiguration<TeamDomainLink>
{
    public void Configure(EntityTypeBuilder<TeamDomainLink> builder)
    {
        builder.ToTable("gov_team_domain_links");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TeamDomainLinkId(value));

        builder.Property(x => x.TeamId)
            .HasConversion(id => id.Value, value => new TeamId(value))
            .IsRequired();

        builder.Property(x => x.DomainId)
            .HasConversion(id => id.Value, value => new GovernanceDomainId(value))
            .IsRequired();

        builder.Property(x => x.OwnershipType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LinkedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.TeamId);
        builder.HasIndex(x => x.DomainId);
        builder.HasIndex(x => new { x.TeamId, x.DomainId }).IsUnique();
        builder.HasIndex(x => x.OwnershipType);
    }
}
