using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Configurations;

internal sealed class AIBudgetConfiguration : IEntityTypeConfiguration<AIBudget>
{
    public void Configure(EntityTypeBuilder<AIBudget> builder)
    {
        builder.ToTable("ai_gov_budgets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => AIBudgetId.From(value));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScopeValue).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Period).HasMaxLength(50).HasConversion<string>().IsRequired();
        builder.Property(x => x.PeriodStartDate).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.Scope);
        builder.HasIndex(x => x.IsActive);
    }
}
