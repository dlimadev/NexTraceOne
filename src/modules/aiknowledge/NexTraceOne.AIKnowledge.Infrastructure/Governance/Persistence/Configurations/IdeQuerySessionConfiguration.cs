using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade IdeQuerySession.
/// Tabela: ai_ide_query_sessions.
/// </summary>
internal sealed class IdeQuerySessionConfiguration : IEntityTypeConfiguration<IdeQuerySession>
{
    public void Configure(EntityTypeBuilder<IdeQuerySession> builder)
    {
        builder.ToTable("ai_ide_query_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => IdeQuerySessionId.From(value));

        builder.Property(x => x.UserId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IdeClient).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IdeClientVersion).HasMaxLength(50).IsRequired();

        builder.Property(x => x.QueryType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.QueryText).HasMaxLength(10000).IsRequired();
        builder.Property(x => x.QueryContext).HasColumnType("jsonb");
        builder.Property(x => x.ResponseText);
        builder.Property(x => x.ModelUsed).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TokensUsed).IsRequired();
        builder.Property(x => x.PromptTokens).IsRequired();
        builder.Property(x => x.CompletionTokens).IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.GovernanceCheckResult).HasColumnType("jsonb");
        builder.Property(x => x.ResponseTimeMs);
        builder.Property(x => x.SubmittedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RespondedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.IdeClient);
        builder.HasIndex(x => x.QueryType);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.SubmittedAt);
        builder.HasIndex(x => x.ModelUsed);
    }
}
