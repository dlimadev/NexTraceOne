using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade DemoSeedState.</summary>
internal sealed class DemoSeedStateConfiguration : IEntityTypeConfiguration<DemoSeedState>
{
    public void Configure(EntityTypeBuilder<DemoSeedState> builder)
    {
        builder.ToTable("gov_demo_seed_state");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new DemoSeedStateId(value));

        builder.Property(x => x.State).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SeededAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.EntitiesCount).IsRequired();
        builder.Property(x => x.TenantId);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
