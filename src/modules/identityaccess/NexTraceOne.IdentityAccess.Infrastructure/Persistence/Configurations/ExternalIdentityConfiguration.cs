using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para ExternalIdentity.
/// Mapeia identidades federadas externas (OIDC, SAML, SCIM) vinculadas a usuários internos.
/// Índice único por (Provider, ExternalUserId) garante unicidade da identidade por provedor.
/// </summary>
internal sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.ToTable("identity_external_identities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ExternalIdentityId.From(value));

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ExternalUserId)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ExternalEmail)
            .HasMaxLength(320);

        builder.Property(x => x.ExternalGroupsJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.LastSyncAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.Provider, x.ExternalUserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
