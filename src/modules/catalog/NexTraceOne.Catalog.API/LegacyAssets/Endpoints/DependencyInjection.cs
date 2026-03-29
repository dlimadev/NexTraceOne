using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyAssetDetail;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.ListLegacyAssets;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCicsTransaction;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCobolProgram;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCopybook;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterDb2Artifact;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterImsTransaction;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterMainframeSystem;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterZosConnectBinding;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.SyncLegacyAssets;
using NexTraceOne.Catalog.Infrastructure.LegacyAssets;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Registra serviços específicos da camada API do sub-domínio Legacy Assets do módulo Catalog.
/// Compõe Infrastructure layer e validators das features de registo e consulta de ativos legacy.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogLegacyAssetsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Infrastructure: DbContext, UnitOfWork, Repositórios ───────────
        services.AddCatalogLegacyAssetsInfrastructure(configuration);

        // ── Validators de registo ────────────────────────────────────────
        services.AddTransient<IValidator<RegisterMainframeSystem.Command>, RegisterMainframeSystem.Validator>();
        services.AddTransient<IValidator<RegisterCobolProgram.Command>, RegisterCobolProgram.Validator>();
        services.AddTransient<IValidator<RegisterCopybook.Command>, RegisterCopybook.Validator>();
        services.AddTransient<IValidator<RegisterCicsTransaction.Command>, RegisterCicsTransaction.Validator>();
        services.AddTransient<IValidator<RegisterImsTransaction.Command>, RegisterImsTransaction.Validator>();
        services.AddTransient<IValidator<RegisterDb2Artifact.Command>, RegisterDb2Artifact.Validator>();
        services.AddTransient<IValidator<RegisterZosConnectBinding.Command>, RegisterZosConnectBinding.Validator>();

        // ── Validators de consulta ───────────────────────────────────────
        services.AddTransient<IValidator<ListLegacyAssets.Query>, ListLegacyAssets.Validator>();
        services.AddTransient<IValidator<GetLegacyAssetDetail.Query>, GetLegacyAssetDetail.Validator>();

        // ── Validator de ingestão bulk ───────────────────────────────────
        services.AddTransient<IValidator<SyncLegacyAssets.Command>, SyncLegacyAssets.Validator>();

        return services;
    }
}
