using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Infrastructure.Persistence.Configurations;

internal sealed class ContractTemplateConfiguration : IEntityTypeConfiguration<ContractTemplate>
{
    public void Configure(EntityTypeBuilder<ContractTemplate> builder)
    {
        builder.ToTable("cfg_contract_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ContractTemplateId(value));
        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ContractType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TemplateJson).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TemplateCreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsBuiltIn).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.ContractType });
    }
}
