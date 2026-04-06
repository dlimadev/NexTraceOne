using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.SecurityGate.Ports;

namespace NexTraceOne.Governance.Application.SecurityGate.Features.GetSecurityDashboard;

/// <summary>Retorna dashboard global de segurança com métricas agregadas.</summary>
public static class GetSecurityDashboard
{
    /// <summary>Query para o dashboard de segurança.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Validação vazia — sem parâmetros obrigatórios.</summary>
    public sealed class Validator : AbstractValidator<Query>;

    /// <summary>Handler que agrega métricas de todos os scans.</summary>
    public sealed class Handler(ISecurityScanRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (totalScans, passedScans) = await repository.GetScanCountsAsync(cancellationToken);
            var topCategories = await repository.GetTopCategoriesAsync(5, cancellationToken);

            var passRate = totalScans == 0 ? 100.0 : Math.Round((double)passedScans / totalScans * 100, 1);

            return Result<Response>.Success(new Response(
                TotalScans: totalScans,
                PassedScans: passedScans,
                FailedScans: totalScans - passedScans,
                GatePassRate: passRate,
                TopCategories: topCategories.Select(c => new CategoryCountDto(c.Category, c.Count)).ToList()));
        }
    }

    /// <summary>Resposta do dashboard de segurança.</summary>
    public sealed record Response(
        int TotalScans,
        int PassedScans,
        int FailedScans,
        double GatePassRate,
        IReadOnlyList<CategoryCountDto> TopCategories);

    /// <summary>Contagem por categoria.</summary>
    public sealed record CategoryCountDto(string Category, int Count);
}
