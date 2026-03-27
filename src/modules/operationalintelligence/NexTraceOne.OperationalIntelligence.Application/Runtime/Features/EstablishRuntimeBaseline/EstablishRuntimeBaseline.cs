using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.EstablishRuntimeBaseline;

/// <summary>
/// Feature: EstablishRuntimeBaseline — cria ou actualiza a baseline de métricas esperadas de um serviço.
/// A baseline serve como referência para a detecção de drift (DetectRuntimeDrift).
/// Se já existir uma baseline para o par (ServiceName, Environment), é actualizada via Refresh.
/// Se não existir, é criada via factory method do domínio.
///
/// Estratégia de upsert:
/// - Existe → Refresh (actualiza métricas e score de confiança).
/// - Não existe → Establish (cria nova baseline).
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EstablishRuntimeBaseline
{
    /// <summary>Comando para estabelecer ou actualizar a baseline de runtime de um serviço.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        decimal ExpectedAvgLatencyMs,
        decimal ExpectedP99LatencyMs,
        decimal ExpectedErrorRate,
        decimal ExpectedRequestsPerSecond,
        int DataPointCount,
        decimal ConfidenceScore) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExpectedAvgLatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ExpectedP99LatencyMs).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ExpectedErrorRate).InclusiveBetween(0m, 1m);
            RuleFor(x => x.ExpectedRequestsPerSecond).GreaterThanOrEqualTo(0);
            RuleFor(x => x.DataPointCount).GreaterThanOrEqualTo(1);
            RuleFor(x => x.ConfidenceScore).InclusiveBetween(0m, 1m);
        }
    }

    public sealed class Handler(
        IRuntimeBaselineRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;
            var existing = await repository.GetByServiceAndEnvironmentAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            bool isUpdate;

            if (existing is not null)
            {
                existing.Refresh(
                    request.ExpectedAvgLatencyMs,
                    request.ExpectedP99LatencyMs,
                    request.ExpectedErrorRate,
                    request.ExpectedRequestsPerSecond,
                    now,
                    request.DataPointCount,
                    request.ConfidenceScore);

                isUpdate = true;
            }
            else
            {
                var baseline = RuntimeBaseline.Establish(
                    request.ServiceName,
                    request.Environment,
                    request.ExpectedAvgLatencyMs,
                    request.ExpectedP99LatencyMs,
                    request.ExpectedErrorRate,
                    request.ExpectedRequestsPerSecond,
                    now,
                    request.DataPointCount,
                    request.ConfidenceScore);

                repository.Add(baseline);
                existing = baseline;
                isUpdate = false;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                existing.Id.Value,
                existing.ServiceName,
                existing.Environment,
                existing.ExpectedAvgLatencyMs,
                existing.ExpectedP99LatencyMs,
                existing.ExpectedErrorRate,
                existing.ExpectedRequestsPerSecond,
                existing.DataPointCount,
                existing.ConfidenceScore,
                existing.IsConfident,
                existing.EstablishedAt,
                isUpdate));
        }
    }

    public sealed record Response(
        Guid BaselineId,
        string ServiceName,
        string Environment,
        decimal ExpectedAvgLatencyMs,
        decimal ExpectedP99LatencyMs,
        decimal ExpectedErrorRate,
        decimal ExpectedRequestsPerSecond,
        int DataPointCount,
        decimal ConfidenceScore,
        bool IsConfident,
        DateTimeOffset EstablishedAt,
        bool IsUpdate);
}
