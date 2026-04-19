using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

public sealed class WarRoomSessionConfiguration : IEntityTypeConfiguration<WarRoomSession>
{
    public void Configure(EntityTypeBuilder<WarRoomSession> builder)
    {
        builder.ToTable("aik_war_rooms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WarRoomSessionId.From(value));

        builder.Property(e => e.IncidentId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.IncidentTitle).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Severity).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ServiceAffected).HasMaxLength(300);
        builder.Property(e => e.CreatedByAgentId).HasMaxLength(200);
        builder.Property(e => e.ParticipantsJson).HasColumnType("text");
        builder.Property(e => e.TimelineJson).HasColumnType("text");
        builder.Property(e => e.SuggestedActionsJson).HasColumnType("text");
        builder.Property(e => e.PostMortemDraft).HasColumnType("text");
        builder.Property(e => e.SkillUsed).HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.IncidentId);
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
