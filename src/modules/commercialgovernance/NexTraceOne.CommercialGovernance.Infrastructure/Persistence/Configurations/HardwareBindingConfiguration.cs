using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Configurations;

internal sealed class HardwareBindingConfiguration : IEntityTypeConfiguration<HardwareBinding>
{
    public void Configure(EntityTypeBuilder<HardwareBinding> builder)
    {
        builder.ToTable("licensing_hardware_bindings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HardwareBindingId.From(value));
        builder.Property(x => x.Fingerprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BoundAt).IsRequired();
        builder.Property(x => x.LastValidatedAt).IsRequired();
    }
}
