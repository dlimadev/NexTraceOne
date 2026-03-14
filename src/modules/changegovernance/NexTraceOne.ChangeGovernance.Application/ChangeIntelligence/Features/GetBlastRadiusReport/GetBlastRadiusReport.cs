using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.GetBlastRadiusReport;

/// <summary>
/// Feature: GetBlastRadiusReport — retorna o relatório de blast radius de uma Release.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetBlastRadiusReport
{
    /// <summary>Query de consulta do relatório de blast radius de uma Release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de consulta do relatório de blast radius.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna o relatório de blast radius de uma Release.</summary>
    public sealed class Handler(IBlastRadiusRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var report = await repository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (report is null)
                return ChangeIntelligenceErrors.BlastRadiusReportNotFound(request.ReleaseId.ToString());

            return new Response(
                report.Id.Value,
                report.ReleaseId.Value,
                report.TotalAffectedConsumers,
                report.DirectConsumers,
                report.TransitiveConsumers,
                report.CalculatedAt);
        }
    }

    /// <summary>Resposta com os dados do relatório de blast radius da Release.</summary>
    public sealed record Response(
        Guid ReportId,
        Guid ReleaseId,
        int TotalAffectedConsumers,
        IReadOnlyList<string> DirectConsumers,
        IReadOnlyList<string> TransitiveConsumers,
        DateTimeOffset CalculatedAt);
}
