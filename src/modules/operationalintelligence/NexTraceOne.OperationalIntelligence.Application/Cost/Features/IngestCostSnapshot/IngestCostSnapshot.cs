using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Entities;

namespace NexTraceOne.CostIntelligence.Application.Features.IngestCostSnapshot;

/// <summary>
/// Feature: IngestCostSnapshot — recebe dados de custo de infraestrutura e cria um snapshot.
/// Permite captura periódica de custos por serviço/ambiente para análise histórica,
/// detecção de anomalias e correlação com releases.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class IngestCostSnapshot
{
    /// <summary>Comando para ingestão de um novo snapshot de custo de infraestrutura.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        decimal TotalCost,
        decimal CpuCostShare,
        decimal MemoryCostShare,
        decimal NetworkCostShare,
        decimal StorageCostShare,
        DateTimeOffset CapturedAt,
        string Source,
        string Period,
        string Currency = "USD") : ICommand<Response>;

    /// <summary>Valida a entrada do comando de ingestão de snapshot de custo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CpuCostShare).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MemoryCostShare).GreaterThanOrEqualTo(0);
            RuleFor(x => x.NetworkCostShare).GreaterThanOrEqualTo(0);
            RuleFor(x => x.StorageCostShare).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CapturedAt).NotEmpty();
            RuleFor(x => x.Source).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        }
    }

    /// <summary>
    /// Handler que cria e persiste um snapshot de custo de infraestrutura.
    /// Valida que a soma das parcelas (CPU, memória, rede, storage) não excede o custo total.
    /// </summary>
    public sealed class Handler(
        ICostSnapshotRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshot = CostSnapshot.Create(
                request.ServiceName,
                request.Environment,
                request.TotalCost,
                request.CpuCostShare,
                request.MemoryCostShare,
                request.NetworkCostShare,
                request.StorageCostShare,
                request.CapturedAt,
                request.Source,
                request.Period,
                request.Currency);

            var validation = snapshot.Validate();
            if (validation.IsFailure)
                return validation.Error;

            repository.Add(snapshot);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                snapshot.Id.Value,
                snapshot.ServiceName,
                snapshot.Environment,
                snapshot.TotalCost,
                snapshot.Currency,
                snapshot.CapturedAt);
        }
    }

    /// <summary>Resposta da ingestão do snapshot de custo com identificador e totais.</summary>
    public sealed record Response(
        Guid SnapshotId,
        string ServiceName,
        string Environment,
        decimal TotalCost,
        string Currency,
        DateTimeOffset CapturedAt);
}
