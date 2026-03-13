using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexTraceOne.DeveloperPortal.Domain.Entities;

namespace NexTraceOne.DeveloperPortal.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração EF Core para a entidade PlaygroundSession.
/// Mapeia sessões de execução sandbox com trilha de auditoria completa.
/// </summary>
internal sealed class PlaygroundSessionConfiguration : IEntityTypeConfiguration<PlaygroundSession>
{
    public void Configure(EntityTypeBuilder<PlaygroundSession> builder)
    {
        builder.ToTable("dp_playground_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => PlaygroundSessionId.From(value));

        builder.Property(x => x.ApiAssetId).IsRequired();
        builder.Property(x => x.ApiName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(x => x.RequestPath).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RequestBody).HasColumnType("text");
        builder.Property(x => x.RequestHeaders).HasColumnType("text");
        builder.Property(x => x.ResponseStatusCode).IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("text");
        builder.Property(x => x.DurationMs).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExecutedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ApiAssetId);
    }
}
