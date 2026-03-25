using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AuditCompliance.Domain.Entities;

namespace NexTraceOne.AuditCompliance.Infrastructure.Persistence.Configurations;

internal sealed class AuditCampaignConfiguration : IEntityTypeConfiguration<AuditCampaign>
{
    /// <summary>Configura o mapeamento da entidade AuditCampaign para a tabela aud_campaigns.</summary>
    public void Configure(EntityTypeBuilder<AuditCampaign> builder)
    {
        builder.ToTable("aud_campaigns", t =>
        {
            t.HasCheckConstraint("CK_aud_campaigns_status",
                "\"Status\" IN ('Planned','InProgress','Completed','Cancelled')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AuditCampaignId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.CampaignType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScheduledStartAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CampaignType);

        // ── Concorrência otimista (PostgreSQL xmin) ──────────────────────────
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
