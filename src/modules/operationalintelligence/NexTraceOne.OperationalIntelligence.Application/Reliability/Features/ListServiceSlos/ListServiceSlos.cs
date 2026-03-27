using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceSlos;

/// <summary>
/// Feature: ListServiceSlos — lista todas as definições de SLO registadas para um serviço.
/// Retorna todos os SLOs activos e inactivos ordenados por ambiente e nome.
/// </summary>
public static class ListServiceSlos
{
    public sealed record Query(string ServiceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository repository,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var slos = await repository.GetByServiceAsync(request.ServiceId, tenant.Id, cancellationToken);

            var items = slos.Select(s => new SloItem(
                s.Id.Value,
                s.Name,
                s.ServiceId,
                s.Environment,
                s.Type,
                s.TargetPercent,
                s.AlertThresholdPercent,
                s.WindowDays,
                s.IsActive)).ToList();

            return Result<Response>.Success(new Response(request.ServiceId, items));
        }
    }

    public sealed record SloItem(
        Guid Id,
        string Name,
        string ServiceId,
        string Environment,
        SloType Type,
        decimal TargetPercent,
        decimal? AlertThresholdPercent,
        int WindowDays,
        bool IsActive);

    public sealed record Response(string ServiceId, IReadOnlyList<SloItem> Items);
}
