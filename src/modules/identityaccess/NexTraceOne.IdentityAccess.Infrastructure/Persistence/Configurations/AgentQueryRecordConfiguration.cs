using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Persistence.Configurations;

internal sealed class AgentQueryRecordConfiguration : IEntityTypeConfiguration<AgentQueryRecord>
{
    public void Configure(EntityTypeBuilder<AgentQueryRecord> builder)
    {
        builder.ToTable("iam_agent_query_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => AgentQueryRecordId.From(v))
            .HasColumnType("uuid");
        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.TokenId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.QueryType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.QueryParametersJson).HasColumnType("text");
        builder.Property(x => x.ResponseCode).IsRequired();
        builder.Property(x => x.DurationMs).IsRequired();
        builder.Property(x => x.ExecutedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_iam_agent_query_tenant");
        builder.HasIndex(x => x.TokenId).HasDatabaseName("ix_iam_agent_query_token");
        builder.HasIndex(x => x.ExecutedAt).HasDatabaseName("ix_iam_agent_query_executed");
    }
}
