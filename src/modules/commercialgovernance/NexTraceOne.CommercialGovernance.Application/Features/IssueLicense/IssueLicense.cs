using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;
using NexTraceOne.Licensing.Domain.Enums;

namespace NexTraceOne.Licensing.Application.Features.IssueLicense;

/// <summary>
/// Feature: IssueLicense — operação de vendor ops para emitir uma nova licença.
///
/// Usada pelo backoffice interno da NexTraceOne para criar licenças comerciais.
/// Suporta configuração completa: tipo, edição, deployment model, ativação, capabilities e quotas.
/// Gera uma chave de licença única automaticamente.
///
/// Permissão requerida: licensing:vendor:license:create
/// </summary>
public static class IssueLicense
{
    /// <summary>Comando para emissão de uma nova licença.</summary>
    public sealed record Command(
        string CustomerName,
        int DurationDays,
        int MaxActivations,
        LicenseType Type = LicenseType.Standard,
        LicenseEdition Edition = LicenseEdition.Professional,
        int GracePeriodDays = 15,
        DeploymentModel DeploymentModel = DeploymentModel.SaaS,
        ActivationMode ActivationMode = ActivationMode.Online,
        CommercialModel CommercialModel = CommercialModel.Subscription,
        MeteringMode MeteringMode = MeteringMode.RealTime) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de emissão.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DurationDays).InclusiveBetween(1, 3650);
            RuleFor(x => x.MaxActivations).GreaterThan(0);
            RuleFor(x => x.GracePeriodDays).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Handler que cria uma licença comercial completa.
    /// Gera chave única, define capabilities e quotas conforme a edição.
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var licenseKey = $"LIC-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

            var capabilities = GetCapabilitiesForEdition(request.Edition);
            var quotas = GetQuotasForEdition(request.Edition);

            var license = License.Create(
                licenseKey,
                request.CustomerName,
                now,
                now.AddDays(request.DurationDays),
                request.MaxActivations,
                capabilities,
                quotas,
                request.Type,
                request.Edition,
                request.GracePeriodDays,
                request.DeploymentModel,
                request.ActivationMode,
                request.CommercialModel,
                request.MeteringMode);

            licenseRepository.Add(license);

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.CustomerName,
                license.IssuedAt,
                license.ExpiresAt,
                request.Type.ToString(),
                request.Edition.ToString(),
                request.DeploymentModel.ToString());
        }

        /// <summary>Retorna capabilities padrão conforme a edição comercial.</summary>
        private static List<LicenseCapability> GetCapabilitiesForEdition(LicenseEdition edition) => edition switch
        {
            LicenseEdition.Community => [
                LicenseCapability.Create("catalog:read", "Catalog Read"),
                LicenseCapability.Create("contracts:import", "Contract Import"),
                LicenseCapability.Create("audit:read", "Audit Trail Read")
            ],
            LicenseEdition.Professional => [
                LicenseCapability.Create("catalog:read", "Catalog Read"),
                LicenseCapability.Create("catalog:write", "Catalog Write"),
                LicenseCapability.Create("contracts:import", "Contract Import"),
                LicenseCapability.Create("contracts:diff", "Semantic Diff"),
                LicenseCapability.Create("releases:manage", "Release Management"),
                LicenseCapability.Create("workflow:basic", "Basic Workflow"),
                LicenseCapability.Create("audit:read", "Audit Trail Read")
            ],
            LicenseEdition.Enterprise => [
                LicenseCapability.Create("catalog:read", "Catalog Read"),
                LicenseCapability.Create("catalog:write", "Catalog Write"),
                LicenseCapability.Create("contracts:import", "Contract Import"),
                LicenseCapability.Create("contracts:diff", "Semantic Diff"),
                LicenseCapability.Create("releases:manage", "Release Management"),
                LicenseCapability.Create("workflow:basic", "Basic Workflow"),
                LicenseCapability.Create("workflow:advanced", "Advanced Workflow"),
                LicenseCapability.Create("audit:read", "Audit Trail Read"),
                LicenseCapability.Create("audit:export", "Audit Export"),
                LicenseCapability.Create("ai:consultation", "AI Consultation")
            ],
            LicenseEdition.Unlimited => [
                LicenseCapability.Create("catalog:read", "Catalog Read"),
                LicenseCapability.Create("catalog:write", "Catalog Write"),
                LicenseCapability.Create("contracts:import", "Contract Import"),
                LicenseCapability.Create("contracts:diff", "Semantic Diff"),
                LicenseCapability.Create("releases:manage", "Release Management"),
                LicenseCapability.Create("workflow:basic", "Basic Workflow"),
                LicenseCapability.Create("workflow:advanced", "Advanced Workflow"),
                LicenseCapability.Create("audit:read", "Audit Trail Read"),
                LicenseCapability.Create("audit:export", "Audit Export"),
                LicenseCapability.Create("ai:consultation", "AI Consultation"),
                LicenseCapability.Create("ai:advanced", "Advanced AI")
            ],
            _ => []
        };

        /// <summary>Retorna quotas padrão conforme a edição comercial.</summary>
        private static List<UsageQuota> GetQuotasForEdition(LicenseEdition edition) => edition switch
        {
            LicenseEdition.Community => [
                UsageQuota.Create("api.count", 10, enforcementLevel: EnforcementLevel.Hard),
                UsageQuota.Create("environment.count", 1, enforcementLevel: EnforcementLevel.Hard),
                UsageQuota.Create("user.count", 3, enforcementLevel: EnforcementLevel.Hard)
            ],
            LicenseEdition.Professional => [
                UsageQuota.Create("api.count", 100, enforcementLevel: EnforcementLevel.Hard),
                UsageQuota.Create("environment.count", 5, enforcementLevel: EnforcementLevel.Hard),
                UsageQuota.Create("user.count", 25, enforcementLevel: EnforcementLevel.Soft)
            ],
            LicenseEdition.Enterprise => [
                UsageQuota.Create("api.count", 500, enforcementLevel: EnforcementLevel.Soft),
                UsageQuota.Create("environment.count", 20, enforcementLevel: EnforcementLevel.Soft),
                UsageQuota.Create("user.count", 100, enforcementLevel: EnforcementLevel.Soft)
            ],
            LicenseEdition.Unlimited => [
                UsageQuota.Create("api.count", 10000, enforcementLevel: EnforcementLevel.NeverBreak),
                UsageQuota.Create("environment.count", 1000, enforcementLevel: EnforcementLevel.NeverBreak),
                UsageQuota.Create("user.count", 10000, enforcementLevel: EnforcementLevel.NeverBreak)
            ],
            _ => []
        };
    }

    /// <summary>Resposta da emissão de licença.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt,
        string LicenseType,
        string Edition,
        string DeploymentModel);
}
