using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Infrastructure.Persistence.Configurations;

internal sealed class AIIDEClientRegistrationConfiguration : IEntityTypeConfiguration<AIIDEClientRegistration>
{
    public void Configure(EntityTypeBuilder<AIIDEClientRegistration> builder)
    {
        builder.ToTable("ai_gov_ide_client_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIIDEClientRegistrationId.From(value));

        builder.Property(x => x.UserId).HasMaxLength(500).IsRequired();
        builder.Property(x => x.UserDisplayName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ClientType).HasMaxLength(100).HasConversion<string>().IsRequired();
        builder.Property(x => x.ClientVersion).HasMaxLength(100);
        builder.Property(x => x.DeviceIdentifier).HasMaxLength(500);
        builder.Property(x => x.LastAccessAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RevocationReason).HasMaxLength(1000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IsActive);
    }
}
