using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.GetRelease;

/// <summary>
/// Feature: GetRelease — retorna os detalhes de uma Release pelo seu identificador.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetRelease
{
    /// <summary>Query de consulta de Release por identificador.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de consulta de Release.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna os detalhes de uma Release.</summary>
    public sealed class Handler(IReleaseRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await repository.GetByIdAsync(ReleaseId.From(request.ReleaseId), cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            return new Response(
                release.Id.Value,
                release.ApiAssetId,
                release.ServiceName,
                release.Version,
                release.Environment,
                release.Status.ToString(),
                release.ChangeLevel,
                release.ChangeScore,
                release.WorkItemReference,
                release.CreatedAt);
        }
    }

    /// <summary>Resposta com detalhes da Release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        Guid ApiAssetId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        ChangeLevel ChangeLevel,
        decimal ChangeScore,
        string? WorkItemReference,
        DateTimeOffset CreatedAt);
}
