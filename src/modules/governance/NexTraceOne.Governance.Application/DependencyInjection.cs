using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Governance.Application.Features.ApplyGovernancePack;
using NexTraceOne.Governance.Application.Features.ApproveGovernanceWaiver;
using NexTraceOne.Governance.Application.Features.CreateDelegatedAdministration;
using NexTraceOne.Governance.Application.Features.CreateDomain;
using NexTraceOne.Governance.Application.Features.CreateGovernancePack;
using NexTraceOne.Governance.Application.Features.CreateGovernanceWaiver;
using NexTraceOne.Governance.Application.Features.CreatePackVersion;
using NexTraceOne.Governance.Application.Features.CreateTeam;
using NexTraceOne.Governance.Application.Features.GetBenchmarking;
using NexTraceOne.Governance.Application.Features.GetComplianceGaps;
using NexTraceOne.Governance.Application.Features.GetComplianceSummary;
using NexTraceOne.Governance.Application.Features.GetControlsSummary;
using NexTraceOne.Governance.Application.Features.GetCrossDomainDependencies;
using NexTraceOne.Governance.Application.Features.GetCrossTeamDependencies;
using NexTraceOne.Governance.Application.Features.GetDomainDetail;
using NexTraceOne.Governance.Application.Features.GetDomainFinOps;
using NexTraceOne.Governance.Application.Features.GetDomainGovernanceSummary;
using NexTraceOne.Governance.Application.Features.GetEfficiencyIndicators;
using NexTraceOne.Governance.Application.Features.GetEvidencePackage;
using NexTraceOne.Governance.Application.Features.GetExecutiveDrillDown;
using NexTraceOne.Governance.Application.Features.GetExecutiveOverview;
using NexTraceOne.Governance.Application.Features.GetExecutiveTrends;
using NexTraceOne.Governance.Application.Features.GetFinOpsSummary;
using NexTraceOne.Governance.Application.Features.GetFinOpsTrends;
using NexTraceOne.Governance.Application.Features.GetGovernancePack;
using NexTraceOne.Governance.Application.Features.GetMaturityScorecards;
using NexTraceOne.Governance.Application.Features.GetOnboardingContext;
using NexTraceOne.Governance.Application.Features.GetPackApplicability;
using NexTraceOne.Governance.Application.Features.GetPackCoverage;
using NexTraceOne.Governance.Application.Features.GetPlatformEvents;
using NexTraceOne.Governance.Application.Features.GetPlatformJobs;
using NexTraceOne.Governance.Application.Features.GetPolicy;
using NexTraceOne.Governance.Application.Features.GetReportsSummary;
using NexTraceOne.Governance.Application.Features.GetRiskHeatmap;
using NexTraceOne.Governance.Application.Features.GetRiskSummary;
using NexTraceOne.Governance.Application.Features.GetServiceFinOps;
using NexTraceOne.Governance.Application.Features.GetTeamDetail;
using NexTraceOne.Governance.Application.Features.GetTeamFinOps;
using NexTraceOne.Governance.Application.Features.GetTeamGovernanceSummary;
using NexTraceOne.Governance.Application.Features.GetWasteSignals;
using NexTraceOne.Governance.Application.Features.ListEvidencePackages;
using NexTraceOne.Governance.Application.Features.ListGovernancePacks;
using NexTraceOne.Governance.Application.Features.ListGovernanceWaivers;
using NexTraceOne.Governance.Application.Features.ListPackVersions;
using NexTraceOne.Governance.Application.Features.ListPolicies;
using NexTraceOne.Governance.Application.Features.RejectGovernanceWaiver;
using NexTraceOne.Governance.Application.Features.RunComplianceChecks;
using NexTraceOne.Governance.Application.Features.UpdateDomain;
using NexTraceOne.Governance.Application.Features.UpdateGovernancePack;
using NexTraceOne.Governance.Application.Features.UpdateTeam;

namespace NexTraceOne.Governance.Application;

