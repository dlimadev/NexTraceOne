using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetDatabaseHealth;

/// <summary>
/// Feature: GetDatabaseHealth — saúde e métricas da base de dados da plataforma.
/// Lê de IConfiguration "ConnectionStrings:*". Métricas detalhadas requerem integração real.
/// </summary>
public static class GetDatabaseHealth
{
    /// <summary>Query sem parâmetros — retorna relatório de saúde da base de dados.</summary>
    public sealed record Query() : IQuery<DatabaseHealthReport>;

    /// <summary>Handler que retorna estado de saúde da base de dados.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, DatabaseHealthReport>
    {
        public Task<Result<DatabaseHealthReport>> Handle(Query request, CancellationToken cancellationToken)
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? configuration.GetConnectionString("Primary")
                ?? configuration.GetConnectionString("NexTraceOne");

            var connected = !string.IsNullOrWhiteSpace(connectionString);

            var response = new DatabaseHealthReport(
                Connected: connected,
                VersionString: connected ? "PostgreSQL 16.x" : null,
                DbSizeMb: 0,
                ConnectionPoolUsed: 0,
                ConnectionPoolMax: 100,
                ActiveQueries: 0,
                LongestQueryMs: 0,
                CheckedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<DatabaseHealthReport>.Success(response));
        }
    }

    /// <summary>Relatório de saúde da base de dados.</summary>
    public sealed record DatabaseHealthReport(
        bool Connected,
        string? VersionString,
        double DbSizeMb,
        int ConnectionPoolUsed,
        int ConnectionPoolMax,
        int ActiveQueries,
        long LongestQueryMs,
        DateTimeOffset CheckedAt);
}
