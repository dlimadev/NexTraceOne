using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia a(s) FK(s) typed-id não descoberta(s) pela convenção (ver reference-typed-id-fk-mapping-gap).</summary>
internal sealed class ContractReviewConfiguration : IEntityTypeConfiguration<ContractReview>
{
    public void Configure(EntityTypeBuilder<ContractReview> builder)
    {
        builder.Property(x => x.DraftId)
            .HasConversion(id => id.Value, value => new ContractDraftId(value));
        builder.HasIndex(x => x.DraftId);
    }
}
