using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ContractVersionConfiguration : IEntityTypeConfiguration<ContractVersion>
{
    public void Configure(EntityTypeBuilder<ContractVersion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractVersionId(value));

        builder.Property(x => x.SemVer).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ImportedFrom).HasMaxLength(1000);
        builder.Property(x => x.LockedBy).HasMaxLength(200);
        builder.Property(x => x.DeprecationNotice).HasMaxLength(2000);

        // Sem este mapeamento explícito os value objects eram ignorados pela convenção
        // do NexTraceDbContextBase e nunca persistidos (SLA, assinatura e proveniência
        // eram perdidos silenciosamente em cada SaveChanges).
        builder.OwnsOne(x => x.Sla, sla => sla.ToJson("sla_json"));
        builder.OwnsOne(x => x.Signature, sig => sig.ToJson("signature_json"));
        builder.OwnsOne(x => x.Provenance, prov => prov.ToJson("provenance_json"));

        // Invariante de domínio: uma única versão semântica por ativo de API.
        // Filtrado por IsDeleted para permitir re-importação após soft-delete.
        builder.HasIndex(x => new { x.ApiAssetId, x.SemVer })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.LifecycleState);

        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
