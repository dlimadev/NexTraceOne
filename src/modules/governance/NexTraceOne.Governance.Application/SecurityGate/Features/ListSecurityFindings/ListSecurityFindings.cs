using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;
using NexTraceOne.Governance.Domain.SecurityGate.Enums;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.ListSecurityFindings;

/// <summary>Lista achados de segurança com filtros por severidade, categoria e estado.</summary>
public static class ListSecurityFindings
{
    /// <summary>Query para listar achados.</summary>
    public sealed record Query(
        Guid? TargetId = null,
        FindingSeverity MinSeverity = FindingSeverity.Info,
        SecurityCategory? Category = null,
        FindingStatus? Status = null,
        int PageSize = 20,
        int PageNumber = 1) : IQuery<Response>;

    /// <summary>Validação da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        }
    }

    /// <summary>Handler que carrega e filtra os achados.</summary>
    public sealed class Handler(ISecurityScanRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var findings = await repository.ListFindingsAsync(
                request.TargetId, request.MinSeverity, request.Category,
                request.Status, request.PageSize, request.PageNumber, cancellationToken);

            var items = findings.Select(f => new FindingItemDto(
                f.FindingId, f.ScanResultId, f.RuleId,
                f.Category.ToString(), f.Severity.ToString(),
                f.FilePath, f.LineNumber, f.Description, f.CweId, f.Status.ToString())).ToList();

            return Result<Response>.Success(new Response(Items: items, PageSize: request.PageSize, PageNumber: request.PageNumber));
        }
    }

    /// <summary>Resposta com lista de achados.</summary>
    public sealed record Response(
        IReadOnlyList<FindingItemDto> Items,
        int PageSize,
        int PageNumber);

    /// <summary>Item resumido de achado.</summary>
    public sealed record FindingItemDto(
        Guid FindingId,
        Guid ScanResultId,
        string RuleId,
        string Category,
        string Severity,
        string FilePath,
        int? LineNumber,
        string Description,
        string? CweId,
        string Status);
}
