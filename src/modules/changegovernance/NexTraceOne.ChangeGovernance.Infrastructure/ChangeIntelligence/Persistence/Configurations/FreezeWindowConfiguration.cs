using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence.Configurations;

/// <summary>Configuração EF Core para a entidade FreezeWindow.</summary>
internal sealed class FreezeWindowConfiguration : IEntityTypeConfiguration<FreezeWindow>
{
    /// <summary>Configura o mapeamento da entidade FreezeWindow para a tabela ci_freeze_windows.</summary>
    public void Configure(EntityTypeBuilder<FreezeWindow> builder)
    {
        builder.ToTable("ci_freeze_windows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => FreezeWindowId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScopeValue).HasMaxLength(500);
        builder.Property(x => x.StartsAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EndsAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => new { x.StartsAt, x.EndsAt, x.IsActive });
    }
}
