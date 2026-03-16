using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ListIngestionExecutions;

/// <summary>
/// Feature: ListIngestionExecutions — lista execuções de ingestão com filtros temporais e de resultado.
/// Permite rastrear histórico de execuções por conector, fonte, resultado e intervalo de tempo.
/// </summary>
public static class ListIngestionExecutions
{
    /// <summary>Query para listar execuções de ingestão com filtros e paginação.</summary>
    public sealed record Query(
        Guid? ConnectorId = null,
        Guid? SourceId = null,
        string? Result = null,
        DateTimeOffset? From = null,
        DateTimeOffset? To = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que retorna a lista paginada de execuções de ingestão.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var githubId = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001");
            var jiraId = Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003");
            var pagerdutyId = Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004");
            var datadogId = Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005");
            var kongId = Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006");
            var kafkaId = Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008");
            var azureDevOpsId = Guid.Parse("a1b2c3d4-0009-4000-8000-000000000009");

            var githubSrc = Guid.Parse("b1b2b3b4-0001-4000-8000-000000000001");
            var jiraSrc = Guid.Parse("b1b2b3b4-0002-4000-8000-000000000002");
            var pdSrc = Guid.Parse("b1b2b3b4-0003-4000-8000-000000000003");
            var ddTelSrc = Guid.Parse("b1b2b3b4-0004-4000-8000-000000000004");
            var kongSrc = Guid.Parse("b1b2b3b4-0005-4000-8000-000000000005");
            var kafkaSrc = Guid.Parse("b1b2b3b4-0006-4000-8000-000000000006");
            var azdoSrc = Guid.Parse("b1b2b3b4-0007-4000-8000-000000000007");
            var ddAlertSrc = Guid.Parse("b1b2b3b4-0009-4000-8000-000000000009");

            var executions = new List<IngestionExecutionItem>
            {
                new(Guid.Parse("c1c2c3c4-0001-4000-8000-000000000001"),
                    githubId, "GitHub CI/CD", githubSrc,
                    now.AddMinutes(-12), now.AddMinutes(-11), 48_000,
                    "Success", 142, 142, 140, 0, 0, 0,
                    "corr-gh-001"),
                new(Guid.Parse("c1c2c3c4-0002-4000-8000-000000000002"),
                    githubId, "GitHub CI/CD", githubSrc,
                    now.AddMinutes(-42), now.AddMinutes(-41), 52_000,
                    "Success", 89, 89, 89, 0, 0, 0,
                    "corr-gh-002"),
                new(Guid.Parse("c1c2c3c4-0003-4000-8000-000000000003"),
                    githubId, "GitHub CI/CD", githubSrc,
                    now.AddHours(-1).AddMinutes(-12), now.AddHours(-1).AddMinutes(-10), 120_000,
                    "PartialSuccess", 210, 207, 205, 3, 0, 0,
                    "corr-gh-003"),
                new(Guid.Parse("c1c2c3c4-0004-4000-8000-000000000004"),
                    jiraId, "Jira Work Items", jiraSrc,
                    now.AddMinutes(-5), now.AddMinutes(-4), 38_000,
                    "Success", 56, 56, 56, 0, 0, 0,
                    "corr-jira-001"),
                new(Guid.Parse("c1c2c3c4-0005-4000-8000-000000000005"),
                    jiraId, "Jira Work Items", jiraSrc,
                    now.AddMinutes(-35), now.AddMinutes(-34), 41_000,
                    "Success", 72, 72, 72, 0, 0, 0,
                    "corr-jira-002"),
                new(Guid.Parse("c1c2c3c4-0006-4000-8000-000000000006"),
                    pagerdutyId, "PagerDuty Incidents", pdSrc,
                    now.AddHours(-2), now.AddHours(-1).AddMinutes(-55), 300_000,
                    "PartialSuccess", 18, 15, 15, 2, 1, 0,
                    "corr-pd-001"),
                new(Guid.Parse("c1c2c3c4-0007-4000-8000-000000000007"),
                    pagerdutyId, "PagerDuty Incidents", pdSrc,
                    now.AddHours(-3), now.AddHours(-2).AddMinutes(-58), 120_000,
                    "Success", 24, 24, 24, 0, 0, 0,
                    "corr-pd-002"),
                new(Guid.Parse("c1c2c3c4-0008-4000-8000-000000000008"),
                    datadogId, "Datadog Telemetry", ddTelSrc,
                    now.AddMinutes(-3), now.AddMinutes(-2), 65_000,
                    "Success", 1_240, 1_240, 1_238, 0, 0, 0,
                    "corr-dd-001"),
                new(Guid.Parse("c1c2c3c4-0009-4000-8000-000000000009"),
                    datadogId, "Datadog Telemetry", ddAlertSrc,
                    now.AddMinutes(-4), now.AddMinutes(-3), 42_000,
                    "Success", 87, 87, 87, 0, 0, 0,
                    "corr-dd-002"),
                new(Guid.Parse("c1c2c3c4-000a-4000-8000-000000000010"),
                    kongId, "Kong Gateway", kongSrc,
                    now.AddMinutes(-15), now.AddMinutes(-14), 55_000,
                    "Success", 340, 340, 338, 0, 0, 0,
                    "corr-kong-001"),
                new(Guid.Parse("c1c2c3c4-000b-4000-8000-000000000011"),
                    kafkaId, "Kafka Events", kafkaSrc,
                    now.AddHours(-6), now.AddHours(-5).AddMinutes(-50), 600_000,
                    "Failed", 0, 0, 0, 0, 3, 2,
                    "corr-kafka-001"),
                new(Guid.Parse("c1c2c3c4-000c-4000-8000-000000000012"),
                    kafkaId, "Kafka Events", kafkaSrc,
                    now.AddHours(-5).AddMinutes(-45), now.AddHours(-5).AddMinutes(-40), 300_000,
                    "Failed", 120, 0, 0, 0, 2, 1,
                    "corr-kafka-002"),
                new(Guid.Parse("c1c2c3c4-000d-4000-8000-000000000013"),
                    azureDevOpsId, "Azure DevOps Deployments", azdoSrc,
                    now.AddMinutes(-10), now.AddMinutes(-9), 47_000,
                    "Success", 64, 64, 64, 0, 0, 0,
                    "corr-azdo-001"),
                new(Guid.Parse("c1c2c3c4-000e-4000-8000-000000000014"),
                    azureDevOpsId, "Azure DevOps Deployments", azdoSrc,
                    now.AddMinutes(-40), now.AddMinutes(-39), 51_000,
                    "Success", 48, 48, 48, 0, 0, 0,
                    "corr-azdo-002"),
                new(Guid.Parse("c1c2c3c4-000f-4000-8000-000000000015"),
                    datadogId, "Datadog Telemetry", ddTelSrc,
                    now.AddMinutes(-33), now.AddMinutes(-32), 68_000,
                    "Success", 1_180, 1_180, 1_178, 0, 0, 0,
                    "corr-dd-003"),
                new(Guid.Parse("c1c2c3c4-0010-4000-8000-000000000016"),
                    githubId, "GitHub CI/CD", githubSrc,
                    now.AddHours(-3).AddMinutes(-12), now.AddHours(-3).AddMinutes(-10), 130_000,
                    "Failed", 0, 0, 0, 0, 2, 1,
                    "corr-gh-004")
            };

            IEnumerable<IngestionExecutionItem> filtered = executions;

            if (request.ConnectorId.HasValue)
                filtered = filtered.Where(e => e.ConnectorId == request.ConnectorId.Value);

            if (request.SourceId.HasValue)
                filtered = filtered.Where(e => e.SourceId == request.SourceId.Value);

            if (!string.IsNullOrEmpty(request.Result))
                filtered = filtered.Where(e =>
                    e.Result.Equals(request.Result, StringComparison.OrdinalIgnoreCase));

            if (request.From.HasValue)
                filtered = filtered.Where(e => e.StartedAt >= request.From.Value);

            if (request.To.HasValue)
                filtered = filtered.Where(e => e.StartedAt <= request.To.Value);

            var list = filtered.OrderByDescending(e => e.StartedAt).ToList();
            var total = list.Count;
            var paged = list
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var response = new Response(
                TotalCount: total,
                Page: request.Page,
                PageSize: request.PageSize,
                Items: paged);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta paginada com lista de execuções de ingestão.</summary>
    public sealed record Response(
        int TotalCount,
        int Page,
        int PageSize,
        IReadOnlyList<IngestionExecutionItem> Items);

    /// <summary>DTO de uma execução de ingestão com métricas de processamento.</summary>
    public sealed record IngestionExecutionItem(
        Guid ExecutionId,
        Guid ConnectorId,
        string ConnectorName,
        Guid? SourceId,
        DateTimeOffset StartedAt,
        DateTimeOffset? FinishedAt,
        long DurationMs,
        string Result,
        long RecordsReceived,
        long RecordsProcessed,
        long RecordsNormalized,
        int Warnings,
        int Errors,
        int RetryAttempt,
        string CorrelationId);
}
