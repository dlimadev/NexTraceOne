using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.RulesetGovernance.Application.Features.ArchiveRuleset;
using NexTraceOne.RulesetGovernance.Application.Features.BindRulesetToAssetType;
using NexTraceOne.RulesetGovernance.Application.Features.ComputeRulesetScore;
using NexTraceOne.RulesetGovernance.Application.Features.ExecuteLintForRelease;
using NexTraceOne.RulesetGovernance.Application.Features.GetRulesetFindings;
using NexTraceOne.RulesetGovernance.Application.Features.GetRulesetScore;
using NexTraceOne.RulesetGovernance.Application.Features.InstallDefaultRulesets;
using NexTraceOne.RulesetGovernance.Application.Features.ListRulesets;
using NexTraceOne.RulesetGovernance.Application.Features.UploadRuleset;

namespace NexTraceOne.RulesetGovernance.Application;

/// <summary>
/// Registra serviços da camada Application do módulo RulesetGovernance.
/// Inclui: MediatR handlers, FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços da camada Application do módulo RulesetGovernance ao contêiner de DI.</summary>
    public static IServiceCollection AddRulesetGovernanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<UploadRuleset.Command>, UploadRuleset.Validator>();
        services.AddTransient<IValidator<ListRulesets.Query>, ListRulesets.Validator>();
        services.AddTransient<IValidator<ArchiveRuleset.Command>, ArchiveRuleset.Validator>();
        services.AddTransient<IValidator<BindRulesetToAssetType.Command>, BindRulesetToAssetType.Validator>();
        services.AddTransient<IValidator<ExecuteLintForRelease.Command>, ExecuteLintForRelease.Validator>();
        services.AddTransient<IValidator<GetRulesetFindings.Query>, GetRulesetFindings.Validator>();
        services.AddTransient<IValidator<GetRulesetScore.Query>, GetRulesetScore.Validator>();
        services.AddTransient<IValidator<InstallDefaultRulesets.Command>, InstallDefaultRulesets.Validator>();
        services.AddTransient<IValidator<ComputeRulesetScore.Command>, ComputeRulesetScore.Validator>();

        return services;
    }
}
