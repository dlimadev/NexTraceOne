using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Ingestion.Api.Models;
using NexTraceOne.Ingestion.Api.Security;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using IngestCommitFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestCommit.IngestCommit;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Endpoints de ingestão de commits de repositórios de código.
/// Permite que pipelines CI/CD (GitHub Actions, GitLab CI, Jenkins, Azure DevOps)
/// reportem commits ao NexTraceOne para alimentar o Change Advisory, correlação de
/// incidentes e rastreabilidade de mudanças.
/// </summary>
internal static class CommitIngestEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de ingestão de commits no grupo raiz de commits.
    /// </summary>
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            HttpContext httpContext,
            IngestCommitRequest request,
            IIntegrationConnectorRepository connectorRepo,
            IIngestionExecutionRepository executionRepo,
            IIngestionSourceRepository sourceRepo,
            ISender sender,
            IUnitOfWork unitOfWork,
            IDateTimeProvider clock,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(CommitIngestEndpoints));
            var correlationId = IngestionCorrelationHelper.ResolveCorrelationId(httpContext, request.CorrelationId);

            // ── Auditoria de ingestão ─────────────────────────────────────────────
            var externalSystem = request.ExternalSystem ?? request.PipelineSource ?? "external";
            var sourceName = request.PipelineSource ?? externalSystem;
            var connector = await connectorRepo.GetByNameAsync(externalSystem, ct);
            if (connector is null)
            {
                connector = IntegrationConnector.Create(
                    name: externalSystem.ToLowerInvariant().Replace(" ", "-"),
                    connectorType: "VCS",
                    description: $"Auto-registered {externalSystem} commit connector",
                    provider: externalSystem,
                    endpoint: null,
                    environment: null,
                    authenticationMode: null,
                    pollingMode: null,
                    allowedTeams: null,
                    utcNow: clock.UtcNow);
                await connectorRepo.AddAsync(connector, ct);
            }

            var source = await sourceRepo.GetByConnectorAndNameAsync(connector.Id, "commits", ct);
            if (source is null)
            {
                source = IngestionSource.Create(
                    connectorId: connector.Id,
                    name: "commits",
                    sourceType: "Webhook",
                    dataDomain: "ChangeIntelligence",
                    description: $"Commit events from {externalSystem}",
                    endpoint: null,
                    expectedIntervalMinutes: 5,
                    utcNow: clock.UtcNow);
                await sourceRepo.AddAsync(source, ct);
            }

            var execution = IngestionExecution.Start(
                connectorId: connector.Id,
                sourceId: source.Id,
                correlationId: correlationId,
                utcNow: clock.UtcNow);

            // ── Dispatch para o domínio ───────────────────────────────────────────
            Guid? commitId = null;
            bool? isNew = null;
            string processingStatus;

            try
            {
                var command = new IngestCommitFeature.Command(
                    ServiceName: request.ServiceName,
                    CommitSha: request.CommitSha,
                    BranchName: request.Branch,
                    CommitAuthor: request.Author,
                    CommitMessage: request.Message,
                    CommittedAt: request.CommittedAt,
                    RepositoryUrl: null);

                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    commitId = result.Value.CommitAssociationId;
                    isNew = result.Value.IsNew;
                    processingStatus = isNew == true ? "commit_recorded" : "commit_already_exists";
                    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
                }
                else
                {
                    processingStatus = "domain_rejected";
                    execution.CompleteFailed(result.Error?.Message ?? "Domain rejected the commit", null, clock.UtcNow);
                    logger.LogWarning(
                        "IngestCommit rejected for {ServiceName} sha={CommitSha}: {Error}",
                        request.ServiceName, request.CommitSha, result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                processingStatus = "ingest_error";
                execution.CompleteFailed(ex.Message, "unexpected_error", clock.UtcNow);
                logger.LogError(ex,
                    "Unexpected error ingesting commit {CommitSha} for {ServiceName}",
                    request.CommitSha, request.ServiceName);
            }

            source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);
            connector.RecordSuccess(clock.UtcNow);

            await executionRepo.AddAsync(execution, ct);
            await sourceRepo.UpdateAsync(source, ct);
            await connectorRepo.UpdateAsync(connector, ct);
            await unitOfWork.CommitAsync(ct);

            return Results.Accepted(null, new
            {
                message = "Commit ingestion accepted",
                status = "accepted",
                processingStatus,
                correlationId,
                executionId = execution.Id.Value,
                commitId,
                isNew,
                serviceName = request.ServiceName,
                commitSha = request.CommitSha
            });
        })
        .WithName("PostIngestCommit")
        .WithSummary("Ingest a commit from a CI/CD pipeline or VCS webhook")
        .WithDescription(
            "Records a commit from an external repository (GitHub, GitLab, Azure DevOps, Jenkins) " +
            "into NexTraceOne to enrich Change Advisory, incident correlation and change traceability.")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
