using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetSpectralMarketplace;

/// <summary>
/// Feature: GetSpectralMarketplace — retorna o catálogo de pacotes Spectral disponíveis
/// para activação no Contract Linting Marketplace.
///
/// Pacotes predefinidos:
///   - enterprise        : regras de governança empresarial (breaking changes, versioning)
///   - security          : regras de segurança de API (autenticação, autorização, exposição PII)
///   - accessibility     : regras de conformidade com standards de acessibilidade de API
///   - internal-platform : regras de convenção interna da plataforma NexTraceOne
///
/// Config key: spectral.marketplace.packages_enabled
/// CC-08: Contract Linting Marketplace.
/// </summary>
public static class GetSpectralMarketplace
{
    public sealed record Query : IQuery<Response>;

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private static readonly IReadOnlyList<MarketplacePackage> Packages =
        [
            new MarketplacePackage(
                Id: "enterprise",
                DisplayName: "Enterprise Governance Pack",
                Description: "Breaking change detection, API versioning conventions, backward compatibility gates.",
                Category: "governance",
                RuleCount: 24,
                Publisher: "NexTraceOne",
                Version: "2.1.0",
                Tags: ["breaking-change", "versioning", "governance"]),

            new MarketplacePackage(
                Id: "security",
                DisplayName: "API Security Pack",
                Description: "Authentication, authorization, PII data exposure, OWASP API Top 10 rules.",
                Category: "security",
                RuleCount: 31,
                Publisher: "NexTraceOne",
                Version: "1.4.2",
                Tags: ["security", "oauth", "pii", "owasp"]),

            new MarketplacePackage(
                Id: "accessibility",
                DisplayName: "API Accessibility Pack",
                Description: "HTTP status codes, error response formats, pagination conventions, i18n support.",
                Category: "accessibility",
                RuleCount: 18,
                Publisher: "NexTraceOne",
                Version: "1.0.1",
                Tags: ["accessibility", "http", "errors", "pagination"]),

            new MarketplacePackage(
                Id: "internal-platform",
                DisplayName: "NexTraceOne Platform Conventions",
                Description: "Internal platform standards: naming conventions, header policies, contract metadata requirements.",
                Category: "internal",
                RuleCount: 42,
                Publisher: "NexTraceOne",
                Version: "3.0.0",
                Tags: ["internal", "naming", "headers", "metadata"]),
        ];

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
            => Task.FromResult(Result<Response>.Success(new Response(Packages)));
    }

    public sealed record MarketplacePackage(
        string Id,
        string DisplayName,
        string Description,
        string Category,
        int RuleCount,
        string Publisher,
        string Version,
        IReadOnlyList<string> Tags);

    public sealed record Response(IReadOnlyList<MarketplacePackage> Packages);
}
