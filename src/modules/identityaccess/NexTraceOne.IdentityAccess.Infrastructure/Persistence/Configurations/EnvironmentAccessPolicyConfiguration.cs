using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>Configuração EF Core da entidade EnvironmentAccessPolicy.</summary>
internal sealed class EnvironmentAccessPolicyConfiguration : IEntityTypeConfiguration<EnvironmentAccessPolicy>
{
    public void Configure(EntityTypeBuilder<EnvironmentAccessPolicy> builder)
    {
        builder.ToTable("iam_environment_access_policies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => EnvironmentAccessPolicyId.From(value));

        builder.Property(x => x.PolicyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.JitApprovalRequiredFrom)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Armazenar listas como JSON no PostgreSQL
        builder.Property(x => x.Environments)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);

        builder.Property(x => x.AllowedRoles)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);

        builder.Property(x => x.RequireJitForRoles)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);

        // Índice único por tenant + nome da política
        builder.HasIndex(x => new { x.TenantId, x.PolicyName })
            .IsUnique()
            .HasDatabaseName("IX_iam_environment_access_policies_tenant_name");

        // Índice para pesquisa por tenant
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_iam_environment_access_policies_tenant");
    }
}
