using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Features.AddDraftExample;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyProvenanceReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetExperimentGovernanceReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagInventoryReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetFeatureFlagRiskReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetVulnerabilityExposureReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSecurityPatchComplianceReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDocumentationHealthReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSbomCoverageReport;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSupplyChainRiskReport;
using NexTraceOne.Catalog.Application.Contracts.Features.IngestFeatureFlagState;
using NexTraceOne.Catalog.Application.Contracts.Features.IngestSbomRecord;
using NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.ClassifyBreakingChange;
using NexTraceOne.Catalog.Application.Contracts.Features.ComputeContractHealthDashboard;
using NexTraceOne.Catalog.Application.Contracts.Features.ComputeSemanticDiff;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateBackgroundServiceDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateContractVersion;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateEventDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateSoapDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractRules;
using NexTraceOne.Catalog.Application.Contracts.Features.EvaluateDesignGuidelines;
using NexTraceOne.Catalog.Application.Contracts.Features.ExportContract;
using NexTraceOne.Catalog.Application.Contracts.Features.ExportContractMultiFormat;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateEvidencePack;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateMockConfiguration;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateScorecard;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateSemanticChangelog;
using NexTraceOne.Catalog.Application.Contracts.Features.GetCompatibilityAssessment;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerExpectations;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractHistory;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDeprecationProgress;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.GetBackgroundServiceContractDetail;
using NexTraceOne.Catalog.Application.Contracts.Features.GetEventContractDetail;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSoapContractDetail;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractListing;
using NexTraceOne.Catalog.Application.Contracts.Features.InferDependenciesFromContracts;
using NexTraceOne.Catalog.Application.Contracts.Features.InitiateContractDeprecation;
using NexTraceOne.Catalog.Application.Contracts.Features.SuggestSchemaFromContext;
using NexTraceOne.Catalog.Application.Contracts.Features.ImportAsyncApiContract;
using NexTraceOne.Catalog.Application.Contracts.Features.ImportContract;
using NexTraceOne.Catalog.Application.Contracts.Features.ImportWsdlContract;
using NexTraceOne.Catalog.Application.Contracts.Features.ListDraftReviews;
using NexTraceOne.Catalog.Application.Contracts.Features.ListDrafts;
using NexTraceOne.Catalog.Application.Contracts.Features.LockContractVersion;
using NexTraceOne.Catalog.Application.Contracts.Features.PublishDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.PublishToMarketplace;
using NexTraceOne.Catalog.Application.Contracts.Features.RejectDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.SubmitDraftForReview;
using NexTraceOne.Catalog.Application.Contracts.Features.RegisterBackgroundServiceContract;
using NexTraceOne.Catalog.Application.Contracts.Features.SuggestSemanticVersion;
using NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftContent;
using NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftMetadata;
using NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractIntegrity;
using NexTraceOne.Catalog.Application.Contracts.Features.PropagateCanonicalEntityChange;
using NexTraceOne.Catalog.Application.Contracts.Features.RegisterConsumerExpectation;
using NexTraceOne.Catalog.Application.Contracts.Features.SearchMarketplace;
using NexTraceOne.Catalog.Application.Contracts.Features.SubmitContractReview;
using NexTraceOne.Catalog.Application.Contracts.Features.VerifyProviderCompatibility;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Registra serviços da camada Application do módulo Contracts.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo Contracts ao contêiner de DI.</summary>
    public static IServiceCollection AddContractsApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<ImportContract.Command>, ImportContract.Validator>();
        services.AddTransient<IValidator<CreateContractVersion.Command>, CreateContractVersion.Validator>();
        services.AddTransient<IValidator<ComputeSemanticDiff.Query>, ComputeSemanticDiff.Validator>();
        services.AddTransient<IValidator<ClassifyBreakingChange.Query>, ClassifyBreakingChange.Validator>();
        services.AddTransient<IValidator<SuggestSemanticVersion.Query>, SuggestSemanticVersion.Validator>();
        services.AddTransient<IValidator<GetContractHistory.Query>, GetContractHistory.Validator>();
        services.AddTransient<IValidator<LockContractVersion.Command>, LockContractVersion.Validator>();
        services.AddTransient<IValidator<ExportContract.Query>, ExportContract.Validator>();
        services.AddTransient<IValidator<ValidateContractIntegrity.Query>, ValidateContractIntegrity.Validator>();
        services.AddTransient<IValidator<GenerateScorecard.Query>, GenerateScorecard.Validator>();
        services.AddTransient<IValidator<GenerateEvidencePack.Query>, GenerateEvidencePack.Validator>();
        services.AddTransient<IValidator<EvaluateContractRules.Query>, EvaluateContractRules.Validator>();
        services.AddTransient<IValidator<GetCompatibilityAssessment.Query>, GetCompatibilityAssessment.Validator>();

        // Contract Studio — Draft CRUD
        services.AddTransient<IValidator<CreateDraft.Command>, CreateDraft.Validator>();
        services.AddTransient<IValidator<GetDraft.Query>, GetDraft.Validator>();
        services.AddTransient<IValidator<ListDrafts.Query>, ListDrafts.Validator>();
        services.AddTransient<IValidator<UpdateDraftContent.Command>, UpdateDraftContent.Validator>();
        services.AddTransient<IValidator<UpdateDraftMetadata.Command>, UpdateDraftMetadata.Validator>();

        // Contract Studio — Review workflow
        services.AddTransient<IValidator<SubmitDraftForReview.Command>, SubmitDraftForReview.Validator>();
        services.AddTransient<IValidator<ApproveDraft.Command>, ApproveDraft.Validator>();
        services.AddTransient<IValidator<RejectDraft.Command>, RejectDraft.Validator>();
        services.AddTransient<IValidator<ListDraftReviews.Query>, ListDraftReviews.Validator>();

        // Contract Studio — Publication & AI
        services.AddTransient<IValidator<PublishDraft.Command>, PublishDraft.Validator>();
        services.AddTransient<IValidator<GenerateDraftFromAi.Command>, GenerateDraftFromAi.Validator>();
        services.AddTransient<IValidator<AddDraftExample.Command>, AddDraftExample.Validator>();

        // SOAP/WSDL — workflow específico de contratos SOAP
        services.AddTransient<IValidator<ImportWsdlContract.Command>, ImportWsdlContract.Validator>();
        services.AddTransient<IValidator<CreateSoapDraft.Command>, CreateSoapDraft.Validator>();
        services.AddTransient<IValidator<GetSoapContractDetail.Query>, GetSoapContractDetail.Validator>();

        // Event Contracts / AsyncAPI — workflow específico de contratos de eventos
        services.AddTransient<IValidator<ImportAsyncApiContract.Command>, ImportAsyncApiContract.Validator>();
        services.AddTransient<IValidator<CreateEventDraft.Command>, CreateEventDraft.Validator>();
        services.AddTransient<IValidator<GetEventContractDetail.Query>, GetEventContractDetail.Validator>();

        // Background Service Contracts — workflow específico de jobs/workers/schedulers
        services.AddTransient<IValidator<RegisterBackgroundServiceContract.Command>, RegisterBackgroundServiceContract.Validator>();
        services.AddTransient<IValidator<CreateBackgroundServiceDraft.Command>, CreateBackgroundServiceDraft.Validator>();
        services.AddTransient<IValidator<GetBackgroundServiceContractDetail.Query>, GetBackgroundServiceContractDetail.Validator>();

        // Phase 5/6 — Mock, Design Guidelines, Canonical Propagation, CDCT, Multi-Format Export
        services.AddTransient<IValidator<GenerateMockConfiguration.Query>, GenerateMockConfiguration.Validator>();
        services.AddTransient<IValidator<EvaluateDesignGuidelines.Query>, EvaluateDesignGuidelines.Validator>();
        services.AddTransient<IValidator<PropagateCanonicalEntityChange.Command>, PropagateCanonicalEntityChange.Validator>();
        services.AddTransient<IValidator<RegisterConsumerExpectation.Command>, RegisterConsumerExpectation.Validator>();
        services.AddTransient<IValidator<GetContractConsumerExpectations.Query>, GetContractConsumerExpectations.Validator>();
        services.AddTransient<IValidator<VerifyProviderCompatibility.Query>, VerifyProviderCompatibility.Validator>();
        services.AddTransient<IValidator<ExportContractMultiFormat.Query>, ExportContractMultiFormat.Validator>();

        // Phase 6 — Intelligence & Governance
        services.AddTransient<IValidator<InferDependenciesFromContracts.Command>, InferDependenciesFromContracts.Validator>();
        services.AddTransient<IValidator<GenerateSemanticChangelog.Query>, GenerateSemanticChangelog.Validator>();
        services.AddTransient<IValidator<SuggestSchemaFromContext.Query>, SuggestSchemaFromContext.Validator>();
        services.AddTransient<IValidator<InitiateContractDeprecation.Command>, InitiateContractDeprecation.Validator>();
        services.AddTransient<IValidator<GetDeprecationProgress.Query>, GetDeprecationProgress.Validator>();

        // Phase 7 — Contract Marketplace Interno
        services.AddTransient<IValidator<PublishToMarketplace.Command>, PublishToMarketplace.Validator>();
        services.AddTransient<IValidator<SubmitContractReview.Command>, SubmitContractReview.Validator>();
        services.AddTransient<IValidator<SearchMarketplace.Query>, SearchMarketplace.Validator>();
        services.AddTransient<IValidator<GetContractListing.Query>, GetContractListing.Validator>();

        // ── Wave AO — Supply Chain & Dependency Provenance ─────────────────
        services.AddTransient<IValidator<IngestSbomRecord.Command>, IngestSbomRecord.Validator>();
        services.AddTransient<IValidator<GetSbomCoverageReport.Query>, GetSbomCoverageReport.Validator>();
        services.AddTransient<IValidator<GetDependencyProvenanceReport.Query>, GetDependencyProvenanceReport.Validator>();
        services.AddTransient<IValidator<GetSupplyChainRiskReport.Query>, GetSupplyChainRiskReport.Validator>();

        // ── Wave AS — Feature Flag & Experimentation Governance ────────────
        services.AddTransient<IValidator<IngestFeatureFlagState.Command>, IngestFeatureFlagState.Validator>();
        services.AddTransient<IValidator<GetFeatureFlagInventoryReport.Query>, GetFeatureFlagInventoryReport.Validator>();
        services.AddTransient<IValidator<GetFeatureFlagRiskReport.Query>, GetFeatureFlagRiskReport.Validator>();
        services.AddTransient<IValidator<GetExperimentGovernanceReport.Query>, GetExperimentGovernanceReport.Validator>();

        services.AddSingleton<IFeatureFlagRepository, NullFeatureFlagRepository>();
        services.AddSingleton<IFeatureFlagRiskReader, NullFeatureFlagRiskReader>();
        services.AddSingleton<IExperimentGovernanceReader, NullExperimentGovernanceReader>();

        // ── Wave AX — Security Posture & Vulnerability Intelligence ──────────
        services.AddTransient<IValidator<GetVulnerabilityExposureReport.Query>, GetVulnerabilityExposureReport.Validator>();
        services.AddTransient<IValidator<GetSecurityPatchComplianceReport.Query>, GetSecurityPatchComplianceReport.Validator>();
        services.AddSingleton<IVulnerabilityExposureReader, NullVulnerabilityExposureReader>();
        services.AddSingleton<ISecurityPatchComplianceReader, NullSecurityPatchComplianceReader>();

        // ── Wave AY — Organizational Knowledge & Documentation Intelligence ─
        services.AddTransient<IValidator<GetDocumentationHealthReport.Query>, GetDocumentationHealthReport.Validator>();
        services.AddSingleton<IDocumentationHealthReader, NullDocumentationHealthReader>();

        return services;
    }
}

