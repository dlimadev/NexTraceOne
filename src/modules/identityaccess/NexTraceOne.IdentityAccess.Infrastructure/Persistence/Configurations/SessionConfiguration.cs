using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("identity_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => SessionId.From(value));
        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();
        builder.Property(x => x.RefreshToken)
            .HasConversion(hash => hash.Value, value => RefreshTokenHash.FromHash(value))
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedByIp).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.HasIndex(x => x.RefreshToken).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
