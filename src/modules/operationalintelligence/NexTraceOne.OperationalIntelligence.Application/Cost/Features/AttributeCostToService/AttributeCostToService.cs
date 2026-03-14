using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Application.Features.AttributeCostToService;

/// <summary>
/// Feature: AttributeCostToService — atribui custos de infraestrutura a um serviço/API.
/// Cria uma atribuição de custo para um período específico, calculando automaticamente
/// o custo por requisição a partir do volume de tráfego informado.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AttributeCostToService
{
    /// <summary>Comando para atribuir custo a um serviço/API num período.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ServiceName,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal TotalCost,
        long RequestCount,
        string Environment) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atribuição de custo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).NotEmpty()
                .GreaterThan(x => x.PeriodStart)
                .WithMessage("Period end must be after period start.");
            RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.RequestCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma atribuição de custo para um serviço/API.
    /// Delega a criação ao factory method do domínio que valida período e calcula custo por requisição.
    /// </summary>
    public sealed class Handler(
        ICostAttributionRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var attributionResult = CostAttribution.Attribute(
                request.ApiAssetId,
                request.ServiceName,
                request.PeriodStart,
                request.PeriodEnd,
                request.TotalCost,
                request.RequestCount,
                request.Environment);

            if (attributionResult.IsFailure)
                return attributionResult.Error;

            var attribution = attributionResult.Value;

            repository.Add(attribution);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                attribution.Id.Value,
                attribution.ApiAssetId,
                attribution.ServiceName,
                attribution.PeriodStart,
                attribution.PeriodEnd,
                attribution.TotalCost,
                attribution.RequestCount,
                attribution.CostPerRequest,
                attribution.Environment);
        }
    }

    /// <summary>Resposta da atribuição de custo com dados calculados.</summary>
    public sealed record Response(
        Guid AttributionId,
        Guid ApiAssetId,
        string ServiceName,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        decimal TotalCost,
        long RequestCount,
        decimal CostPerRequest,
        string Environment);
}
