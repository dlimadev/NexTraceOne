using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Application.Nql;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.ExecuteNqlQuery;

/// <summary>
/// Feature: ExecuteNqlQuery — executa uma query NQL sob governance total.
/// Retorna dados tabulares, série temporal ou métrica agregada conforme o RenderHint.
/// Quando os dados reais de um módulo cruzado não estão disponíveis, retorna
/// IsSimulated = true + SimulatedNote (honest gap pattern).
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// </summary>
public static class ExecuteNqlQuery
{
    public sealed record Query(
        string NqlQuery,
        string TenantId,
        string? EnvironmentId,
        string Persona,
        string UserId) : IQuery<Response>;

    public sealed record Response(
        bool IsSimulated,
        string? SimulatedNote,
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<object?>> Rows,
        int TotalRows,
        string RenderHint,
        long ExecutionMs,
        string ParsedEntity,
        int AppliedLimit);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NqlQuery).NotEmpty().MaximumLength(4096)
                .WithMessage("NQL query cannot be empty and must not exceed 4096 characters.");
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Persona).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class Handler(IQueryGovernanceService governanceService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var ctx = new NqlExecutionContext(
                request.TenantId,
                request.EnvironmentId,
                request.Persona,
                request.UserId);

            // Validate first
            var validation = governanceService.Validate(request.NqlQuery, ctx);
            if (!validation.IsValid)
                return Error.Validation(
                    "NqlQuery.Invalid",
                    validation.Error ?? "Invalid NQL query.");

            var plan = validation.Plan!;

            // Execute under governance
            var result = await governanceService.ExecuteAsync(plan, ctx, cancellationToken);

            return Result<Response>.Success(new Response(
                IsSimulated: result.IsSimulated,
                SimulatedNote: result.SimulatedNote,
                Columns: result.Columns,
                Rows: result.Rows,
                TotalRows: result.TotalRows,
                RenderHint: result.RenderHint,
                ExecutionMs: result.ExecutionMs,
                ParsedEntity: plan.Entity.ToString(),
                AppliedLimit: plan.Limit));
        }
    }
}
