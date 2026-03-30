using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.RunServiceDiscovery;

/// <summary>
/// Feature: RunServiceDiscovery — executa discovery automático a partir de telemetria.
/// Consulta o IServiceDiscoveryProvider para obter service.names observados,
/// compara com serviços já descobertos e cria novos registos para os desconhecidos.
/// Aplica regras de matching automático quando existem.
/// </summary>
public static class RunServiceDiscovery
{
    /// <summary>Comando para executar discovery num ambiente e janela temporal.</summary>
    public sealed record Command(
        string Environment,
        DateTimeOffset From,
        DateTimeOffset Until) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de discovery.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.From).LessThan(x => x.Until);
        }
    }

    /// <summary>Handler que executa o ciclo de discovery.</summary>
    public sealed class Handler(
        IServiceDiscoveryProvider discoveryProvider,
        IDiscoveredServiceRepository discoveredServiceRepository,
        IDiscoveryRunRepository discoveryRunRepository,
        IDiscoveryMatchRuleRepository matchRuleRepository,
        IServiceAssetRepository serviceAssetRepository,
        IDateTimeProvider dateTimeProvider,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var run = DiscoveryRun.Start("OpenTelemetry", request.Environment, dateTimeProvider.UtcNow);
            discoveryRunRepository.Add(run);

            try
            {
                var discovered = await discoveryProvider.DiscoverServicesAsync(
                    request.Environment,
                    request.From,
                    request.Until,
                    cancellationToken);

                var matchRules = await matchRuleRepository.ListActiveAsync(cancellationToken);
                var newCount = 0;

                foreach (var info in discovered)
                {
                    var existing = await discoveredServiceRepository
                        .GetByNameAndEnvironmentAsync(info.ServiceName, request.Environment, cancellationToken);

                    if (existing is not null)
                    {
                        existing.UpdateObservation(info.LastSeen, info.TraceCount, info.EndpointCount);
                    }
                    else
                    {
                        var newService = DiscoveredService.Create(
                            info.ServiceName,
                            info.ServiceNamespace,
                            request.Environment,
                            info.FirstSeen,
                            info.LastSeen,
                            info.TraceCount,
                            info.EndpointCount,
                            run.Id.Value);

                        // Tentar matching automático
                        await TryAutoMatch(newService, matchRules, serviceAssetRepository, cancellationToken);

                        discoveredServiceRepository.Add(newService);
                        newCount++;
                    }
                }

                run.Complete(dateTimeProvider.UtcNow, discovered.Count, newCount);
                await unitOfWork.CommitAsync(cancellationToken);

                return new Response(run.Id.Value, discovered.Count, newCount, 0, "Completed");
            }
            catch (Exception ex)
            {
                run.Fail(dateTimeProvider.UtcNow, ex.Message, 1);
                await unitOfWork.CommitAsync(cancellationToken);

                return new Response(run.Id.Value, 0, 0, 1, "Failed");
            }
        }

        private static async Task TryAutoMatch(
            DiscoveredService service,
            IReadOnlyList<DiscoveryMatchRule> rules,
            IServiceAssetRepository serviceAssetRepository,
            CancellationToken cancellationToken)
        {
            foreach (var rule in rules)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(service.ServiceName, rule.Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var target = await serviceAssetRepository.GetByIdAsync(
                        ServiceAssetId.From(rule.TargetServiceAssetId), cancellationToken);
                    if (target is not null)
                    {
                        service.MatchToService(rule.TargetServiceAssetId);
                        return;
                    }
                }
            }
        }
    }

    /// <summary>Resposta da execução de discovery.</summary>
    public sealed record Response(
        Guid RunId,
        int TotalServicesFound,
        int NewServicesFound,
        int ErrorCount,
        string Status);
}
