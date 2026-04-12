using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteContractCompliancePolicy;

/// <summary>
/// Feature: DeleteContractCompliancePolicy — remove uma política de compliance contratual.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class DeleteContractCompliancePolicy
{
    /// <summary>Comando para remover uma política de compliance contratual.</summary>
    public sealed record Command(Guid Id) : ICommand<Response>;

    /// <summary>Valida o identificador da política a remover.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que remove uma política de compliance contratual após verificar existência.
    /// </summary>
    public sealed class Handler(
        IContractCompliancePolicyRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var policy = await repository.GetByIdAsync(
                new ContractCompliancePolicyId(request.Id), cancellationToken);

            if (policy is null)
                return Error.NotFound("CompliancePolicy.NotFound", $"Compliance policy '{request.Id}' not found.");

            if (policy.TenantId != currentTenant.Id.ToString())
                return Error.NotFound("CompliancePolicy.NotFound", $"Compliance policy '{request.Id}' not found.");

            await repository.DeleteAsync(new ContractCompliancePolicyId(request.Id), cancellationToken);

            return new Response(request.Id);
        }
    }

    /// <summary>Resposta da remoção de política de compliance contratual.</summary>
    public sealed record Response(Guid Id);
}