/// <summary>
/// Registra serviços da camada Application do módulo Governance.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Governance ao contêiner de DI.</summary>
    public static IServiceCollection AddGovernanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Command validators (existing)
        services.AddTransient<IValidator<UpdateDomain.Command>, UpdateDomain.Validator>();
        services.AddTransient<IValidator<ApproveGovernanceWaiver.Command>, ApproveGovernanceWaiver.Validator>();
        services.AddTransient<IValidator<RunComplianceChecks.Query>, RunComplianceChecks.Validator>();
        services.AddTransient<IValidator<CreateGovernanceWaiver.Command>, CreateGovernanceWaiver.Validator>();
        services.AddTransient<IValidator<UpdateGovernancePack.Command>, UpdateGovernancePack.Validator>();
        services.AddTransient<IValidator<CreateDomain.Command>, CreateDomain.Validator>();
        services.AddTransient<IValidator<CreateDelegatedAdministration.Command>, CreateDelegatedAdministration.Validator>();
        services.AddTransient<IValidator<CreatePackVersion.Command>, CreatePackVersion.Validator>();
        services.AddTransient<IValidator<ApplyGovernancePack.Command>, ApplyGovernancePack.Validator>();
        services.AddTransient<IValidator<CreateTeam.Command>, CreateTeam.Validator>();
        services.AddTransient<IValidator<RejectGovernanceWaiver.Command>, RejectGovernanceWaiver.Validator>();
        services.AddTransient<IValidator<CreateGovernancePack.Command>, CreateGovernancePack.Validator>();
        services.AddTransient<IValidator<UpdateTeam.Command>, UpdateTeam.Validator>();

        // Query validators (new)
        services.AddTransient<IValidator<GetExecutiveDrillDown.Query>, GetExecutiveDrillDown.Validator>();
        services.AddTransient<IValidator<GetEvidencePackage.Query>, GetEvidencePackage.Validator>();
        services.AddTransient<IValidator<GetCrossDomainDependencies.Query>, GetCrossDomainDependencies.Validator>();
        services.AddTransient<IValidator<GetGovernancePack.Query>, GetGovernancePack.Validator>();
        services.AddTransient<IValidator<GetComplianceGaps.Query>, GetComplianceGaps.Validator>();
        services.AddTransient<IValidator<ListGovernanceWaivers.Query>, ListGovernanceWaivers.Validator>();
        services.AddTransient<IValidator<GetDomainGovernanceSummary.Query>, GetDomainGovernanceSummary.Validator>();
        services.AddTransient<IValidator<GetControlsSummary.Query>, GetControlsSummary.Validator>();
        services.AddTransient<IValidator<GetTeamGovernanceSummary.Query>, GetTeamGovernanceSummary.Validator>();
        services.AddTransient<IValidator<GetComplianceSummary.Query>, GetComplianceSummary.Validator>();
        services.AddTransient<IValidator<GetReportsSummary.Query>, GetReportsSummary.Validator>();
        services.AddTransient<IValidator<GetExecutiveOverview.Query>, GetExecutiveOverview.Validator>();
        services.AddTransient<IValidator<GetPlatformEvents.Query>, GetPlatformEvents.Validator>();
        services.AddTransient<IValidator<GetExecutiveTrends.Query>, GetExecutiveTrends.Validator>();
        services.AddTransient<IValidator<ListGovernancePacks.Query>, ListGovernancePacks.Validator>();
        services.AddTransient<IValidator<GetRiskHeatmap.Query>, GetRiskHeatmap.Validator>();
        services.AddTransient<IValidator<GetWasteSignals.Query>, GetWasteSignals.Validator>();
        services.AddTransient<IValidator<GetFinOpsTrends.Query>, GetFinOpsTrends.Validator>();
        services.AddTransient<IValidator<GetFinOpsSummary.Query>, GetFinOpsSummary.Validator>();
        services.AddTransient<IValidator<GetPlatformJobs.Query>, GetPlatformJobs.Validator>();
        services.AddTransient<IValidator<GetBenchmarking.Query>, GetBenchmarking.Validator>();
        services.AddTransient<IValidator<GetRiskSummary.Query>, GetRiskSummary.Validator>();
        services.AddTransient<IValidator<GetEfficiencyIndicators.Query>, GetEfficiencyIndicators.Validator>();
        services.AddTransient<IValidator<GetMaturityScorecards.Query>, GetMaturityScorecards.Validator>();
        services.AddTransient<IValidator<GetPackApplicability.Query>, GetPackApplicability.Validator>();
        services.AddTransient<IValidator<GetPackCoverage.Query>, GetPackCoverage.Validator>();
        services.AddTransient<IValidator<GetDomainDetail.Query>, GetDomainDetail.Validator>();
        services.AddTransient<IValidator<GetTeamDetail.Query>, GetTeamDetail.Validator>();
        services.AddTransient<IValidator<GetTeamFinOps.Query>, GetTeamFinOps.Validator>();
        services.AddTransient<IValidator<GetDomainFinOps.Query>, GetDomainFinOps.Validator>();
        services.AddTransient<IValidator<GetServiceFinOps.Query>, GetServiceFinOps.Validator>();
        services.AddTransient<IValidator<GetCrossTeamDependencies.Query>, GetCrossTeamDependencies.Validator>();
        services.AddTransient<IValidator<ListEvidencePackages.Query>, ListEvidencePackages.Validator>();
        services.AddTransient<IValidator<ListPackVersions.Query>, ListPackVersions.Validator>();
        services.AddTransient<IValidator<ListPolicies.Query>, ListPolicies.Validator>();
        services.AddTransient<IValidator<GetPolicy.Query>, GetPolicy.Validator>();
        services.AddTransient<IValidator<GetOnboardingContext.Query>, GetOnboardingContext.Validator>();

        return services;
    }
}
