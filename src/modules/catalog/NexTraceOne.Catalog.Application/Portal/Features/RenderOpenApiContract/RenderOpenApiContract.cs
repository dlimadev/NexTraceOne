using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.RenderOpenApiContract;

/// <summary>
/// Feature: RenderOpenApiContract — retorna contrato formatado para renderização.
/// Busca a versão de contrato pelo ApiAssetId (opcionalmente por versão semântica)
/// e retorna o SpecContent real para renderização no Developer Portal.
/// </summary>
public static class RenderOpenApiContract
{
    /// <summary>Query para obter contrato de uma API.</summary>
    public sealed record Query(Guid ApiAssetId, string? Version = null) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de contrato.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna contrato para renderização.
    /// Busca a versão mais recente ou uma versão específica do contrato
    /// e devolve o SpecContent real com metadados de confiança.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Fetch specific version or latest
            var contract = !string.IsNullOrWhiteSpace(request.Version)
                ? await contractVersionRepository.GetByApiAssetAndSemVerAsync(
                    request.ApiAssetId, request.Version, cancellationToken)
                : await contractVersionRepository.GetLatestByApiAssetAsync(
                    request.ApiAssetId, cancellationToken);

            if (contract is null)
            {
                return DeveloperPortalErrors.ApiNotFound(request.ApiAssetId.ToString());
            }

            var isValidState = contract.LifecycleState is
                ContractLifecycleState.Approved or
                ContractLifecycleState.Locked or
                ContractLifecycleState.Deprecated;

            return Result<Response>.Success(new Response(
                ApiAssetId: contract.ApiAssetId,
                ContractContent: contract.SpecContent,
                Version: contract.SemVer,
                Format: contract.Format,
                IsValid: isValidState,
                IsLocked: contract.IsLocked,
                LastUpdated: contract.UpdatedAt,
                Owner: null));
        }
    }

    /// <summary>Resposta com contrato e metadados de confiança.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string? ContractContent,
        string? Version,
        string Format,
        bool IsValid,
        bool IsLocked,
        DateTimeOffset? LastUpdated,
        string? Owner);
}
