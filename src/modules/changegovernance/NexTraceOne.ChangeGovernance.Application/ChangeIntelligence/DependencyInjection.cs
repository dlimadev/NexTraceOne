using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AttachWorkItemContext;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CalculateBlastRadius;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ClassifyChangeLevel;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ComputeChangeScore;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetBlastRadiusReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeAdvisory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeDecisionHistory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeScore;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPostReleaseReview;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseHistory;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseImpactReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTraceCorrelations;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestCommit;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestExternalRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AddWorkItemToRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListApprovalRequests;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListCommitsByRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListWorkItemsByRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RemoveWorkItemFromRelease;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RequestExternalApproval;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RespondToApprovalRequest;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CreateApprovalPolicy;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListApprovalPolicies;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.DeleteApprovalPolicy;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListReleases;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordChangeDecision;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordConfidenceEvent;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeConfidenceTimeline;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetRiskScoreTrend;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluateReleaseTrain;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordObservationMetrics;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordTraceCorrelation;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RegisterRollback;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.SyncJiraWorkItems;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.UpdateDeploymentState;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetDeploymentRiskForecastReport;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Services;
using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetConfigurationDriftReport;
using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetPlatformHealthIndexReport;
using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetAdaptiveRecommendationReport;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence;

/// <summary>
/// Registra serviços da camada Application do módulo ChangeIntelligence.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo ChangeIntelligence ao contêiner de DI.</summary>
    public static IServiceCollection AddChangeIntelligenceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Serviço de cálculo automático de score (P5.3)
        services.AddSingleton<IChangeScoreCalculator, ChangeScoreCalculator>();

        // Serviço de verificação pós-mudança (P5.5)
        services.AddSingleton<IPostChangeVerificationService, PostChangeVerificationService>();

        services.AddTransient<IValidator<NotifyDeployment.Command>, NotifyDeployment.Validator>();
        services.AddTransient<IValidator<ClassifyChangeLevel.Command>, ClassifyChangeLevel.Validator>();
        services.AddTransient<IValidator<CalculateBlastRadius.Command>, CalculateBlastRadius.Validator>();
        services.AddTransient<IValidator<ComputeChangeScore.Command>, ComputeChangeScore.Validator>();
        services.AddTransient<IValidator<GetRelease.Query>, GetRelease.Validator>();
        services.AddTransient<IValidator<GetReleaseHistory.Query>, GetReleaseHistory.Validator>();
        services.AddTransient<IValidator<ListReleases.Query>, ListReleases.Validator>();
        services.AddTransient<IValidator<UpdateDeploymentState.Command>, UpdateDeploymentState.Validator>();
        services.AddTransient<IValidator<RegisterRollback.Command>, RegisterRollback.Validator>();
        services.AddTransient<IValidator<GetBlastRadiusReport.Query>, GetBlastRadiusReport.Validator>();
        services.AddTransient<IValidator<GetChangeScore.Query>, GetChangeScore.Validator>();
        services.AddTransient<IValidator<AttachWorkItemContext.Command>, AttachWorkItemContext.Validator>();
        services.AddTransient<IValidator<SyncJiraWorkItems.Command>, SyncJiraWorkItems.Validator>();
        services.AddTransient<IValidator<GetChangeAdvisory.Query>, GetChangeAdvisory.Validator>();
        services.AddTransient<IValidator<RecordChangeDecision.Command>, RecordChangeDecision.Validator>();
        services.AddTransient<IValidator<GetChangeDecisionHistory.Query>, GetChangeDecisionHistory.Validator>();
        services.AddTransient<IValidator<RecordTraceCorrelation.Command>, RecordTraceCorrelation.Validator>();
        services.AddTransient<IValidator<GetTraceCorrelations.Query>, GetTraceCorrelations.Validator>();
        services.AddTransient<IValidator<RecordObservationMetrics.Command>, RecordObservationMetrics.Validator>();
        services.AddTransient<IValidator<GetPostReleaseReview.Query>, GetPostReleaseReview.Validator>();
        services.AddTransient<IValidator<RecordConfidenceEvent.Command>, RecordConfidenceEvent.Validator>();
        services.AddTransient<IValidator<GetChangeConfidenceTimeline.Query>, GetChangeConfidenceTimeline.Validator>();
        services.AddTransient<IValidator<GetRiskScoreTrend.Query>, GetRiskScoreTrend.Validator>();
        services.AddTransient<IValidator<EvaluateReleaseTrain.Command>, EvaluateReleaseTrain.Validator>();

        // Phase 2: Commit Pool & Work Item Association
        services.AddTransient<IValidator<IngestCommit.Command>, IngestCommit.Validator>();
        services.AddTransient<IValidator<ListCommitsByRelease.Query>, ListCommitsByRelease.Validator>();
        services.AddTransient<IValidator<AddWorkItemToRelease.Command>, AddWorkItemToRelease.Validator>();
        services.AddTransient<IValidator<RemoveWorkItemFromRelease.Command>, RemoveWorkItemFromRelease.Validator>();
        services.AddTransient<IValidator<ListWorkItemsByRelease.Query>, ListWorkItemsByRelease.Validator>();

        // Phase 3: External Approval Gateway
        services.AddTransient<IValidator<RequestExternalApproval.Command>, RequestExternalApproval.Validator>();
        services.AddTransient<IValidator<RespondToApprovalRequest.Command>, RespondToApprovalRequest.Validator>();
        services.AddTransient<IValidator<ListApprovalRequests.Query>, ListApprovalRequests.Validator>();
        services.AddTransient<IValidator<CreateApprovalPolicy.Command>, CreateApprovalPolicy.Validator>();
        services.AddTransient<IValidator<ListApprovalPolicies.Query>, ListApprovalPolicies.Validator>();
        services.AddTransient<IValidator<DeleteApprovalPolicy.Command>, DeleteApprovalPolicy.Validator>();

        // Phase 4: Ingest Release from External System
        services.AddTransient<IValidator<IngestExternalRelease.Command>, IngestExternalRelease.Validator>();

        // Phase 5: Impact Report
        services.AddTransient<IValidator<GetReleaseImpactReport.Query>, GetReleaseImpactReport.Validator>();

        // Wave AI.1 — Deployment Risk Forecast Report
        services.AddTransient<IValidator<GetDeploymentRiskForecastReport.Query>, GetDeploymentRiskForecastReport.Validator>();

        // Wave AU — Platform Self-Optimization & Adaptive Intelligence
        services.AddTransient<IValidator<GetConfigurationDriftReport.Query>, GetConfigurationDriftReport.Validator>();
        services.AddTransient<IValidator<GetPlatformHealthIndexReport.Query>, GetPlatformHealthIndexReport.Validator>();
        services.AddTransient<IValidator<GetAdaptiveRecommendationReport.Query>, GetAdaptiveRecommendationReport.Validator>();

        return services;
    }
}


