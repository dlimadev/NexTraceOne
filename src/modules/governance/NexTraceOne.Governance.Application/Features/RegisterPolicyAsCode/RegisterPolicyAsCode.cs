using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.RegisterPolicyAsCode;

/// <summary>
/// Feature: RegisterPolicyAsCode — regista uma política de governança como código (YAML/JSON).
/// A política fica no estado Draft após registo. Pode ser activada e simulada antes de enforcement real.
/// </summary>
public static class RegisterPolicyAsCode
{
    public sealed record Command(
        string Name,
        string DisplayName,
        string? Description,
        string Version,
        PolicyDefinitionFormat Format,
        string DefinitionContent,
        PolicyEnforcementMode EnforcementMode,
        string RegisteredBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(30);
            RuleFor(x => x.DefinitionContent).NotEmpty().MaximumLength(100_000);
            RuleFor(x => x.RegisteredBy).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IPolicyAsCodeRepository repository,
        ICurrentTenant tenant,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await repository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
                return Error.Conflict("POLICY_AS_CODE_NAME_EXISTS",
                    "A policy definition named '{0}' already exists.", request.Name);

            var definition = PolicyAsCodeDefinition.Create(
                tenant.Id,
                request.Name,
                request.DisplayName,
                request.Description,
                request.Version,
                request.Format,
                request.DefinitionContent,
                request.EnforcementMode,
                request.RegisteredBy);

            await repository.AddAsync(definition, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                definition.Id.Value,
                definition.Name,
                definition.DisplayName,
                definition.Version,
                definition.Format,
                definition.EnforcementMode,
                definition.Status));
        }
    }

    public sealed record Response(
        Guid Id,
        string Name,
        string DisplayName,
        string Version,
        PolicyDefinitionFormat Format,
        PolicyEnforcementMode EnforcementMode,
        PolicyDefinitionStatus Status);
}
