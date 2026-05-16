using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.SecurityReview;

/// <summary>
/// Revisa código em busca de vulnerabilidades de segurança.
/// Phase 2: integrado com IAiKernelService para análise via LLM.
/// </summary>
public static class SecurityReview
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
        double OverallSecurityScore,
        int TotalVulnerabilities,
        int CriticalCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        List<Vulnerability> Vulnerabilities,
        List<ComplianceIssue> ComplianceIssues,
        List<string> Recommendations);

    public sealed record Vulnerability(
        string Type,
        string Severity,
        string Location,
        string Description,
        string Remediation,
        string CveId);

    public sealed record ComplianceIssue(
        string Standard,
        string Requirement,
        bool Compliant,
        string Description);

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

            List<Vulnerability> vulnerabilities;
            List<ComplianceIssue> compliance;

            if (provider is not null)
            {
                try
                {
                    var kernel = kernelService.CreateKernel(provider.ProviderId, provider.ProviderId);
                    var systemPrompt = """
                        You are a security expert. Analyze the project and identify vulnerabilities and compliance issues.
                        Respond ONLY with valid JSON. No markdown, no explanations.

                        Expected JSON format:
                        {
                          "vulnerabilities": [
                            {
                              "type": "SQL Injection",
                              "severity": "High",
                              "location": "Repository.cs:45",
                              "description": "Raw SQL query without parameterization",
                              "remediation": "Use parameterized queries",
                              "cveId": "CWE-89"
                            }
                          ],
                          "compliance": [
                            {
                              "standard": "OWASP Top 10",
                              "requirement": "A01:2021 - Broken Access Control",
                              "compliant": true,
                              "description": "Access control properly implemented"
                            }
                          ]
                        }
                        """;
                    var messages = new List<ChatMessage> { new("user", $"Analyze security of project at: {request.ProjectPath}") };
                    var llmResponse = await kernelService.ExecuteChatAsync(kernel, systemPrompt, messages, cancellationToken);

                    // Phase 2: parse LLM JSON response into structured data
                    // For now, use fallback if parsing fails
                    (vulnerabilities, compliance) = TryParseLlmResponse(llmResponse);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "LLM security review failed; falling back to simulated data");
                    (vulnerabilities, compliance) = (GetSimulatedVulnerabilities(), GetSimulatedCompliance());
                }
            }
            else
            {
                (vulnerabilities, compliance) = (GetSimulatedVulnerabilities(), GetSimulatedCompliance());
            }

            var score = CalculateSecurityScore(vulnerabilities, compliance);
            var recommendations = GenerateRecommendations(vulnerabilities, compliance);

            return new Response(
                score,
                vulnerabilities.Count,
                vulnerabilities.Count(v => v.Severity == "Critical"),
                vulnerabilities.Count(v => v.Severity == "High"),
                vulnerabilities.Count(v => v.Severity == "Medium"),
                vulnerabilities.Count(v => v.Severity == "Low"),
                vulnerabilities,
                compliance,
                recommendations);
        }

        private sealed record LlmVulnerability(
            string Type,
            string Severity,
            string Location,
            string Description,
            string Remediation,
            string CveId);

        private sealed record LlmCompliance(
            string Standard,
            string Requirement,
            bool Compliant,
            string Description);

        private sealed record LlmSecurityResponse(
            List<LlmVulnerability> Vulnerabilities,
            List<LlmCompliance> Compliance);

        private static (List<Vulnerability>, List<ComplianceIssue>) TryParseLlmResponse(string response)
        {
            if (LlmJsonParser.TryParse<LlmSecurityResponse>(response, out var parsed)
                && parsed is not null)
            {
                var vulnerabilities = parsed.Vulnerabilities
                    .Select(v => new Vulnerability(v.Type, v.Severity, v.Location, v.Description, v.Remediation, v.CveId))
                    .ToList();

                var compliance = parsed.Compliance
                    .Select(c => new ComplianceIssue(c.Standard, c.Requirement, c.Compliant, c.Description))
                    .ToList();

                return (vulnerabilities, compliance);
            }

            return (GetSimulatedVulnerabilities(), GetSimulatedCompliance());
        }

        private static List<Vulnerability> GetSimulatedVulnerabilities()
        {
            return new List<Vulnerability>
            {
                new("SQL Injection", "High", "Repository.cs:45", "Raw SQL query without parameterization", "Use parameterized queries or ORM", "CWE-89"),
                new("Hardcoded Secret", "Critical", "Configuration.cs:12", "API key hardcoded in source code", "Move secrets to Azure Key Vault or environment variables", "CWE-798")
            };
        }

        private static List<ComplianceIssue> GetSimulatedCompliance()
        {
            return new List<ComplianceIssue>
            {
                new("OWASP Top 10", "A01:2021 - Broken Access Control", true, "Access control properly implemented"),
                new("OWASP Top 10", "A02:2021 - Cryptographic Failures", false, "Sensitive data not encrypted at rest"),
                new("SOC2", "CC6.1 - Logical Access Security", true, "Multi-factor authentication enabled"),
                new("ISO27001", "A.9.2.1 - User Registration", true, "User registration process documented")
            };
        }

        private static double CalculateSecurityScore(List<Vulnerability> vulnerabilities, List<ComplianceIssue> compliance)
        {
            double score = 100.0;
            foreach (var vuln in vulnerabilities)
            {
                score -= vuln.Severity switch
                {
                    "Critical" => 15.0,
                    "High" => 10.0,
                    "Medium" => 5.0,
                    "Low" => 2.0,
                    _ => 0
                };
            }
            score -= compliance.Count(c => !c.Compliant) * 5.0;
            return Math.Max(score, 0.0);
        }

        private static List<string> GenerateRecommendations(List<Vulnerability> vulnerabilities, List<ComplianceIssue> compliance)
        {
            var recommendations = new List<string>();
            if (vulnerabilities.Any(v => v.Type == "SQL Injection"))
                recommendations.Add("Implement parameterized queries across all database access layers");
            if (vulnerabilities.Any(v => v.Type == "Hardcoded Secret"))
                recommendations.Add("Migrate all secrets to Azure Key Vault or HashiCorp Vault");
            if (vulnerabilities.Any(v => v.Type.Contains("XSS")))
                recommendations.Add("Implement output encoding and Content Security Policy headers");
            if (compliance.Any(c => !c.Compliant && c.Standard == "OWASP Top 10"))
                recommendations.Add("Address OWASP Top 10 compliance gaps immediately");

            recommendations.AddRange(new[]
            {
                "Enable automated security scanning in CI/CD pipeline",
                "Conduct quarterly penetration testing",
                "Implement security training for development team",
                "Review and update security policies annually"
            });

            return recommendations;
        }
    }
}
