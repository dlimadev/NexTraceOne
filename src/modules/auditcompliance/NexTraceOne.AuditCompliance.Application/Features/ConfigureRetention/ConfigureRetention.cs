using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.AuditCompliance.Domain.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AuditCompliance.Application.Features.ConfigureRetention;

/// <summary>
/// Feature: ConfigureRetention — configura e persiste a política de retenção de eventos de auditoria.
/// P7.4 — handler real que persiste RetentionPolicy no AuditDbContext.
///
/// A política de retenção define por quantos dias os eventos de auditoria são mantidos.
/// A aplicação efectiva da retenção (eliminação de eventos expirados) é feita pelo ApplyRetention feature.
/// </summary>
public static class ConfigureRetention
{
    /// <summary>Comando de configuração de retenção.</summary>
    public sealed record Command(string PolicyName, int RetentionDays) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RetentionDays).GreaterThan(0).LessThanOrEqualTo(3650);
        }
    }

    /// <summary>Resposta com o Id da política criada e seus dados.</summary>
    public sealed record Response(Guid PolicyId, string PolicyName, int RetentionDays, bool IsActive);

    /// <summary>Handler que cria e persiste uma nova RetentionPolicy.</summary>
    public sealed class Handler(
        IRetentionPolicyRepository retentionPolicyRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var policy = RetentionPolicy.Create(request.PolicyName, request.RetentionDays);

            retentionPolicyRepository.Add(policy);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(policy.Id.Value, policy.Name, policy.RetentionDays, policy.IsActive);
        }
    }
}
