using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.ValidateContractIntegrity;

/// <summary>
/// Feature: ValidateContractIntegrity — valida se uma especificação OpenAPI pode ser parseada com sucesso.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ValidateContractIntegrity
{
    /// <summary>Query de validação de integridade de versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de validação de integridade de contrato.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que verifica se a especificação OpenAPI da versão é válida e retorna seus metadados.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            try
            {
                var schema = OpenApiSchema.Parse(version.SpecContent, version.Format);
                return new Response(true, schema.PathCount, schema.EndpointCount, schema.Version, null);
            }
            catch (Exception)
            {
                return new Response(false, 0, 0, null, "The specification content could not be parsed.");
            }
        }
    }

    /// <summary>Resposta da validação de integridade de versão de contrato.</summary>
    public sealed record Response(
        bool IsValid,
        int PathCount,
        int EndpointCount,
        string? SchemaVersion,
        string? ValidationError);
}

