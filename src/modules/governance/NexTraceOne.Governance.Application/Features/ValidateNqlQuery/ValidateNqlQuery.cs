using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Nql;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ValidateNqlQuery;

/// <summary>
/// Feature: ValidateNqlQuery — valida a sintaxe e governance de uma query NQL
/// sem executá-la.
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// </summary>
public static class ValidateNqlQuery
{
    public sealed record Query(
        string NqlQuery,
        string TenantId,
        string Persona,
        string UserId) : IQuery<Response>;

    public sealed record ErrorDto(string Code, string Message);

    public sealed record Response(
        bool IsValid,
        string? ParsedEntity,
        int? ParsedLimit,
        string? ParsedRenderHint,
        int FilterCount,
        int GroupByCount,
        ErrorDto? Error);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NqlQuery).NotEmpty().MaximumLength(4096);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Persona).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class Handler(IQueryGovernanceService governanceService) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var ctx = new NqlExecutionContext(request.TenantId, null, request.Persona, request.UserId);
            var result = governanceService.Validate(request.NqlQuery, ctx);

            Response response;
            if (result.IsValid && result.Plan is { } plan)
            {
                response = new Response(
                    IsValid: true,
                    ParsedEntity: plan.Entity.ToString(),
                    ParsedLimit: plan.Limit,
                    ParsedRenderHint: plan.RenderHint,
                    FilterCount: plan.Filters.Count,
                    GroupByCount: plan.GroupBy.Count,
                    Error: null);
            }
            else
            {
                response = new Response(
                    IsValid: false,
                    ParsedEntity: null,
                    ParsedLimit: null,
                    ParsedRenderHint: null,
                    FilterCount: 0,
                    GroupByCount: 0,
                    Error: new ErrorDto("NQL.SyntaxError", result.Error ?? "Unknown error."));
            }

            return Task.FromResult(Result<Response>.Success(response));
        }
    }
}
