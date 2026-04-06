using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDeprecationProgress;

/// <summary>
/// Feature: GetDeprecationProgress — retorna o progresso da migração de um contrato deprecated.
/// Mostra informações de sunset, dias restantes, contrato substituto e percentagem de progresso.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetDeprecationProgress
{
    /// <summary>Query para obter o progresso da deprecação de um contrato.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que calcula o progresso da deprecação e estima dias restantes até o sunset.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var latestVersion = await repository.GetLatestByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            if (latestVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.ApiAssetId.ToString());

            // Calcular dias restantes até sunset
            var daysUntilSunset = latestVersion.SunsetDate.HasValue
                ? (int)Math.Ceiling((latestVersion.SunsetDate.Value - DateTimeOffset.UtcNow).TotalDays)
                : (int?)null;

            // Calcular progresso com base na data de deprecação e sunset
            int progressPercent = 0;
            if (latestVersion.DeprecationDate.HasValue && latestVersion.SunsetDate.HasValue)
            {
                var totalDays = (latestVersion.SunsetDate.Value - latestVersion.DeprecationDate.Value).TotalDays;
                var elapsedDays = (DateTimeOffset.UtcNow - latestVersion.DeprecationDate.Value).TotalDays;
                progressPercent = totalDays > 0
                    ? Math.Clamp((int)Math.Round(elapsedDays / totalDays * 100), 0, 100)
                    : 100;
            }

            return new Response(
                request.ApiAssetId,
                latestVersion.Id.Value,
                latestVersion.LifecycleState.ToString(),
                latestVersion.SunsetDate,
                latestVersion.DeprecationDate,
                latestVersion.DeprecationNotice,
                daysUntilSunset,
                progressPercent);
        }
    }

    /// <summary>Resposta com o progresso da deprecação do contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        Guid LatestVersionId,
        string LifecycleState,
        DateTimeOffset? SunsetDate,
        DateTimeOffset? DeprecationDate,
        string? DeprecationNotice,
        int? DaysUntilSunset,
        int ProgressPercent);
}
