using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.ImportFromKongGateway;

/// <summary>
/// Feature: ImportFromKongGateway — importa serviços e rotas do Kong Gateway para o grafo.
/// Aceita um payload representando o catálogo de serviços/rotas exportado do Kong Admin API.
/// Cada serviço é registado ou atualizado no grafo, e suas rotas são mapeadas como APIs.
/// Utiliza o padrão de batch import com resultados individuais por item (não-bloqueante).
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportFromKongGateway
{
    /// <summary>
    /// Comando de importação de dados do Kong Gateway.
    /// Recebe a lista de serviços com suas rotas para registo no grafo de engenharia.
    /// O campo CorrelationId permite rastreamento end-to-end da importação.
    /// </summary>
    public sealed record Command(
        IReadOnlyList<KongServiceItem> Services,
        string GatewayInstanceId,
        string? CorrelationId) : ICommand<Response>;

    /// <summary>
    /// Item individual representando um serviço Kong com suas rotas associadas.
    /// O campo KongServiceId é o identificador do serviço no Kong Admin API.
    /// </summary>
    public sealed record KongServiceItem(
        string KongServiceId,
        string ServiceName,
        string Host,
        int Port,
        string Protocol,
        string Domain,
        string TeamName,
        IReadOnlyList<KongRouteItem> Routes);

    /// <summary>
    /// Rota associada a um serviço Kong, mapeada como API no grafo de engenharia.
    /// Cada rota representa um endpoint exposto pelo serviço.
    /// </summary>
    public sealed record KongRouteItem(
        string KongRouteId,
        string Name,
        string Path,
        string Version,
        string Visibility);

    /// <summary>Valida o comando de importação do Kong Gateway.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Services).NotNull().NotEmpty()
                .WithMessage("At least one Kong service is required.");
            RuleFor(x => x.Services.Count).LessThanOrEqualTo(50)
                .WithMessage("Maximum of 50 services per import batch.");
            RuleFor(x => x.GatewayInstanceId).NotEmpty().MaximumLength(200);
            RuleForEach(x => x.Services).ChildRules(svc =>
            {
                svc.RuleFor(s => s.KongServiceId).NotEmpty().MaximumLength(200);
                svc.RuleFor(s => s.ServiceName).NotEmpty().MaximumLength(200);
                svc.RuleFor(s => s.Host).NotEmpty().MaximumLength(500);
                svc.RuleFor(s => s.Port).InclusiveBetween(1, 65535);
                svc.RuleFor(s => s.Protocol).NotEmpty().MaximumLength(20);
                svc.RuleFor(s => s.Domain).NotEmpty().MaximumLength(200);
                svc.RuleFor(s => s.TeamName).NotEmpty().MaximumLength(200);
                svc.RuleForEach(s => s.Routes).ChildRules(route =>
                {
                    route.RuleFor(r => r.KongRouteId).NotEmpty().MaximumLength(200);
                    route.RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
                    route.RuleFor(r => r.Path).NotEmpty().MaximumLength(500);
                    route.RuleFor(r => r.Version).NotEmpty().MaximumLength(50);
                    route.RuleFor(r => r.Visibility).NotEmpty().MaximumLength(50);
                });
            });
        }
    }

    /// <summary>
    /// Handler que processa a importação de serviços e rotas do Kong Gateway.
    /// Para cada serviço Kong, cria ou reutiliza o ServiceAsset correspondente no grafo,
    /// e regista cada rota como um ApiAsset com fonte de descoberta "KongGateway".
    /// Itens com erros são registados individualmente sem bloquear o lote.
    /// </summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository,
        IDateTimeProvider dateTimeProvider,
        ICatalogGraphUnitOfWork unitOfWork,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = new List<ImportItemResult>();
            var now = dateTimeProvider.UtcNow;
            var servicesCreated = 0;
            var apisCreated = 0;
            var apisSkipped = 0;
            var failed = 0;

            foreach (var svc in request.Services)
            {
                // Localiza ou cria o serviço proprietário no grafo
                var existingService = await serviceAssetRepository.GetByNameAsync(svc.ServiceName, cancellationToken);
                if (existingService is null)
                {
                    existingService = ServiceAsset.Create(svc.ServiceName, svc.Domain, svc.TeamName, currentTenant.Id);
                    serviceAssetRepository.Add(existingService);
                    servicesCreated++;
                }

                foreach (var route in svc.Routes)
                {
                    var existingApi = await apiAssetRepository.GetByNameAndOwnerAsync(
                        route.Name, existingService.Id, cancellationToken);

                    if (existingApi is not null)
                    {
                        // API já existe — registar como skipped
                        apisSkipped++;
                        results.Add(new ImportItemResult(
                            svc.ServiceName,
                            route.Name,
                            ImportOutcome.Skipped,
                            "CatalogGraph.ApiAsset.AlreadyExists"));
                        continue;
                    }

                    var apiAsset = ApiAsset.Register(
                        route.Name,
                        route.Path,
                        route.Version,
                        route.Visibility,
                        existingService);

                    var discoverySource = DiscoverySource.Create(
                        "KongGateway",
                        $"kong://{request.GatewayInstanceId}/services/{svc.KongServiceId}/routes/{route.KongRouteId}",
                        now,
                        0.95m);

                    var addResult = apiAsset.AddDiscoverySource(discoverySource);
                    if (addResult.IsFailure)
                    {
                        failed++;
                        results.Add(new ImportItemResult(
                            svc.ServiceName,
                            route.Name,
                            ImportOutcome.Failed,
                            addResult.Error.Code));
                        continue;
                    }

                    apiAssetRepository.Add(apiAsset);
                    apisCreated++;
                    results.Add(new ImportItemResult(
                        svc.ServiceName,
                        route.Name,
                        ImportOutcome.Created,
                        null));
                }
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                results,
                servicesCreated,
                apisCreated,
                apisSkipped,
                failed,
                request.Services.Count,
                request.CorrelationId);
        }
    }

    /// <summary>Resposta da importação com contadores e resultados por item.</summary>
    public sealed record Response(
        IReadOnlyList<ImportItemResult> Results,
        int ServicesCreated,
        int ApisCreated,
        int ApisSkipped,
        int Failed,
        int TotalServicesProcessed,
        string? CorrelationId);

    /// <summary>Resultado individual de cada item importado.</summary>
    public sealed record ImportItemResult(
        string ServiceName,
        string ApiName,
        ImportOutcome Outcome,
        string? ErrorCode);

    /// <summary>Resultado possível de cada item na importação.</summary>
    public enum ImportOutcome
    {
        /// <summary>API criada com sucesso no grafo.</summary>
        Created,
        /// <summary>API já existia — ignorada (idempotência).</summary>
        Skipped,
        /// <summary>Falha ao processar o item.</summary>
        Failed
    }
}
