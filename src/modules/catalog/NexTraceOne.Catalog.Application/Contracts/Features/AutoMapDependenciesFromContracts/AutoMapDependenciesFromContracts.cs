using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.AutoMapDependenciesFromContracts;

/// <summary>
/// Feature: AutoMapDependenciesFromContracts — analisa contratos publicados para
/// descobrir automaticamente dependências entre serviços com base em produtores
/// e consumidores de APIs. Examina o conteúdo das especificações à procura de
/// referências cruzadas ($ref) entre serviços, gerando um mapa de dependências
/// sugeridas com pontuação de confiança.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AutoMapDependenciesFromContracts
{
    /// <summary>Query para descoberta automática de dependências a partir de contratos.</summary>
    public sealed record Query(Guid? ServiceAssetId = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            When(x => x.ServiceAssetId.HasValue, () =>
            {
                RuleFor(x => x.ServiceAssetId!.Value).NotEmpty();
            });
        }
    }

    /// <summary>
    /// Handler que analisa contratos publicados e as suas relações de consumo
    /// para descobrir dependências implícitas entre serviços. Cruza informação
    /// de ApiAssets por serviço com referências $ref no conteúdo das especificações.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var services = request.ServiceAssetId.HasValue
                ? await GetSingleServiceAsync(request.ServiceAssetId.Value, cancellationToken)
                : await serviceAssetRepository.ListAllAsync(cancellationToken);

            if (services is null || services.Count == 0)
                return new Response(0, 0, []);

            var serviceMap = services.ToDictionary(s => s.Id, s => s);

            // Mapear ApiAssets por serviço e indexar por ApiAssetId
            var apiAssetsByService = new Dictionary<ServiceAssetId, IReadOnlyList<ApiAsset>>();
            var apiAssetToService = new Dictionary<Guid, ServiceAsset>();

            foreach (var service in services)
            {
                var apis = await apiAssetRepository.ListByServiceIdAsync(service.Id, cancellationToken);
                apiAssetsByService[service.Id] = apis;

                foreach (var api in apis)
                    apiAssetToService[api.Id.Value] = service;
            }

            var allApiAssetIds = apiAssetToService.Keys.ToList();
            if (allApiAssetIds.Count == 0)
                return new Response(0, 0, []);

            // Obter contratos mais recentes para todos os ApiAssets envolvidos
            var contracts = await contractVersionRepository.ListByApiAssetIdsAsync(
                allApiAssetIds, cancellationToken);

            var dependencies = new List<DiscoveredDependency>();
            var analyzedCount = 0;

            foreach (var contract in contracts)
            {
                analyzedCount++;

                if (!apiAssetToService.TryGetValue(contract.ApiAssetId, out var producerService))
                    continue;

                // Analisar referências $ref no conteúdo da especificação
                var refs = ExtractSchemaReferences(contract.SpecContent);

                foreach (var refPath in refs)
                {
                    // Procurar se algum outro serviço possui uma entidade/schema com este nome
                    foreach (var (serviceId, apis) in apiAssetsByService)
                    {
                        if (serviceId == producerService.Id)
                            continue;

                        var consumerService = serviceMap[serviceId];

                        foreach (var consumerApi in apis)
                        {
                            if (refPath.Contains(consumerApi.Name, StringComparison.OrdinalIgnoreCase)
                                || contract.SpecContent.Contains(consumerService.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                var alreadyMapped = dependencies.Any(d =>
                                    d.ProducerServiceId == producerService.Id.Value
                                    && d.ConsumerServiceName == consumerService.Name
                                    && d.ContractName == contract.SemVer);

                                if (!alreadyMapped)
                                {
                                    dependencies.Add(new DiscoveredDependency(
                                        ProducerServiceId: producerService.Id.Value,
                                        ProducerServiceName: producerService.Name,
                                        ConsumerServiceName: consumerService.Name,
                                        ContractName: contract.SemVer,
                                        ContractType: contract.Protocol.ToString(),
                                        ConfidenceScore: 0.7m,
                                        Reason: $"Cross-reference detected: spec contains reference to '{refPath}' matching service '{consumerService.Name}'"));
                                }
                            }
                        }
                    }
                }

                // Verificar referências textuais a nomes de outros serviços
                foreach (var (serviceId, _) in apiAssetsByService)
                {
                    if (serviceId == producerService.Id)
                        continue;

                    var otherService = serviceMap[serviceId];

                    if (contract.SpecContent.Contains(otherService.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var alreadyMapped = dependencies.Any(d =>
                            d.ProducerServiceId == producerService.Id.Value
                            && d.ConsumerServiceName == otherService.Name
                            && d.ContractName == contract.SemVer);

                        if (!alreadyMapped)
                        {
                            dependencies.Add(new DiscoveredDependency(
                                ProducerServiceId: producerService.Id.Value,
                                ProducerServiceName: producerService.Name,
                                ConsumerServiceName: otherService.Name,
                                ContractName: contract.SemVer,
                                ContractType: contract.Protocol.ToString(),
                                ConfidenceScore: 0.5m,
                                Reason: $"Service name '{otherService.Name}' found in contract spec content"));
                        }
                    }
                }
            }

            return new Response(
                analyzedCount,
                dependencies.Count,
                dependencies.AsReadOnly());
        }

        private async Task<IReadOnlyList<ServiceAsset>> GetSingleServiceAsync(
            Guid serviceAssetId, CancellationToken cancellationToken)
        {
            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(serviceAssetId), cancellationToken);

            return service is not null ? [service] : [];
        }

        /// <summary>Extrai referências $ref do conteúdo da especificação.</summary>
        private static List<string> ExtractSchemaReferences(string specContent)
        {
            var refs = new List<string>();
            if (string.IsNullOrWhiteSpace(specContent))
                return refs;

            var span = specContent.AsSpan();
            const string refMarker = "$ref";
            var searchStart = 0;

            while (searchStart < span.Length)
            {
                var idx = specContent.IndexOf(refMarker, searchStart, StringComparison.Ordinal);
                if (idx < 0)
                    break;

                // Avançar até o valor da referência (procurar aspas após $ref)
                var valueStart = specContent.IndexOf('"', idx + refMarker.Length);
                if (valueStart < 0)
                    break;

                valueStart++; // Avançar para depois da aspa de abertura
                var valueEnd = specContent.IndexOf('"', valueStart);
                if (valueEnd < 0)
                    break;

                var refValue = specContent[valueStart..valueEnd];
                if (!string.IsNullOrWhiteSpace(refValue))
                    refs.Add(refValue);

                searchStart = valueEnd + 1;
            }

            return refs;
        }
    }

    /// <summary>Dependência descoberta entre serviços a partir da análise de contratos.</summary>
    public sealed record DiscoveredDependency(
        Guid ProducerServiceId,
        string ProducerServiceName,
        string ConsumerServiceName,
        string ContractName,
        string ContractType,
        decimal ConfidenceScore,
        string Reason);

    /// <summary>Resposta com o mapa de dependências descobertas.</summary>
    public sealed record Response(
        int TotalAnalyzed,
        int DependenciesDiscovered,
        IReadOnlyList<DiscoveredDependency> Dependencies);
}
