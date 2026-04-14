using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.UpdateDomain;

/// <summary>
/// Feature: UpdateDomain — atualiza os dados de um domínio de negócio existente.
/// Permite alteração do nome de exibição, descrição, criticidade e classificação de capacidade.
/// </summary>
public static class UpdateDomain
{
    /// <summary>Comando para atualizar um domínio existente.</summary>
    public sealed record Command(
        string DomainId,
        string DisplayName,
        string? Description,
        string Criticality,
        string? CapabilityClassification) : ICommand;

    /// <summary>Valida a entrada do comando de atualização de domínio.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DomainId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000)
                .When(x => x.Description is not null);
            RuleFor(x => x.Criticality).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CapabilityClassification).MaximumLength(200)
                .When(x => x.CapabilityClassification is not null);
        }
    }

    /// <summary>Handler que atualiza os dados do domínio.</summary>
    public sealed class Handler(
        IGovernanceDomainRepository domainRepository,
        IGovernanceUnitOfWork unitOfWork) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.DomainId, out var domainGuid))
                return Error.Validation("INVALID_DOMAIN_ID", "Domain ID '{0}' is not a valid GUID.", request.DomainId);

            var domain = await domainRepository.GetByIdAsync(new GovernanceDomainId(domainGuid), cancellationToken);
            if (domain is null)
                return Error.NotFound("DOMAIN_NOT_FOUND", "Domain '{0}' not found.", request.DomainId);

            // Parse do criticality
            if (!Enum.TryParse<DomainCriticality>(request.Criticality, ignoreCase: true, out var criticality))
                return Error.Validation("INVALID_CRITICALITY", "Criticality '{0}' is not valid. Use Low, Medium, High or Critical.", request.Criticality);

            domain.Update(
                displayName: request.DisplayName,
                description: request.Description,
                criticality: criticality,
                capabilityClassification: request.CapabilityClassification);

            await domainRepository.UpdateAsync(domain, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
