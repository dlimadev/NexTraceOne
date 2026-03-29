using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

/// <summary>
/// Configuração EF Core da entidade AiToolDefinition.
/// Tabela: aik_tool_definitions.
/// </summary>
internal sealed class AiToolDefinitionConfiguration : IEntityTypeConfiguration<AiToolDefinition>
{
    public void Configure(EntityTypeBuilder<AiToolDefinition> builder)
    {
        builder.ToTable("aik_tool_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AiToolDefinitionId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ParametersSchema).HasMaxLength(5000).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsActive);
    }
}
