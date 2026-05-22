using MediatR;

using IngestCodeQualityRecordFeature = NexTraceOne.Catalog.Application.Contracts.Features.IngestCodeQualityRecord.IngestCodeQualityRecord;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de métricas de qualidade de código.
///
/// Suporta dois modos:
/// - POST /api/v1/quality/sonarqube/analysis — webhook no formato SonarQube
/// - POST /api/v1/quality/record             — formato genérico normalizado
///
/// Ambos persistem um <c>CodeQualityRecord</c> via <c>IngestCodeQualityRecord</c>.
/// O unitOfWork é gerido pelo <c>TransactionBehavior</c> do pipeline MediatR.
/// </summary>
internal static class SonarQubeIngestEndpoints
{
    internal static void Map(RouteGroupBuilder group)
    {
        MapSonarQubeWebhook(group);
        MapGenericRecord(group);
    }

    private static void MapSonarQubeWebhook(RouteGroupBuilder group)
    {
        group.MapPost("/sonarqube/analysis", async (
            SonarQubeWebhookPayload payload,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(SonarQubeIngestEndpoints));

            var qualityGate = payload.QualityGate;
            var measures = payload.Project?.Measures ?? [];

            double GetMeasure(string key)
                => measures.TryGetValue(key, out var v) && double.TryParse(v, out var d) ? d : 0.0;
            int GetMeasureInt(string key) => (int)GetMeasure(key);

            var cmd = new IngestCodeQualityRecordFeature.Command(
                TenantId: payload.Project?.TenantId ?? string.Empty,
                ServiceId: payload.Project?.ServiceId ?? payload.Project?.Key ?? string.Empty,
                ServiceName: payload.Project?.Name ?? string.Empty,
                ProjectKey: payload.Project?.Key ?? string.Empty,
                QualityGateStatus: qualityGate?.Status ?? "NONE",
                Coverage: GetMeasure("coverage"),
                Bugs: GetMeasureInt("bugs"),
                Vulnerabilities: GetMeasureInt("vulnerabilities"),
                CodeSmells: GetMeasureInt("code_smells"),
                DuplicatedLinesDensity: GetMeasure("duplicated_lines_density"),
                Branch: payload.Branch?.Name);

            var result = await sender.Send(cmd, ct);

            if (result.IsSuccess)
                return Results.Accepted(null, new { id = result.Value, status = "accepted" });

            logger.LogWarning("IngestCodeQualityRecord failed: {Error}", result.Error?.Message);
            return Results.UnprocessableEntity(new { error = result.Error?.Message });
        })
        .WithName("PostSonarQubeAnalysis")
        .WithSummary("Ingest SonarQube analysis result via webhook")
        .WithDescription("Receives SonarQube webhook payload and persists the code quality record")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static void MapGenericRecord(RouteGroupBuilder group)
    {
        group.MapPost("/record", async (
            IngestCodeQualityRecordFeature.Command cmd,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(SonarQubeIngestEndpoints));
            var result = await sender.Send(cmd, ct);

            if (result.IsSuccess)
                return Results.Accepted(null, new { id = result.Value, status = "accepted" });

            logger.LogWarning("IngestCodeQualityRecord (generic) failed: {Error}", result.Error?.Message);
            return Results.UnprocessableEntity(new { error = result.Error?.Message });
        })
        .WithName("PostCodeQualityRecord")
        .WithSummary("Ingest a normalised code quality record")
        .WithDescription("Receives a code quality analysis result in the normalised NexTraceOne format")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}

// ── Payload models ─────────────────────────────────────────────────────────

/// <summary>
/// Payload de webhook do SonarQube (formato oficial).
/// TenantId e ServiceId são extensões NexTraceOne enviadas como propriedades customizadas.
/// </summary>
internal sealed record SonarQubeWebhookPayload(
    SonarQubeProjectInfo? Project,
    SonarQubeQualityGate? QualityGate,
    SonarQubeBranchInfo? Branch);

internal sealed record SonarQubeProjectInfo(
    string? Key,
    string? Name,
    string? TenantId,
    string? ServiceId,
    Dictionary<string, string>? Measures);

internal sealed record SonarQubeQualityGate(string? Status, string? Name);

internal sealed record SonarQubeBranchInfo(string? Name, string? Type);
