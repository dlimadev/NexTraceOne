using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.Contracts.Features.AddDraftExample;
using NexTraceOne.Catalog.Application.Contracts.Features.ApproveDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.ClassifyBreakingChange;
using NexTraceOne.Catalog.Application.Contracts.Features.ComputeSemanticDiff;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateContractVersion;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.CreateSoapDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractRules;
using NexTraceOne.Catalog.Application.Contracts.Features.ExportContract;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateEvidencePack;
using NexTraceOne.Catalog.Application.Contracts.Features.GenerateScorecard;
using NexTraceOne.Catalog.Application.Contracts.Features.GetCompatibilityAssessment;
using NexTraceOne.Catalog.Application.Contracts.Features.GetContractHistory;
using NexTraceOne.Catalog.Application.Contracts.Features.GetDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.GetSoapContractDetail;
using NexTraceOne.Catalog.Application.Contracts.Features.ImportContract;
using NexTraceOne.Catalog.Application.Contracts.Features.ImportWsdlContract;
using NexTraceOne.Catalog.Application.Contracts.Features.ListDraftReviews;
using NexTraceOne.Catalog.Application.Contracts.Features.ListDrafts;
using NexTraceOne.Catalog.Application.Contracts.Features.LockContractVersion;
using NexTraceOne.Catalog.Application.Contracts.Features.PublishDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.RejectDraft;
using NexTraceOne.Catalog.Application.Contracts.Features.SubmitDraftForReview;
using NexTraceOne.Catalog.Application.Contracts.Features.SuggestSemanticVersion;
using NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftContent;
using NexTraceOne.Catalog.Application.Contracts.Features.UpdateDraftMetadata;
using NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractIntegrity;

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

        return services;
    }
}

