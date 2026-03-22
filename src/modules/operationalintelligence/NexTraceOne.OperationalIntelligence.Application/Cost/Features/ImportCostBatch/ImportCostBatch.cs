using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.ImportCostBatch;

/// <summary>
/// Feature: ImportCostBatch — recebe um lote de registos de custo reais e persiste-os no sistema.
/// Cria um CostImportBatch para rastreabilidade e múltiplos CostRecord para cada item do lote.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ImportCostBatch
{
    /// <summary>Registo individual de custo dentro do comando de importação.</summary>
    public sealed record CostRecordInput(
        string ServiceId,
        string ServiceName,
        string? Team,
        string? Domain,
        string? Environment,
        decimal TotalCost);

    /// <summary>Comando para importação de um lote de registos de custo.</summary>
    public sealed record Command(
        string Source,
        string Period,
        string Currency,
        IReadOnlyList<CostRecordInput> Records) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de importação de batch de custo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Source).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Records).NotEmpty();

            RuleForEach(x => x.Records).ChildRules(record =>
            {
                record.RuleFor(r => r.ServiceId).NotEmpty().MaximumLength(200);
                record.RuleFor(r => r.ServiceName).NotEmpty().MaximumLength(200);
                record.RuleFor(r => r.TotalCost).GreaterThanOrEqualTo(0);
            });
        }
    }

    /// <summary>
    /// Handler que cria e persiste um batch de importação de custo com todos os seus registos.
    /// Valida duplicação de batch por fonte/período antes de prosseguir.
    /// </summary>
    public sealed class Handler(
        ICostImportBatchRepository batchRepository,
        ICostRecordRepository recordRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (request.Records.Count == 0)
                return CostIntelligenceErrors.EmptyImportBatch();

            var duplicateExists = await batchRepository.ExistsBySourceAndPeriodAsync(
                request.Source, request.Period, cancellationToken);

            if (duplicateExists)
                return CostIntelligenceErrors.DuplicateImportBatch(request.Source, request.Period);

            var now = clock.UtcNow;

            var batchResult = CostImportBatch.Create(
                request.Source,
                request.Period,
                now,
                request.Currency);

            if (batchResult.IsFailure)
                return batchResult.Error;

            var batch = batchResult.Value;
            batchRepository.Add(batch);

            logger.LogInformation(
                "Creating cost import batch {BatchId} for source '{Source}', period '{Period}' with {RecordCount} records",
                batch.Id.Value, request.Source, request.Period, request.Records.Count);

            var costRecords = new List<CostRecord>(request.Records.Count);

            foreach (var input in request.Records)
            {
                var recordResult = CostRecord.Create(
                    batch.Id.Value,
                    input.ServiceId,
                    input.ServiceName,
                    input.Team,
                    input.Domain,
                    input.Environment,
                    request.Period,
                    input.TotalCost,
                    request.Currency,
                    request.Source,
                    now);

                if (recordResult.IsFailure)
                {
                    batch.Fail(recordResult.Error.Message);
                    await unitOfWork.CommitAsync(cancellationToken);

                    logger.LogWarning(
                        "Cost import batch {BatchId} failed: {Error}",
                        batch.Id.Value, recordResult.Error.Message);

                    return recordResult.Error;
                }

                costRecords.Add(recordResult.Value);
            }

            recordRepository.AddRange(costRecords);
            batch.Complete(costRecords.Count);

            await unitOfWork.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Cost import batch {BatchId} completed successfully with {RecordCount} records",
                batch.Id.Value, costRecords.Count);

            return new Response(
                batch.Id.Value,
                batch.Source,
                batch.Period,
                batch.Currency,
                batch.RecordCount,
                batch.Status,
                batch.ImportedAt);
        }
    }

    /// <summary>Resposta da importação do batch de custo com identificador e totais.</summary>
    public sealed record Response(
        Guid BatchId,
        string Source,
        string Period,
        string Currency,
        int RecordCount,
        string Status,
        DateTimeOffset ImportedAt);
}
