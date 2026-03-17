using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ArchiveRuleset;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.BindRulesetToAssetType;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ComputeRulesetScore;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ExecuteLintForRelease;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetFindings;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.GetRulesetScore;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.InstallDefaultRulesets;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ListRulesets;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.UploadRuleset;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance;

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
