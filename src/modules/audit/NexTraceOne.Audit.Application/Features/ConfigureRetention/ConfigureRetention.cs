using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;

namespace NexTraceOne.Audit.Application.Features.ConfigureRetention;

/// <summary>
/// Feature: ConfigureRetention — configura a política de retenção de eventos de auditoria.
/// Placeholder para configuração futura via admin.
/// </summary>
public static class ConfigureRetention
{
    /// <summary>Comando de configuração de retenção.</summary>
    public sealed record Command(string PolicyName, int RetentionDays) : ICommand;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RetentionDays).GreaterThan(0).LessThanOrEqualTo(3650);
        }
    }

    /// <summary>Handler placeholder para configuração de retenção.</summary>
    public sealed class Handler : ICommandHandler<Command>
    {
        public Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            return Task.FromResult(Result<Unit>.Success(Unit.Value));
        }
    }
}
