using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("identity_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UserId.From(value));

        builder.Property(x => x.Email)
            .HasConversion(email => email.Value, value => Email.FromDatabase(value))
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.OwnsOne(x => x.FullName, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            ownedBuilder.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(x => x.PasswordHash)
            .HasConversion(
                hash => hash == null ? null : hash.Value,
                value => string.IsNullOrWhiteSpace(value) ? null : HashedPassword.FromHash(value))
            .HasMaxLength(200);

        builder.Property(x => x.FederationProvider).HasMaxLength(100);
        builder.Property(x => x.ExternalId).HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.FailedLoginAttempts).IsRequired();
        builder.Property(x => x.LastLoginAt);
        builder.Property(x => x.LockoutEnd);
    }
}
