using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.ImportFromBackstage;

/// <summary>
/// Feature: ImportFromBackstage — importa entidades do Backstage.io para o grafo.
/// Recebe o catálogo de componentes e APIs exportados do Backstage Catalog API.
/// Cada componente do tipo "service" é registado como ServiceAsset,
/// e cada API entity do Backstage é mapeada como ApiAsset com fonte de descoberta "Backstage".
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportFromBackstage
{
    /// <summary>
    /// Comando de importação de dados do Backstage.io.
    /// Recebe entidades do catálogo Backstage para registo no grafo de engenharia.
    /// </summary>
    public sealed record Command(
        IReadOnlyList<BackstageEntityItem> Entities,
        string BackstageInstanceUrl,
        string? CorrelationId) : ICommand<Response>;

    /// <summary>
    /// Item individual representando uma entidade do Backstage catalog.
    /// Segue a convenção Backstage de kind/namespace/name para identificação única.
    /// O campo Kind diferencia entre "Component" (serviço) e "API" (api asset).
    /// </summary>
    public sealed record BackstageEntityItem(
        string Kind,
        string Name,
        string Namespace,
        string Lifecycle,
        string Owner,
        string Domain,
        string? Description,
        BackstageApiSpec? ApiSpec);

    /// <summary>
    /// Especificação da API do Backstage, presente apenas quando Kind é "API".
    /// Contém a rota, versão e visibilidade que serão mapeadas para o ApiAsset.
    /// </summary>
    public sealed record BackstageApiSpec(
        string RoutePattern,
        string Version,
        string Visibility,
        string OwnerServiceName);

    /// <summary>Valida o comando de importação do Backstage.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] SupportedKinds = ["Component", "API"];

        public Validator()
        {
            RuleFor(x => x.Entities).NotNull().NotEmpty()
                .WithMessage("At least one Backstage entity is required.");
            RuleFor(x => x.Entities.Count).LessThanOrEqualTo(100)
                .WithMessage("Maximum of 100 entities per import batch.");
            RuleFor(x => x.BackstageInstanceUrl).NotEmpty().MaximumLength(500);
            RuleForEach(x => x.Entities).ChildRules(entity =>
            {
                entity.RuleFor(e => e.Kind).NotEmpty()
                    .Must(k => SupportedKinds.Contains(k))
                    .WithMessage($"Kind must be one of: {string.Join(", ", SupportedKinds)}");
                entity.RuleFor(e => e.Name).NotEmpty().MaximumLength(200);
                entity.RuleFor(e => e.Namespace).NotEmpty().MaximumLength(200);
                entity.RuleFor(e => e.Lifecycle).NotEmpty().MaximumLength(50);
                entity.RuleFor(e => e.Owner).NotEmpty().MaximumLength(200);
                entity.RuleFor(e => e.Domain).NotEmpty().MaximumLength(200);

                // ApiSpec obrigatório quando Kind é "API"
                entity.RuleFor(e => e.ApiSpec)
                    .NotNull()
                    .When(e => e.Kind == "API")
                    .WithMessage("ApiSpec is required for API entities.");
                entity.When(e => e.ApiSpec is not null, () =>
                {
                    entity.RuleFor(e => e.ApiSpec!.RoutePattern).NotEmpty().MaximumLength(500);
                    entity.RuleFor(e => e.ApiSpec!.Version).NotEmpty().MaximumLength(50);
                    entity.RuleFor(e => e.ApiSpec!.Visibility).NotEmpty().MaximumLength(50);
                    entity.RuleFor(e => e.ApiSpec!.OwnerServiceName).NotEmpty().MaximumLength(200);
                });
            });
        }
    }

    /// <summary>
    /// Handler que processa a importação de entidades do Backstage.
    /// Entidades do tipo "Component" são registadas como ServiceAsset.
    /// Entidades do tipo "API" são registadas como ApiAsset com fonte "Backstage".
    /// Itens com erros são registados individualmente sem bloquear o lote.
    /// </summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = new List<ImportItemResult>();
            var now = dateTimeProvider.UtcNow;
            var servicesCreated = 0;
            var apisCreated = 0;
            var skipped = 0;
            var failed = 0;

            // Primeira passagem: processar componentes (serviços)
            foreach (var entity in request.Entities.Where(e => e.Kind == "Component"))
            {
                var existing = await serviceAssetRepository.GetByNameAsync(entity.Name, cancellationToken);
                if (existing is not null)
                {
                    skipped++;
                    results.Add(new ImportItemResult(
                        entity.Kind, entity.Name, ImportOutcome.Skipped,
                        "EngineeringGraph.ServiceAsset.AlreadyExists"));
                    continue;
                }

                var service = ServiceAsset.Create(entity.Name, entity.Domain, entity.Owner);
                serviceAssetRepository.Add(service);
                servicesCreated++;
                results.Add(new ImportItemResult(
                    entity.Kind, entity.Name, ImportOutcome.Created, null));
            }

            // Commit dos serviços para que estejam disponíveis para lookup das APIs
            await unitOfWork.CommitAsync(cancellationToken);

            // Segunda passagem: processar APIs
            foreach (var entity in request.Entities.Where(e => e.Kind == "API"))
            {
                if (entity.ApiSpec is null)
                {
                    failed++;
                    results.Add(new ImportItemResult(
                        entity.Kind, entity.Name, ImportOutcome.Failed,
                        "EngineeringGraph.Backstage.ApiSpecMissing"));
                    continue;
                }

                var ownerService = await serviceAssetRepository.GetByNameAsync(
                    entity.ApiSpec.OwnerServiceName, cancellationToken);

                if (ownerService is null)
                {
                    // Cria serviço implicitamente caso não exista
                    ownerService = ServiceAsset.Create(entity.ApiSpec.OwnerServiceName, entity.Domain, entity.Owner);
                    serviceAssetRepository.Add(ownerService);
                    servicesCreated++;
                }

                var existingApi = await apiAssetRepository.GetByNameAndOwnerAsync(
                    entity.Name, ownerService.Id, cancellationToken);

                if (existingApi is not null)
                {
                    skipped++;
                    results.Add(new ImportItemResult(
                        entity.Kind, entity.Name, ImportOutcome.Skipped,
                        "EngineeringGraph.ApiAsset.AlreadyExists"));
                    continue;
                }

                var apiAsset = ApiAsset.Register(
                    entity.Name,
                    entity.ApiSpec.RoutePattern,
                    entity.ApiSpec.Version,
                    entity.ApiSpec.Visibility,
                    ownerService);

                var backstageRef = $"backstage://{request.BackstageInstanceUrl}/{entity.Namespace}/{entity.Name}";
                var discoverySource = DiscoverySource.Create("Backstage", backstageRef, now, 0.90m);
                var addResult = apiAsset.AddDiscoverySource(discoverySource);

                if (addResult.IsFailure)
                {
                    failed++;
                    results.Add(new ImportItemResult(
                        entity.Kind, entity.Name, ImportOutcome.Failed, addResult.Error.Code));
                    continue;
                }

                apiAssetRepository.Add(apiAsset);
                apisCreated++;
                results.Add(new ImportItemResult(
                    entity.Kind, entity.Name, ImportOutcome.Created, null));
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                results,
                servicesCreated,
                apisCreated,
                skipped,
                failed,
                request.Entities.Count,
                request.CorrelationId);
        }
    }

    /// <summary>Resposta da importação com contadores e resultados por item.</summary>
    public sealed record Response(
        IReadOnlyList<ImportItemResult> Results,
        int ServicesCreated,
        int ApisCreated,
        int Skipped,
        int Failed,
        int TotalEntitiesProcessed,
        string? CorrelationId);

    /// <summary>Resultado individual de cada item importado.</summary>
    public sealed record ImportItemResult(
        string Kind,
        string Name,
        ImportOutcome Outcome,
        string? ErrorCode);

    /// <summary>Resultado possível de cada item na importação.</summary>
    public enum ImportOutcome
    {
        /// <summary>Entidade criada com sucesso no grafo.</summary>
        Created,
        /// <summary>Entidade já existia — ignorada (idempotência).</summary>
        Skipped,
        /// <summary>Falha ao processar o item.</summary>
        Failed
    }
}
