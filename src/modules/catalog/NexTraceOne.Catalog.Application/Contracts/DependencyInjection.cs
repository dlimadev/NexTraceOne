using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Contracts.Application.Features.AddDraftExample;
using NexTraceOne.Contracts.Application.Features.ApproveDraft;
using NexTraceOne.Contracts.Application.Features.ClassifyBreakingChange;
using NexTraceOne.Contracts.Application.Features.ComputeSemanticDiff;
using NexTraceOne.Contracts.Application.Features.CreateContractVersion;
using NexTraceOne.Contracts.Application.Features.CreateDraft;
using NexTraceOne.Contracts.Application.Features.EvaluateContractRules;
using NexTraceOne.Contracts.Application.Features.ExportContract;
using NexTraceOne.Contracts.Application.Features.GenerateDraftFromAi;
using NexTraceOne.Contracts.Application.Features.GenerateEvidencePack;
using NexTraceOne.Contracts.Application.Features.GenerateScorecard;
using NexTraceOne.Contracts.Application.Features.GetCompatibilityAssessment;
using NexTraceOne.Contracts.Application.Features.GetContractHistory;
using NexTraceOne.Contracts.Application.Features.GetDraft;
using NexTraceOne.Contracts.Application.Features.ImportContract;
using NexTraceOne.Contracts.Application.Features.ListDraftReviews;
using NexTraceOne.Contracts.Application.Features.ListDrafts;
using NexTraceOne.Contracts.Application.Features.LockContractVersion;
using NexTraceOne.Contracts.Application.Features.PublishDraft;
using NexTraceOne.Contracts.Application.Features.RejectDraft;
using NexTraceOne.Contracts.Application.Features.SubmitDraftForReview;
using NexTraceOne.Contracts.Application.Features.SuggestSemanticVersion;
using NexTraceOne.Contracts.Application.Features.UpdateDraftContent;
using NexTraceOne.Contracts.Application.Features.UpdateDraftMetadata;
using NexTraceOne.Contracts.Application.Features.ValidateContractIntegrity;

namespace NexTraceOne.Contracts.Application;

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

        return services;
    }
}

