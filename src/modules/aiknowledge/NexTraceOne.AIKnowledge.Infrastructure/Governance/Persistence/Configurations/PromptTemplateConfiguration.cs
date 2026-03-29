using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade PromptTemplate.
/// Tabela: aik_prompt_templates.
/// </summary>
internal sealed class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
{
    public void Configure(EntityTypeBuilder<PromptTemplate> builder)
    {
        builder.ToTable("aik_prompt_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PromptTemplateId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(10000).IsRequired();
        builder.Property(x => x.Variables).HasMaxLength(1000);
        builder.Property(x => x.TargetPersonas).HasMaxLength(500);
        builder.Property(x => x.ScopeHint).HasMaxLength(100);
        builder.Property(x => x.Relevance).HasMaxLength(50);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.IsOfficial);
        builder.HasIndex(x => new { x.Name, x.Version }).IsUnique();
    }
}
