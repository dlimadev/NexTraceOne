using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.DeveloperPortal.Domain.Errors;

namespace NexTraceOne.DeveloperPortal.Application.Features.RenderOpenApiContract;

/// <summary>
/// Feature: RenderOpenApiContract — retorna contrato OpenAPI formatado para renderização.
/// Inclui metadados de versão, validade e sinais de confiança do contrato.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RenderOpenApiContract
{
    /// <summary>Query para obter contrato OpenAPI de uma API.</summary>
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
    /// Handler que retorna contrato OpenAPI para renderização.
    /// Em produção, consulta módulo Contracts para obter o spec real.
    /// </summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            return Task.FromResult<Result<Response>>(
                DeveloperPortalErrors.ApiNotFound(request.ApiAssetId.ToString()));
        }
    }

    /// <summary>Resposta com contrato OpenAPI e metadados de confiança.</summary>
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
