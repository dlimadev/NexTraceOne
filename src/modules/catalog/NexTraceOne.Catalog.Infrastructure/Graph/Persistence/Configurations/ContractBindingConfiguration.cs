using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ContractBinding.
/// Vínculos representam a associação activa entre uma interface de serviço
/// e uma versão de contrato, com trilha de activação e desactivação.
/// </summary>
internal sealed class ContractBindingConfiguration : IEntityTypeConfiguration<ContractBinding>
{
    public void Configure(EntityTypeBuilder<ContractBinding> builder)
    {
        builder.ToTable("cat_contract_bindings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ContractBindingId.From(value));

        // ── Referências ───────────────────────────────────────────────
        builder.Property(x => x.ServiceInterfaceId)
            .HasConversion(id => id.Value, value => ServiceInterfaceId.From(value))
            .IsRequired();
        builder.Property(x => x.ContractVersionId).IsRequired();

        // ── Estado ────────────────────────────────────────────────────
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).HasDefaultValue(ContractBindingStatus.Active);
        builder.Property(x => x.BindingEnvironment).HasMaxLength(200).HasDefaultValue(string.Empty);
        builder.Property(x => x.IsDefaultVersion).HasDefaultValue(false);

        // ── Activação / Desactivação ──────────────────────────────────
        builder.Property(x => x.ActivatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ActivatedBy).HasMaxLength(200);
        builder.Property(x => x.DeactivatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeactivatedBy).HasMaxLength(200);

        // ── Notas ─────────────────────────────────────────────────────
        builder.Property(x => x.MigrationNotes).HasMaxLength(2000).HasDefaultValue(string.Empty);

        // ── Auditoria ─────────────────────────────────────────────────
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // ── FK para ServiceInterface ──────────────────────────────────
        builder.HasOne<ServiceInterface>()
            .WithMany()
            .HasForeignKey(x => x.ServiceInterfaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // ── Índices ───────────────────────────────────────────────────
        builder.HasIndex(x => x.ServiceInterfaceId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.ServiceInterfaceId, x.Status });
        builder.HasIndex(x => x.ContractVersionId);
        builder.HasIndex(x => x.IsDeleted).HasFilter("\"IsDeleted\" = false");

        // ── Concorrência otimista ─────────────────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
