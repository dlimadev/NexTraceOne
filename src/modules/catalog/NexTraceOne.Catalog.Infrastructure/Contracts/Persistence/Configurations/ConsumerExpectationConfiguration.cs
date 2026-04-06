using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Configurations;

/// <summary>
/// Configura o mapeamento EF Core da entidade ConsumerExpectation.
/// Suporta Consumer-Driven Contract Testing (CDCT) ao persistir expectativas de consumidores.
/// </summary>
internal sealed class ConsumerExpectationConfiguration : IEntityTypeConfiguration<ConsumerExpectation>
{
    public void Configure(EntityTypeBuilder<ConsumerExpectation> builder)
    {
        builder.ToTable("ctr_consumer_expectations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ConsumerExpectationId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ConsumerServiceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConsumerDomain).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExpectedSubsetJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.RegisteredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.ApiAssetId);
        builder.HasIndex(x => new { x.ApiAssetId, x.ConsumerServiceName }).IsUnique();
        builder.HasIndex(x => x.IsActive).HasFilter("\"IsActive\" = true");
    }
}
