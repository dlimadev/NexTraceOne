using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Contracts.Validation;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractValidationSummary;

/// <summary>
/// Feature: GetContractValidationSummary — devolve o resumo consolidado de validação de uma
/// versão de contrato (contadores por severidade, prontidão para revisão/publicação, fingerprint).
/// Read-only; recalcula a partir do spec via <see cref="ContractLintRunner"/>.
/// </summary>
public static class GetContractValidationSummary
{
    /// <summary>Query de obtenção do resumo de validação.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<ContractLintRunner.ValidationSummaryDto>;

    /// <summary>Valida a query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.ContractVersionId).NotEmpty();
    }

    /// <summary>Handler que calcula o resumo.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, ContractLintRunner.ValidationSummaryDto>
    {
        public async Task<Result<ContractLintRunner.ValidationSummaryDto>> Handle(
            Query request, CancellationToken cancellationToken)
        {
            var contract = await repository.GetByIdAsync(
                new ContractVersionId(request.ContractVersionId), cancellationToken);
            if (contract is null)
                return Error.NotFound("ContractVersion.NotFound",
                    $"Contract version {request.ContractVersionId} not found.");

            return ContractLintRunner.Summarize(contract.SpecContent, contract.Format, clock.UtcNow);
        }
    }
}
