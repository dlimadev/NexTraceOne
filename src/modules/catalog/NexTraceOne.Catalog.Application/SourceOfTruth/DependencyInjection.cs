using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetContractSourceOfTruth;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceCoverage;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GetServiceSourceOfTruth;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.GlobalSearch;
using NexTraceOne.Catalog.Application.SourceOfTruth.Features.SearchSourceOfTruth;

namespace NexTraceOne.Catalog.Application.SourceOfTruth;

/// <summary>
/// Registra serviços da camada Application do módulo Source of Truth.
/// Inclui: MediatR handlers, FluentValidation validators para todas as features
/// de pesquisa, visão consolidada e cobertura do Source of Truth.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona os serviços de aplicação do Source of Truth ao contentor de DI.</summary>
    public static IServiceCollection AddSourceOfTruthApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Source of Truth — visões consolidadas ────────────────────
        services.AddTransient<IValidator<GetServiceSourceOfTruth.Query>, GetServiceSourceOfTruth.Validator>();
        services.AddTransient<IValidator<GetContractSourceOfTruth.Query>, GetContractSourceOfTruth.Validator>();
        services.AddTransient<IValidator<GetServiceCoverage.Query>, GetServiceCoverage.Validator>();

        // ── Source of Truth — pesquisa ───────────────────────────────
        services.AddTransient<IValidator<SearchSourceOfTruth.Query>, SearchSourceOfTruth.Validator>();
        services.AddTransient<IValidator<GlobalSearch.Query>, GlobalSearch.Validator>();

        return services;
    }
}
