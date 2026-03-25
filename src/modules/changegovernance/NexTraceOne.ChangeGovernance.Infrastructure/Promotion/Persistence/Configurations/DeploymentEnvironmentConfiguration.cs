using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence.Configurations;

internal sealed class DeploymentEnvironmentConfiguration : IEntityTypeConfiguration<DeploymentEnvironment>
{
    /// <summary>Configura o mapeamento da entidade DeploymentEnvironment para a tabela prm_deployment_environments.</summary>
    public void Configure(EntityTypeBuilder<DeploymentEnvironment> builder)
    {
        builder.ToTable("chg_deployment_environments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DeploymentEnvironmentId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.RequiresApproval).IsRequired();
        builder.Property(x => x.RequiresEvidencePack).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Order);
    }
}
