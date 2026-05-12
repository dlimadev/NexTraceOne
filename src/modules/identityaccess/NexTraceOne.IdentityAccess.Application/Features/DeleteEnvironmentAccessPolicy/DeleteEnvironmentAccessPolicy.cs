using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.DeleteEnvironmentAccessPolicy;

/// <summary>Feature: DeleteEnvironmentAccessPolicy — desactiva uma política de acesso por ambiente.</summary>
public static class DeleteEnvironmentAccessPolicy
{
    /// <summary>Comando para desactivar uma política de acesso por ambiente.</summary>
    public sealed record Command(Guid PolicyId) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyId).NotEmpty();
        }
    }

    /// <summary>Resposta indicando se a política foi desactivada.</summary>
    public sealed record Response(bool Deactivated);

    /// <summary>Handler que desactiva a política de acesso por ambiente.</summary>
    internal sealed class Handler(
        IEnvironmentAccessPolicyRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = await repository.GetByIdAsync(
                EnvironmentAccessPolicyId.From(request.PolicyId), cancellationToken);

            if (policy is null)
                return Error.NotFound("EnvironmentAccessPolicy.NotFound", "Política de acesso não encontrada.");

            policy.Deactivate();
            await repository.UpdateAsync(policy, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(true);
        }
    }
}
