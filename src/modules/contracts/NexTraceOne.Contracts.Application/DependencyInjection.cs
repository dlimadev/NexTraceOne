using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Contracts.Application.Features.ClassifyBreakingChange;
using NexTraceOne.Contracts.Application.Features.ComputeSemanticDiff;
using NexTraceOne.Contracts.Application.Features.CreateContractVersion;
using NexTraceOne.Contracts.Application.Features.ExportContract;
using NexTraceOne.Contracts.Application.Features.GetContractHistory;
using NexTraceOne.Contracts.Application.Features.ImportContract;
using NexTraceOne.Contracts.Application.Features.LockContractVersion;
using NexTraceOne.Contracts.Application.Features.SuggestSemanticVersion;
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

        return services;
    }
}

