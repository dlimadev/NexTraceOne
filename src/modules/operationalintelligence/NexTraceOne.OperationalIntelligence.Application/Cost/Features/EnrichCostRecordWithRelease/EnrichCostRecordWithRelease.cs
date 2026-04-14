using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.EnrichCostRecordWithRelease;

/// <summary>
/// Feature: EnrichCostRecordWithRelease — correlaciona registos de custo com uma release.
/// Enriquece todos os CostRecords de um serviço+ambiente+período com o ReleaseId informado,
/// permitindo ligar custos operacionais a mudanças de produção para análise de impacto financeiro.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EnrichCostRecordWithRelease
{
    /// <summary>Comando para correlacionar registos de custo com uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string ServiceId,
        string Environment,
        string Period) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
        }
    }

    public sealed class Handler(
        ICostRecordRepository repository,
        ICostIntelligenceUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var records = await repository.ListByServiceAsync(
                request.ServiceId, request.Period, cancellationToken);

            var filtered = records
                .Where(r => string.Equals(r.Environment, request.Environment, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count == 0)
                return CostIntelligenceErrors.RecordNotFound($"{request.ServiceId}/{request.Environment}/{request.Period}");

            var enrichedCount = 0;
            foreach (var record in filtered)
            {
                record.AssignRelease(request.ReleaseId);
                enrichedCount++;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                request.ReleaseId,
                request.ServiceId,
                request.Environment,
                request.Period,
                enrichedCount));
        }
    }

    public sealed record Response(
        Guid ReleaseId,
        string ServiceId,
        string Environment,
        string Period,
        int EnrichedRecordCount);
}
