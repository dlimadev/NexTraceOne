using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade Team.
/// Define mapeamento de tabela, typed ID, enums, concorrência otimista e índices.
/// Prefixo gov_ — alinhado com a futura baseline do módulo Governance.
/// </summary>
internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("gov_teams", t =>
        {
            t.HasCheckConstraint(
                "CK_gov_teams_status",
                "\"Status\" IN ('Active', 'Inactive', 'Archived')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TeamId(value));

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ParentOrganizationUnit)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índices para consultas frequentes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ParentOrganizationUnit);
    }
}
