using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiGuardrail.
/// Tabela: aik_guardrails.
/// Campos de enum são persistidos como strings para legibilidade e compatibilidade de dados. (E-M01)
/// </summary>
internal sealed class AiGuardrailConfiguration : IEntityTypeConfiguration<AiGuardrail>
{
    public void Configure(EntityTypeBuilder<AiGuardrail> builder)
    {
        builder.ToTable("aik_guardrails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiGuardrailId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.Category)
            .HasMaxLength(100)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.GuardType)
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Pattern).HasMaxLength(5000).IsRequired();

        builder.Property(x => x.PatternType)
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Severity)
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Action)
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.UserMessage).HasMaxLength(1000);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.GuardType);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Priority);
    }
}
