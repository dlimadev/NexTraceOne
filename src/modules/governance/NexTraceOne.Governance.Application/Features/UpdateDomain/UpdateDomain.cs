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

    /// <summary>Handler que atualiza os dados do domínio.</summary>
    public sealed class Handler(
        IGovernanceDomainRepository domainRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command>
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
