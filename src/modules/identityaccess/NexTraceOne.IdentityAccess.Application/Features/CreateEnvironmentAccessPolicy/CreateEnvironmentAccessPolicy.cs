using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Features.CreateEnvironmentAccessPolicy;

/// <summary>
/// Feature: CreateEnvironmentAccessPolicy — cria política de acesso granular por ambiente.
/// W5-05: Fine-Grained Auth per Environment.
/// </summary>
public static class CreateEnvironmentAccessPolicy
{
    /// <summary>Comando para criar uma nova política de acesso por ambiente.</summary>
    public sealed record Command(
        string PolicyName,
        IReadOnlyList<string> Environments,
        IReadOnlyList<string> AllowedRoles,
        IReadOnlyList<string> RequireJitForRoles,
        string? JitApprovalRequiredFrom) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environments).NotEmpty();
            RuleFor(x => x.AllowedRoles).NotNull();
            RuleFor(x => x.RequireJitForRoles).NotNull();
        }
    }

    /// <summary>Resposta com o identificador da política criada.</summary>
    public sealed record Response(Guid PolicyId);

    /// <summary>Handler que persiste a política de acesso por ambiente.</summary>
    internal sealed class Handler(
        IEnvironmentAccessPolicyRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = EnvironmentAccessPolicy.Create(
                request.PolicyName,
                currentTenant.Id,
                request.Environments,
                request.AllowedRoles,
                request.RequireJitForRoles,
                request.JitApprovalRequiredFrom,
                clock.UtcNow);

            await repository.AddAsync(policy, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(policy.Id.Value);
        }
    }
}
