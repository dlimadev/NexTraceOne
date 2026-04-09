using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiFeedback.
/// Tabela: aik_gov_feedbacks.
/// </summary>
internal sealed class AiFeedbackConfiguration : IEntityTypeConfiguration<AiFeedback>
{
    public void Configure(EntityTypeBuilder<AiFeedback> builder)
    {
        builder.ToTable("aik_gov_feedbacks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiFeedbackId.From(value));

        builder.Property(x => x.Rating)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Comment).HasMaxLength(5000);
        builder.Property(x => x.AgentName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ModelUsed).HasMaxLength(300).IsRequired();
        builder.Property(x => x.QueryCategory).HasMaxLength(200);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SubmittedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.AgentExecutionId);
        builder.HasIndex(x => x.Rating);
        builder.HasIndex(x => x.AgentName);
        builder.HasIndex(x => x.CreatedByUserId);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.SubmittedAt);
    }
}
