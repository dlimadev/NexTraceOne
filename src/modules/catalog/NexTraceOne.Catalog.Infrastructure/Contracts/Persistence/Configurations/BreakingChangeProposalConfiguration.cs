using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para BreakingChangeProposal.
/// Tabela: ctr_breaking_change_proposals
/// </summary>
internal sealed class BreakingChangeProposalConfiguration : IEntityTypeConfiguration<BreakingChangeProposal>
{
    public void Configure(EntityTypeBuilder<BreakingChangeProposal> builder)
    {
        builder.ToTable("ctr_breaking_change_proposals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BreakingChangeProposalId(value));

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ContractId).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ProposedBreakingChangesJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.MigrationWindowDays).IsRequired();

        builder.Property(x => x.ProposedBy)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ConsultationOpenedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DecidedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DecisionNotes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ContractId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}
