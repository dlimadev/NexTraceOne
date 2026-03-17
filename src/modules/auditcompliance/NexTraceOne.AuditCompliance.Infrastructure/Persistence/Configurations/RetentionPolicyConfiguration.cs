using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Configurations;

internal sealed class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    /// <summary>Configura o mapeamento da entidade RetentionPolicy para a tabela aud_retention_policies.</summary>
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("aud_retention_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RetentionPolicyId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RetentionDays).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.IsActive);
    }
}
