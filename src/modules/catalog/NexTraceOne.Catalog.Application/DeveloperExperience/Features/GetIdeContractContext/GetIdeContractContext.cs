using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

namespace NexTraceOne.Catalog.Application.DeveloperExperience.Features.GetIdeContractContext;

/// <summary>
/// Feature: GetIdeContractContext — snapshot compacto de contexto de contrato para extensões IDE.
///
/// Retorna payload optimizado, campos: versão, tipo, exemplos, status, consumers.
///
/// Endpoint: GET /api/v1/ide/context/contract/{name}
/// Wave AK.1 — IDE Context API (Catalog / DeveloperExperience).
/// </summary>
public static class GetIdeContractContext
{
    public sealed record Query(
        string TenantId,
        string ContractName) : IQuery<ContractContextSnapshot>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Snapshot compacto de contrato para IDE.</summary>
    public sealed record ContractContextSnapshot(
        string ContractName,
        string? ContractType,
        string? CurrentVersion,
        string? Status,
        int ConsumerCount,
        string? ExamplePayloadJson,
        DateTimeOffset GeneratedAt);

    public sealed class Handler(
        IIdeContextReader contextReader,
        IDateTimeProvider clock) : IQueryHandler<Query, ContractContextSnapshot>
    {
        public async Task<Result<ContractContextSnapshot>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.NullOrWhiteSpace(request.ContractName);

            var snapshot = await contextReader.GetContractContextAsync(
                request.TenantId,
                request.ContractName,
                cancellationToken);

            if (snapshot is null)
                return Error.NotFound(
                    "IDE.ContractNotFound",
                    $"Contract '{request.ContractName}' not found for tenant '{request.TenantId}'.");

            return Result<ContractContextSnapshot>.Success(snapshot with { GeneratedAt = clock.UtcNow });
        }
    }
}
