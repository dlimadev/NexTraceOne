using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Compliance.Entities;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade TenantBenchmarkConsent.
/// Tabela: chg_benchmark_consents
/// </summary>
internal sealed class TenantBenchmarkConsentConfiguration : IEntityTypeConfiguration<TenantBenchmarkConsent>
{
    /// <summary>Configura o mapeamento da entidade TenantBenchmarkConsent.</summary>
    public void Configure(EntityTypeBuilder<TenantBenchmarkConsent> builder)
    {
        builder.ToTable("chg_benchmark_consents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => TenantBenchmarkConsentId.From(value));

        builder.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ConsentedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ConsentedByUserId).HasMaxLength(200);
        builder.Property(x => x.LgpdLawfulBasis).HasMaxLength(500).IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_chg_benchmark_consents_tenant_id");
    }
}
