using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractNegotiation.
/// Negociações cross-team rastreiam a colaboração e aprovação de alterações contratuais.
/// </summary>
internal sealed class ContractNegotiationConfiguration : IEntityTypeConfiguration<ContractNegotiation>
{
    public void Configure(EntityTypeBuilder<ContractNegotiation> builder)
    {
        builder.ToTable("cat_contract_negotiations");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractNegotiationId.From(value));

        builder.Property(x => x.ContractId);

        builder.Property(x => x.ProposedByTeamId).IsRequired();
        builder.Property(x => x.ProposedByTeamName).IsRequired().HasMaxLength(300);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(4000);

        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.Deadline)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Participants)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.ParticipantCount).IsRequired();
        builder.Property(x => x.CommentCount).IsRequired();

        builder.Property(x => x.ProposedContractSpec)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.LastActivityAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ResolvedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ResolvedByUserId).HasMaxLength(200);
        builder.Property(x => x.InitiatedByUserId).IsRequired().HasMaxLength(200);

        // Concorrência otimista via PostgreSQL xmin
        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.ContractId);
        builder.HasIndex(x => x.ProposedByTeamId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
