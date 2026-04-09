using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListServicesByMaturityLevel;

/// <summary>
/// Feature: ListServicesByMaturityLevel — lista serviços com as suas avaliações de maturidade.
/// Suporta filtro opcional por nível de maturidade.
///
/// Owner: módulo Governance.
/// Pilar: Service Governance — visão panorâmica de maturidade dos serviços.
/// </summary>
public static class ListServicesByMaturityLevel
{
    /// <summary>Query para listar serviços com avaliação de maturidade, opcionalmente filtrados por nível.</summary>
    public sealed record Query(ServiceMaturityLevel? Level = null) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Level)
                .IsInEnum()
                .When(x => x.Level.HasValue);
        }
    }

    /// <summary>Handler que lista avaliações de maturidade com filtro opcional por nível.</summary>
    public sealed class Handler(
        IServiceMaturityAssessmentRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var assessments = await repository.ListAsync(request.Level, cancellationToken);

            var items = assessments
                .Select(a => new ServiceMaturityItemDto(
                    AssessmentId: a.Id.Value,
                    ServiceId: a.ServiceId,
                    ServiceName: a.ServiceName,
                    CurrentLevel: a.CurrentLevel,
                    AssessedAt: a.AssessedAt,
                    AssessedBy: a.AssessedBy,
                    LastReassessedAt: a.LastReassessedAt,
                    ReassessmentCount: a.ReassessmentCount))
                .ToList();

            return Result<Response>.Success(new Response(
                Items: items,
                TotalCount: items.Count,
                FilteredLevel: request.Level));
        }
    }

    /// <summary>Resposta com a lista de serviços e respectivos níveis de maturidade.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceMaturityItemDto> Items,
        int TotalCount,
        ServiceMaturityLevel? FilteredLevel);

    /// <summary>DTO resumido de uma avaliação de maturidade para listagem.</summary>
    public sealed record ServiceMaturityItemDto(
        Guid AssessmentId,
        Guid ServiceId,
        string ServiceName,
        ServiceMaturityLevel CurrentLevel,
        DateTimeOffset AssessedAt,
        string AssessedBy,
        DateTimeOffset? LastReassessedAt,
        int ReassessmentCount);
}
