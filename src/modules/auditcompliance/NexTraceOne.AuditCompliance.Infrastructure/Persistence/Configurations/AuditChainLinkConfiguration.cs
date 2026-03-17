using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Configurations;

internal sealed class AuditChainLinkConfiguration : IEntityTypeConfiguration<AuditChainLink>
{
    /// <summary>Configura o mapeamento da entidade AuditChainLink para a tabela aud_audit_chain_links.</summary>
    public void Configure(EntityTypeBuilder<AuditChainLink> builder)
    {
        builder.ToTable("aud_audit_chain_links");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AuditChainLinkId.From(value));

        builder.Property(x => x.SequenceNumber).IsRequired();
        builder.Property(x => x.CurrentHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PreviousHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.SequenceNumber).IsUnique();
        builder.HasIndex(x => x.CurrentHash).IsUnique();
    }
}
