using System.Text.RegularExpressions;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.RunPreCommitGovernanceCheck;

/// <summary>
/// Feature: RunPreCommitGovernanceCheck — executa validação de governança sobre um scaffold
/// ou serviço antes de commit, verificando naming conventions, api-versioning, error-handling
/// e ownership completeness contra as políticas activas (HardEnforce / SoftEnforce).
/// Retorna lista de violações com severidade e sugestão de auto-fix.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class RunPreCommitGovernanceCheck
{
    /// <summary>Comando para executar a verificação de governança pré-commit.</summary>
    public sealed record Command(
        string ServiceName,
        string Domain,
        string? TechnicalOwner,
        string? RepositoryUrl,
        IReadOnlyList<string> ExposedApiPaths,
        IReadOnlyList<string> PolicyNamesToEnforce,
        string? ScaffoldLanguage = null) : ICommand<Response>;

    /// <summary>Valida o comando de verificação pré-commit.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExposedApiPaths).NotNull();
            RuleFor(x => x.PolicyNamesToEnforce).NotNull();
        }
    }

    /// <summary>Handler que executa as verificações de governança pré-commit.</summary>
    public sealed class Handler(IPolicyAsCodeRepository policyRepository)
        : ICommandHandler<Command, Response>
    {
        private static readonly Regex s_kebabCasePattern =
            new(@"^[a-z][a-z0-9\-]*[a-z0-9]$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        private static readonly Regex s_singleWordLowerPattern =
            new(@"^[a-z]+$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Carrega políticas activas que devem ser enforced
            var activePolicies = await policyRepository.ListAsync(
                PolicyDefinitionStatus.Active,
                enforcementMode: null,
                cancellationToken);

            // Filtra para as políticas pedidas (ou todas activas se a lista estiver vazia)
            var policiesToCheck = request.PolicyNamesToEnforce.Count == 0
                ? activePolicies
                : activePolicies.Where(p =>
                    request.PolicyNamesToEnforce.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            var violations = new List<GovernanceViolation>();
            var autoFixSuggestions = new List<string>();

            // Regra: naming-convention
            var namingPolicy = policiesToCheck.FirstOrDefault(p =>
                p.Name.Contains("naming", StringComparison.OrdinalIgnoreCase));
            if (namingPolicy is not null || request.PolicyNamesToEnforce.Count == 0)
                violations.AddRange(CheckNamingConvention(request, namingPolicy?.EnforcementMode ?? PolicyEnforcementMode.Advisory));

            // Regra: api-versioning
            var versioningPolicy = policiesToCheck.FirstOrDefault(p =>
                p.Name.Contains("versioning", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("api-version", StringComparison.OrdinalIgnoreCase));
            if (versioningPolicy is not null || request.PolicyNamesToEnforce.Count == 0)
                violations.AddRange(CheckApiVersioning(request, versioningPolicy?.EnforcementMode ?? PolicyEnforcementMode.Advisory));

            // Regra: ownership
            var ownershipPolicy = policiesToCheck.FirstOrDefault(p =>
                p.Name.Contains("ownership", StringComparison.OrdinalIgnoreCase));
            if (ownershipPolicy is not null || request.PolicyNamesToEnforce.Count == 0)
                violations.AddRange(CheckOwnership(request, ownershipPolicy?.EnforcementMode ?? PolicyEnforcementMode.Advisory));

            // Regra: error-handling
            var errorPolicy = policiesToCheck.FirstOrDefault(p =>
                p.Name.Contains("error", StringComparison.OrdinalIgnoreCase));
            if (errorPolicy is not null || request.PolicyNamesToEnforce.Count == 0)
                violations.AddRange(CheckErrorHandling(request, errorPolicy?.EnforcementMode ?? PolicyEnforcementMode.Advisory));

            // Determina auto-fix suggestions
            foreach (var v in violations.Where(v => v.CanAutoFix))
                autoFixSuggestions.Add($"[{v.RuleId}] {v.AutoFixHint}");

            var blocked = violations.Any(v => v.Severity == ViolationSeverity.Error
                && v.EnforcementMode == PolicyEnforcementMode.HardEnforce);

            return Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                Domain: request.Domain,
                PoliciesChecked: policiesToCheck.Count,
                ViolationCount: violations.Count,
                Blocked: blocked,
                Violations: violations.AsReadOnly(),
                AutoFixSuggestions: autoFixSuggestions.AsReadOnly(),
                Summary: BuildSummary(violations, blocked)));
        }

        private static IEnumerable<GovernanceViolation> CheckNamingConvention(
            Command req, PolicyEnforcementMode mode)
        {
            // Verifica se o nome do serviço segue kebab-case
            if (!s_kebabCasePattern.IsMatch(req.ServiceName))
            {
                yield return new GovernanceViolation(
                    RuleId: "NAMING-001",
                    Category: "naming-convention",
                    Message: $"Service name '{req.ServiceName}' should be kebab-case (lowercase letters, numbers, hyphens).",
                    Severity: mode >= PolicyEnforcementMode.HardEnforce ? ViolationSeverity.Error : ViolationSeverity.Warning,
                    EnforcementMode: mode,
                    Field: "serviceName",
                    CanAutoFix: true,
                    AutoFixHint: $"Rename to: {req.ServiceName.ToLowerInvariant().Replace(' ', '-').Replace('_', '-')}");
            }

            // Verifica se o domínio segue kebab-case
            if (!s_kebabCasePattern.IsMatch(req.Domain) && !s_singleWordLowerPattern.IsMatch(req.Domain))
            {
                yield return new GovernanceViolation(
                    RuleId: "NAMING-002",
                    Category: "naming-convention",
                    Message: $"Domain '{req.Domain}' should be kebab-case.",
                    Severity: ViolationSeverity.Warning,
                    EnforcementMode: mode,
                    Field: "domain",
                    CanAutoFix: true,
                    AutoFixHint: $"Rename to: {req.Domain.ToLowerInvariant().Replace(' ', '-').Replace('_', '-')}");
            }
        }

        private static IEnumerable<GovernanceViolation> CheckApiVersioning(
            Command req, PolicyEnforcementMode mode)
        {
            foreach (var path in req.ExposedApiPaths)
            {
                if (!path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new GovernanceViolation(
                        RuleId: "VERSION-001",
                        Category: "api-versioning",
                        Message: $"API path '{path}' does not include version segment (e.g. /api/v1/...).",
                        Severity: mode >= PolicyEnforcementMode.HardEnforce ? ViolationSeverity.Error : ViolationSeverity.Warning,
                        EnforcementMode: mode,
                        Field: "exposedApiPaths",
                        CanAutoFix: true,
                        AutoFixHint: $"Rename to: /api/v1{path.TrimStart('/')}");
                }
            }
        }

        private static IEnumerable<GovernanceViolation> CheckOwnership(
            Command req, PolicyEnforcementMode mode)
        {
            if (string.IsNullOrWhiteSpace(req.TechnicalOwner))
            {
                yield return new GovernanceViolation(
                    RuleId: "OWNERSHIP-001",
                    Category: "ownership",
                    Message: "Technical owner is not defined. Every service must have an identifiable technical owner.",
                    Severity: mode >= PolicyEnforcementMode.HardEnforce ? ViolationSeverity.Error : ViolationSeverity.Warning,
                    EnforcementMode: mode,
                    Field: "technicalOwner",
                    CanAutoFix: false,
                    AutoFixHint: "Assign a technical owner to the service.");
            }

            if (string.IsNullOrWhiteSpace(req.RepositoryUrl))
            {
                yield return new GovernanceViolation(
                    RuleId: "OWNERSHIP-002",
                    Category: "ownership",
                    Message: "Repository URL is not defined. Services should be linked to a source code repository.",
                    Severity: ViolationSeverity.Warning,
                    EnforcementMode: mode,
                    Field: "repositoryUrl",
                    CanAutoFix: false,
                    AutoFixHint: "Provide the repository URL in the service registration.");
            }
        }

        private static IEnumerable<GovernanceViolation> CheckErrorHandling(
            Command req, PolicyEnforcementMode mode)
        {
            // Verifica se a linguagem suporta análise de erro (apenas para .NET por agora)
            if (!string.IsNullOrEmpty(req.ScaffoldLanguage) &&
                req.ScaffoldLanguage.Equals("DotNet", StringComparison.OrdinalIgnoreCase))
            {
                // Aviso de boas práticas — sem bloqueio
                yield return new GovernanceViolation(
                    RuleId: "ERROR-001",
                    Category: "error-handling",
                    Message: "Ensure all API endpoints return RFC 7807 Problem Details on errors (use ProblemDetails middleware).",
                    Severity: ViolationSeverity.Info,
                    EnforcementMode: PolicyEnforcementMode.Advisory,
                    Field: "errorHandling",
                    CanAutoFix: true,
                    AutoFixHint: "Add builder.Services.AddProblemDetails() and app.UseExceptionHandler() to Program.cs.");
            }
        }

        private static string BuildSummary(IReadOnlyList<GovernanceViolation> violations, bool blocked)
        {
            if (violations.Count == 0)
                return "All governance checks passed. Service is ready to commit.";

            var errors = violations.Count(v => v.Severity == ViolationSeverity.Error);
            var warnings = violations.Count(v => v.Severity == ViolationSeverity.Warning);
            var infos = violations.Count(v => v.Severity == ViolationSeverity.Info);

            var parts = new List<string>();
            if (errors > 0) parts.Add($"{errors} error(s)");
            if (warnings > 0) parts.Add($"{warnings} warning(s)");
            if (infos > 0) parts.Add($"{infos} info(s)");

            var status = blocked ? "BLOCKED" : "PASSED WITH WARNINGS";
            return $"Pre-commit check {status}: {string.Join(", ", parts)}.";
        }
    }

    /// <summary>Severidade de uma violação de governança.</summary>
    public enum ViolationSeverity { Info, Warning, Error }

    /// <summary>Violação de governança detectada.</summary>
    public sealed record GovernanceViolation(
        string RuleId,
        string Category,
        string Message,
        ViolationSeverity Severity,
        PolicyEnforcementMode EnforcementMode,
        string Field,
        bool CanAutoFix,
        string AutoFixHint);

    /// <summary>Resposta da verificação pré-commit com lista de violações e recomendações.</summary>
    public sealed record Response(
        string ServiceName,
        string Domain,
        int PoliciesChecked,
        int ViolationCount,
        bool Blocked,
        IReadOnlyList<GovernanceViolation> Violations,
        IReadOnlyList<string> AutoFixSuggestions,
        string Summary);
}
