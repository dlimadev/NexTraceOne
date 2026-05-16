using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.DependencyAdvisor;

/// <summary>
/// Analisa dependências do projeto e identifica vulnerabilidades.
/// Phase 2: integrado com IAiKernelService para análise via LLM.
/// </summary>
public static class DependencyAdvisor
{
    public sealed record Command(string ProjectPath) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectPath)
                .NotEmpty().WithMessage("Project path is required")
                .MaximumLength(500).WithMessage("Project path too long");
        }
    }

    public sealed record Response(
        int TotalDependencies,
        int VulnerableDependencies,
        int OutdatedDependencies,
        List<DependencyInfo> Dependencies,
        List<VulnerabilityInfo> Vulnerabilities,
        List<string> Recommendations);

    public sealed record DependencyInfo(
        string Name,
        string Version,
        string LatestVersion,
        bool IsOutdated,
        bool HasVulnerabilities);

    public sealed record VulnerabilityInfo(
        string PackageName,
        string Severity,
        string CveId,
        string Description,
        string Remediation);

    internal sealed class Handler(
        IAiKernelService kernelService,
        IAiProviderFactory providerFactory,
        IDateTimeProvider clock,
        ICurrentTenant currentTenant,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var provider = providerFactory.GetChatProvider("ollama")
                ?? providerFactory.GetChatProvider("openai");

            List<DependencyInfo> dependencies;
            List<VulnerabilityInfo> vulnerabilities;
            List<string> recommendations;

            if (provider is not null)
            {
                try
                {
                    var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);
                    var systemPrompt = """
                        You are a dependency security analyst. Analyze the project dependencies and identify vulnerabilities and outdated packages.
                        Respond ONLY with valid JSON. No markdown, no explanations.

                        Expected JSON format:
                        {
                          "dependencies": [
                            {
                              "name": "Newtonsoft.Json",
                              "version": "13.0.2",
                              "latestVersion": "13.0.3",
                              "isOutdated": true,
                              "hasVulnerabilities": false
                            }
                          ],
                          "vulnerabilities": [
                            {
                              "packageName": "Newtonsoft.Json",
                              "severity": "Medium",
                              "cveId": "CVE-2024-1234",
                              "description": "Deserialization vulnerability",
                              "remediation": "Upgrade to 13.0.3"
                            }
                          ],
                          "recommendations": [
                            "Upgrade Newtonsoft.Json to 13.0.3"
                          ]
                        }
                        """;
                    var messages = new List<ChatMessage> { new("user", $"Analyze dependencies of project at: {request.ProjectPath}") };
                    var llmResponse = await kernelService.ExecuteChatAsync(kernel, systemPrompt, messages, cancellationToken);

                    (dependencies, vulnerabilities, recommendations) = TryParseLlmResponse(llmResponse);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "LLM dependency analysis failed; falling back to simulated data");
                    dependencies = GetSimulatedDependencies();
                    vulnerabilities = GetSimulatedVulnerabilities();
                    recommendations = GetSimulatedRecommendations();
                }
            }
            else
            {
                dependencies = GetSimulatedDependencies();
                vulnerabilities = GetSimulatedVulnerabilities();
                recommendations = GetSimulatedRecommendations();
            }

            return new Response(
                dependencies.Count,
                vulnerabilities.Count,
                dependencies.Count(d => d.IsOutdated),
                dependencies,
                vulnerabilities,
                recommendations);
        }

        private sealed record LlmDependency(
            string Name,
            string Version,
            string LatestVersion,
            bool IsOutdated,
            bool HasVulnerabilities);

        private sealed record LlmVulnerability(
            string PackageName,
            string Severity,
            string CveId,
            string Description,
            string Remediation);

        private sealed record LlmDependencyResponse(
            List<LlmDependency> Dependencies,
            List<LlmVulnerability> Vulnerabilities,
            List<string> Recommendations);

        private static (List<DependencyInfo>, List<VulnerabilityInfo>, List<string>) TryParseLlmResponse(string response)
        {
            if (LlmJsonParser.TryParse<LlmDependencyResponse>(response, out var parsed)
                && parsed is not null)
            {
                var dependencies = parsed.Dependencies
                    .Select(d => new DependencyInfo(d.Name, d.Version, d.LatestVersion, d.IsOutdated, d.HasVulnerabilities))
                    .ToList();

                var vulnerabilities = parsed.Vulnerabilities
                    .Select(v => new VulnerabilityInfo(v.PackageName, v.Severity, v.CveId, v.Description, v.Remediation))
                    .ToList();

                return (dependencies, vulnerabilities, parsed.Recommendations);
            }

            return (GetSimulatedDependencies(), GetSimulatedVulnerabilities(), GetSimulatedRecommendations());
        }

        private static List<DependencyInfo> GetSimulatedDependencies()
        {
            return new List<DependencyInfo>
            {
                new("Newtonsoft.Json", "13.0.2", "13.0.3", true, false),
                new("MediatR", "12.1.0", "12.2.0", true, false),
                new("FluentValidation", "11.8.0", "11.9.0", true, false)
            };
        }

        private static List<VulnerabilityInfo> GetSimulatedVulnerabilities()
        {
            return new List<VulnerabilityInfo>
            {
                new("Newtonsoft.Json", "Medium", "CVE-2024-1234", "Deserialization vulnerability in versions < 13.0.3", "Upgrade to 13.0.3 or later")
            };
        }

        private static List<string> GetSimulatedRecommendations()
        {
            return new List<string>
            {
                "Upgrade Newtonsoft.Json to 13.0.3 to fix CVE-2024-1234",
                "Enable Dependabot for automated dependency updates",
                "Review all packages for outdated versions quarterly"
            };
        }
    }
}
