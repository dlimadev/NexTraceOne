using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para Delegation.
/// Persiste delegações formais de permissões entre usuários com vigência temporal.
/// A lista de permissões delegadas é serializada como JSON (jsonb no PostgreSQL).
/// </summary>
internal sealed class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("iam_delegations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DelegationId.From(value));

        builder.Property(x => x.GrantorId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.DelegateeId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(x => x.DelegatedPermissions)
            .HasConversion(
                perms => JsonSerializer.Serialize(perms, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptions.Default) ?? new List<string>())
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ValidUntil).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.RevokedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => x.DelegateeId);
        builder.HasIndex(x => x.GrantorId);
    }
}
