using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.ExternalAI.Ports;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Features.GenerateTestScenarios;

/// <summary>
/// Feature: GenerateTestScenarios — gera cenários de teste estruturados a partir de contrato,
/// serviço, mudança ou spec textual. Usa o provider de IA existente via IExternalAIRoutingPort,
/// persiste artefato em GeneratedTestArtifact quando releaseId é fornecido.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GenerateTestScenarios
{
    // ── COMMAND ───────────────────────────────────────────────────────────

    /// <summary>Comando para gerar cenários de teste com IA.</summary>
    public sealed record Command(
        string ServiceName,
        string? Spec,
        string? ChangeDescription,
        string? ContractSummary,
        Guid? ReleaseId,
        string? TestFramework,
        string? PreferredProvider) : ICommand<Response>;

    // ── VALIDATOR ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Spec).MaximumLength(10_000).When(x => x.Spec is not null);
            RuleFor(x => x.ChangeDescription).MaximumLength(5_000).When(x => x.ChangeDescription is not null);
            RuleFor(x => x.ContractSummary).MaximumLength(5_000).When(x => x.ContractSummary is not null);
            RuleFor(x => x).Must(x =>
                !string.IsNullOrWhiteSpace(x.Spec) ||
                !string.IsNullOrWhiteSpace(x.ChangeDescription) ||
                !string.IsNullOrWhiteSpace(x.ContractSummary))
                .WithMessage("At least one input context must be provided: Spec, ChangeDescription, or ContractSummary.");
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
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var framework = request.TestFramework ?? "xunit";
            var prompt = BuildPrompt(request, framework);
            var context = $"test-scenario-generation:{request.ServiceName}";

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
                logger.LogWarning(ex, "AI provider unavailable for GenerateTestScenarios. Service={ServiceName}", request.ServiceName);
                return Error.Business("AIKnowledge.Provider.Unavailable", "AI provider unavailable: {0}", ex.Message);
            }

            Guid? artifactId = null;

            if (request.ReleaseId.HasValue && !isFallback)
            {
                var artifactResult = GeneratedTestArtifact.Generate(
                    request.ReleaseId.Value,
                    request.ServiceName,
                    framework,
                    generatedContent,
                    0.80m,
                    now);

                if (artifactResult.IsSuccess)
                {
                    await artifactRepository.AddAsync(artifactResult.Value!, cancellationToken);
                    artifactId = artifactResult.Value!.Id.Value;
                }
            }

            logger.LogInformation(
                "Test scenarios generated for service '{ServiceName}'. Framework={Framework}, IsFallback={IsFallback}, ArtifactId={ArtifactId}",
                request.ServiceName, framework, isFallback, artifactId);

            return new Response(
                artifactId,
                request.ServiceName,
                framework,
                generatedContent,
                isFallback,
                now);
        }

        private static string BuildPrompt(Command request, string framework)
        {
            var parts = new List<string>
            {
                $"Generate structured test scenarios for service: {request.ServiceName}",
                $"Target test framework: {framework}"
            };

            if (!string.IsNullOrWhiteSpace(request.Spec))
                parts.Add($"Specification:\n{request.Spec}");

            if (!string.IsNullOrWhiteSpace(request.ContractSummary))
                parts.Add($"Contract/API summary:\n{request.ContractSummary}");

            if (!string.IsNullOrWhiteSpace(request.ChangeDescription))
                parts.Add($"Change description:\n{request.ChangeDescription}");

            parts.Add("Include: test scenario name, description, preconditions, steps, expected results, edge cases. Flag any assumptions or gaps.");

            return string.Join("\n\n", parts);
        }
    }

    // ── RESPONSE ──────────────────────────────────────────────────────────

    /// <summary>Resultado da geração de cenários de teste.</summary>
    public sealed record Response(
        Guid? ArtifactId,
        string ServiceName,
        string TestFramework,
        string GeneratedContent,
        bool IsFallback,
        DateTimeOffset GeneratedAt);
}
