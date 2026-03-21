using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateRobotFrameworkDraft;

/// <summary>
/// Feature: GenerateRobotFrameworkDraft — gera um draft Robot Framework a partir de spec real,
/// contrato ou descrição de serviço. Usa o provider de IA via IExternalAIRoutingPort e
/// persiste como GeneratedTestArtifact quando releaseId é fornecido.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GenerateRobotFrameworkDraft
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para gerar draft Robot Framework com IA.</summary>
    public sealed record Command(
        string ServiceName,
        string? Spec,
        string? EndpointDescription,
        string? ContractSummary,
        string? OperationName,
        Guid? ReleaseId,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Spec).MaximumLength(10_000).When(x => x.Spec is not null);
            RuleFor(x => x.EndpointDescription).MaximumLength(5_000).When(x => x.EndpointDescription is not null);
            RuleFor(x => x.ContractSummary).MaximumLength(5_000).When(x => x.ContractSummary is not null);
            RuleFor(x => x).Must(x =>
                !string.IsNullOrWhiteSpace(x.Spec) ||
                !string.IsNullOrWhiteSpace(x.EndpointDescription) ||
                !string.IsNullOrWhiteSpace(x.ContractSummary))
                .WithMessage("At least one input context must be provided: Spec, EndpointDescription, or ContractSummary.");
        }
    }

    // ── HANDLER ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IExternalAIRoutingPort routingPort,
        IGeneratedTestArtifactRepository artifactRepository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        private const string RobotFramework = "robot-framework";

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var prompt = BuildPrompt(request);
            var context = $"robot-framework-generation:{request.ServiceName}";

            string generatedContent;
            bool isFallback;

            try
            {
                generatedContent = await routingPort.RouteQueryAsync(
                    context,
                    prompt,
                    request.PreferredProvider,
                    cancellationToken);

                isFallback = generatedContent.StartsWith("[FALLBACK_PROVIDER_UNAVAILABLE]", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AI provider unavailable for GenerateRobotFrameworkDraft. Service={ServiceName}", request.ServiceName);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            var warnings = new List<string>();
            if (isFallback)
                warnings.Add("AI provider was unavailable — draft may be incomplete.");
            if (string.IsNullOrWhiteSpace(request.ContractSummary) && string.IsNullOrWhiteSpace(request.Spec))
                warnings.Add("No contract or formal spec provided; draft is based on textual description only.");

            Guid? artifactId = null;

            if (request.ReleaseId.HasValue && !isFallback)
            {
                var artifactResult = GeneratedTestArtifact.Generate(
                    request.ReleaseId.Value,
                    request.ServiceName,
                    RobotFramework,
                    generatedContent,
                    0.75m,
                    now);

                if (artifactResult.IsSuccess)
                {
                    await artifactRepository.AddAsync(artifactResult.Value!, cancellationToken);
                    artifactId = artifactResult.Value!.Id.Value;
                }
            }

            logger.LogInformation(
                "Robot Framework draft generated for '{ServiceName}'. IsFallback={IsFallback}, ArtifactId={ArtifactId}",
                request.ServiceName, isFallback, artifactId);

            return new Response(
                artifactId,
                request.ServiceName,
                request.OperationName,
                generatedContent,
                warnings,
                isFallback,
                now);
        }

        private static string BuildPrompt(Command request)
        {
            var parts = new List<string>
            {
                $"Generate a Robot Framework test draft for service: {request.ServiceName}"
            };

            if (!string.IsNullOrWhiteSpace(request.OperationName))
                parts.Add($"Target operation: {request.OperationName}");

            if (!string.IsNullOrWhiteSpace(request.ContractSummary))
                parts.Add($"API Contract:\n{request.ContractSummary}");

            if (!string.IsNullOrWhiteSpace(request.EndpointDescription))
                parts.Add($"Endpoint description:\n{request.EndpointDescription}");

            if (!string.IsNullOrWhiteSpace(request.Spec))
                parts.Add($"Specification:\n{request.Spec}");

            parts.Add("Output a Robot Framework test suite with: Settings, Variables, Keywords, and Test Cases sections. Include realistic test data and cover happy path plus main error scenarios.");

            return string.Join("\n\n", parts);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da geração do draft Robot Framework.</summary>
    public sealed record Response(
        Guid? ArtifactId,
        string ServiceName,
        string? OperationName,
        string GeneratedDraft,
        IReadOnlyList<string> Warnings,
        bool IsFallback,
        DateTimeOffset GeneratedAt);
}
