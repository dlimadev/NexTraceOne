using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CorrelateTraceToChange;

/// <summary>
/// Feature: CorrelateTraceToChange — correlaciona um trace distribuído com mudanças recentes
/// para apoiar a análise de causa raiz pós-deploy. Computação pura, sem persistência.
/// </summary>
public static class CorrelateTraceToChange
{
    public sealed record Query(
        string TraceId,
        string ServiceId,
        string Environment,
        DateTimeOffset TraceTimestamp) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TraceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Correlação pura: sem dados de mudança disponíveis em tempo de query.
            // A integração com o módulo ChangeGovernance consultaria mudanças numa janela
            // de ±2 horas em torno de TraceTimestamp.
            return Task.FromResult(Result<Response>.Success(new Response(
                request.TraceId,
                HasCorrelatedChanges: false,
                CorrelatedChangeId: null,
                ChangeType: null,
                CorrelationConfidence: 0m,
                request.TraceTimestamp,
                CorrelationReason: "No change data available for correlation window. Integrate with ChangeGovernance for full correlation.")));
        }
    }

    public sealed record Response(
        string TraceId,
        bool HasCorrelatedChanges,
        string? CorrelatedChangeId,
        string? ChangeType,
        decimal CorrelationConfidence,
        DateTimeOffset TraceTimestamp,
        string? CorrelationReason);
}
