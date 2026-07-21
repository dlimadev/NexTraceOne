using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Validation;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractSpectral;

/// <summary>
/// Feature: ValidateContractSpectral — executa o linting nativo de contratos sobre uma versão
/// de contrato e devolve os issues detectados + o resumo consolidado. Read-only (não persiste).
/// Ver <see cref="ContractLintRunner"/>.
/// </summary>
public static class ValidateContractSpectral
{
    /// <summary>Query de execução de linting sobre uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ContractVersionId).NotEmpty();
    }

    /// <summary>Resposta com os issues e o resumo de validação.</summary>
    public sealed record Response(
        IReadOnlyList<ValidationIssue> Issues,
        ContractLintRunner.ValidationSummaryDto Summary);

    /// <summary>Handler que executa o linting.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var contract = await repository.GetByIdAsync(
                new ContractVersionId(request.ContractVersionId), cancellationToken);
            if (contract is null)
                return Error.NotFound("ContractVersion.NotFound",
                    $"Contract version {request.ContractVersionId} not found.");

            var lint = ContractLintRunner.Run(contract.SpecContent, contract.Format, clock.UtcNow);
            return new Response(lint.Issues, lint.Summary);
        }
    }
}
