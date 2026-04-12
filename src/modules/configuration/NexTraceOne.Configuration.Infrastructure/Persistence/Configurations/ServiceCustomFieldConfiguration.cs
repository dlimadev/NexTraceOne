using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class ServiceCustomFieldConfiguration : IEntityTypeConfiguration<ServiceCustomField>
{
    public void Configure(EntityTypeBuilder<ServiceCustomField> builder)
    {
        builder.ToTable("cfg_service_custom_fields");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ServiceCustomFieldId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.FieldType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.DefaultValue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasIndex(x => x.TenantId);
    }
}
