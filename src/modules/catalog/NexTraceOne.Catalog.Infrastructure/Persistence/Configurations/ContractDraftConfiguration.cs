using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ContractDraftConfiguration : IEntityTypeConfiguration<ContractDraft>
{
    public void Configure(EntityTypeBuilder<ContractDraft> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractDraftId(value));

        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Author).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ProposedVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LastEditedBy).HasMaxLength(200);

        // Listagens do Contract Studio filtram por serviço e por estado do draft.
        builder.HasIndex(x => new { x.ServiceId, x.Status });
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
