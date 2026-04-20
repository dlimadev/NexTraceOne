using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.ConfigurationKeys;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.ExportToBackstage;

/// <summary>
/// Feature: ExportToBackstage — exporta os serviços do catálogo para o formato do Backstage.io.
/// Produz entidades Backstage do tipo Component para cada ServiceAsset registado,
/// com metadados de ownership, lifecycle, namespace e anotações NexTraceOne.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ExportToBackstage
{
    /// <summary>
    /// Query de exportação para o Backstage.
    /// Permite filtrar por namespace de destino, lifecycle e nome de equipa.
    /// </summary>
    public sealed record Query(
        string? Namespace,
        string? Lifecycle,
        string? TeamName) : IQuery<Response>;

    /// <summary>Valida a query de exportação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Namespace).MaximumLength(200).When(x => x.Namespace is not null);
            RuleFor(x => x.Lifecycle).MaximumLength(50).When(x => x.Lifecycle is not null);
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
        }
    }

    /// <summary>
    /// Handler que lista os ServiceAssets do catálogo e os mapeia para entidades Backstage.
    /// Aplica filtro opcional por equipa e lê a URL da instância Backstage da configuração.
    /// </summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var backstageUrlConfig = await configService.ResolveEffectiveValueAsync(
                BackstageBridgeConfigKeys.InstanceUrl,
                ConfigurationScope.System,
                null,
                cancellationToken);

            var backstageInstanceUrl = backstageUrlConfig?.EffectiveValue ?? string.Empty;

            IReadOnlyList<Domain.Graph.Entities.ServiceAsset> services;
            if (!string.IsNullOrWhiteSpace(request.TeamName))
                services = await serviceAssetRepository.ListByTeamAsync(request.TeamName, cancellationToken);
            else
                services = await serviceAssetRepository.ListAllAsync(cancellationToken);

            var targetNamespace = request.Namespace ?? "default";
            var targetLifecycle = request.Lifecycle ?? "production";

            var entities = services
                .Select(s => MapToBackstageEntity(s, targetNamespace, targetLifecycle, backstageInstanceUrl))
                .ToList();

            return new Response(
                entities,
                entities.Count,
                DateTimeOffset.UtcNow);
        }

        private static BackstageCatalogEntity MapToBackstageEntity(
            Domain.Graph.Entities.ServiceAsset service,
            string targetNamespace,
            string targetLifecycle,
            string backstageInstanceUrl)
        {
            var slug = Slugify(service.Name);

            var annotations = new Dictionary<string, string>
            {
                ["nextraceone.io/service-id"] = service.Id.Value.ToString(),
                ["nextraceone.io/source-url"] = backstageInstanceUrl
            };

            var owner = string.IsNullOrWhiteSpace(service.TeamName)
                ? "platform-team"
                : service.TeamName;

            return new BackstageCatalogEntity(
                "backstage.io/v1alpha1",
                "Component",
                new BackstageMetadata(
                    slug,
                    targetNamespace,
                    string.IsNullOrWhiteSpace(service.Description) ? string.Empty : service.Description,
                    [],
                    annotations),
                new BackstageSpec("service", targetLifecycle, owner));
        }

        private static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return name.ToLowerInvariant().Replace(' ', '-').Replace('_', '-');
        }
    }

    /// <summary>Resposta da exportação com a lista de entidades mapeadas e metadados.</summary>
    public sealed record Response(
        IReadOnlyList<BackstageCatalogEntity> Entities,
        int TotalExported,
        DateTimeOffset ExportedAt);
}

/// <summary>
/// Entidade do Backstage Catalog, compatível com o schema backstage.io/v1alpha1.
/// Representa um componente (serviço) no catálogo do Backstage.
/// </summary>
public sealed record BackstageCatalogEntity(
    string ApiVersion,
    string Kind,
    BackstageMetadata Metadata,
    BackstageSpec Spec);

/// <summary>Metadados da entidade Backstage, seguindo a convenção do Backstage Catalog API.</summary>
public sealed record BackstageMetadata(
    string Name,
    string Namespace,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Annotations);

/// <summary>Especificação da entidade Backstage, com type, lifecycle e owner.</summary>
public sealed record BackstageSpec(
    string Type,
    string Lifecycle,
    string Owner);
