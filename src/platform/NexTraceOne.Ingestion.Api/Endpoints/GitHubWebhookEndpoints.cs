using System.Text.Json;

using MediatR;

using NexTraceOne.Ingestion.Api.Security;

using IngestExternalReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestExternalRelease.IngestExternalRelease;

namespace NexTraceOne.Ingestion.Api.Endpoints;

/// <summary>
/// Webhook inbound do GitHub — traduz eventos de deployment em releases
/// do Change Intelligence sem nenhuma configuração de pipeline.
///
/// Configuração no GitHub: webhook apontando para
/// <c>POST /api/v1/webhooks/github?api_key=...</c> com content type
/// application/json e secret igual a <c>Security:WebhookSecrets:GitHub</c>.
///
/// Eventos tratados:
/// - <c>ping</c> → 200 (verificação de configuração)
/// - <c>deployment_status</c> com state=success → cria release via
///   <see cref="IngestExternalReleaseFeature"/>
/// - demais eventos → 202 ignorado (GitHub não re-tenta)
/// </summary>
internal static class GitHubWebhookEndpoints
{
    private const string EventHeader = "X-GitHub-Event";

    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/webhooks/github", async (
            HttpContext httpContext,
            JsonElement payload,
            ISender sender,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(GitHubWebhookEndpoints));
            var eventName = httpContext.Request.Headers[EventHeader].ToString();

            if (string.Equals(eventName, "ping", StringComparison.OrdinalIgnoreCase))
                return Results.Ok(new { status = "pong" });

            if (!string.Equals(eventName, "deployment_status", StringComparison.OrdinalIgnoreCase))
                return Results.Accepted(value: new { status = "ignored", @event = eventName });

            var state = GetString(payload, "deployment_status", "state");
            if (!string.Equals(state, "success", StringComparison.OrdinalIgnoreCase))
                return Results.Accepted(value: new { status = "ignored", state });

            var repository = GetString(payload, "repository", "name");
            var deploymentId = GetRaw(payload, "deployment", "id");
            var sha = GetString(payload, "deployment", "sha");
            var environment = GetString(payload, "deployment", "environment");
            var description = GetString(payload, "deployment", "description");
            var reference = GetString(payload, "deployment", "ref");

            if (string.IsNullOrWhiteSpace(repository)
                || string.IsNullOrWhiteSpace(deploymentId)
                || string.IsNullOrWhiteSpace(environment))
            {
                return Results.BadRequest(new { error = "Payload is missing repository.name, deployment.id or deployment.environment." });
            }

            var version = !string.IsNullOrWhiteSpace(reference)
                ? reference
                : (sha is { Length: >= 7 } ? sha[..7] : sha ?? deploymentId);

            var command = new IngestExternalReleaseFeature.Command(
                ExternalReleaseId: deploymentId,
                ExternalSystem: "github",
                ServiceName: repository,
                Version: version,
                TargetEnvironment: environment,
                Description: description,
                CommitShas: string.IsNullOrWhiteSpace(sha) ? null : new[] { sha });

            var result = await sender.Send(command, ct);

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "GitHub deployment webhook rejected for {Repository}@{Version}/{Environment}: {Error}",
                    repository, version, environment, result.Error?.Message);
                return Results.UnprocessableEntity(new { error = result.Error?.Message });
            }

            logger.LogInformation(
                "GitHub deployment ingested as release {ReleaseId} ({Repository}@{Version} → {Environment})",
                result.Value.ReleaseId, repository, version, environment);

            return Results.Accepted(value: new
            {
                status = result.Value.IsNew ? "release_created" : "release_already_exists",
                releaseId = result.Value.ReleaseId,
            });
        })
        .RequireAuthorization(IngestionApiSecurity.PolicyName)
        .WithWebhookSignature("GitHub")
        .WithName("GitHubDeploymentWebhook")
        .WithSummary("Inbound GitHub webhook: deployment_status events become Change Intelligence releases")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    private static string? GetString(JsonElement root, string objectName, string property)
    {
        if (root.TryGetProperty(objectName, out var obj)
            && obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(property, out var prop)
            && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static string? GetRaw(JsonElement root, string objectName, string property)
    {
        if (root.TryGetProperty(objectName, out var obj)
            && obj.ValueKind == JsonValueKind.Object
            && obj.TryGetProperty(property, out var prop))
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
        return null;
    }
}
