using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.CreateDomain;

/// <summary>
/// Feature: CreateDomain — cria um novo domínio de negócio na plataforma de governança.
/// Retorna o ID do domínio criado para referência imediata.
/// </summary>
public static class CreateDomain
{
    /// <summary>Comando para criar um novo domínio de negócio.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string? Description,
        string Criticality,
        string? CapabilityClassification) : ICommand<Response>;

    /// <summary>Handler que cria um novo domínio e retorna o ID gerado.</summary>
    public sealed class Handler(
        IGovernanceDomainRepository domainRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verifica se já existe domínio com o mesmo nome
            var existing = await domainRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
                return Error.Conflict("DOMAIN_NAME_EXISTS", "Domain with name '{0}' already exists.", request.Name);

            // Parse do criticality
            if (!Enum.TryParse<DomainCriticality>(request.Criticality, ignoreCase: true, out var criticality))
                return Error.Validation("INVALID_CRITICALITY", "Criticality '{0}' is not valid. Use Low, Medium, High or Critical.", request.Criticality);

            var domain = GovernanceDomain.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                criticality: criticality,
                capabilityClassification: request.CapabilityClassification);

            await domainRepository.AddAsync(domain, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(DomainId: domain.Id.Value.ToString(), IsSimulated: false);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID do domínio criado.</summary>
    public sealed record Response(string DomainId, bool IsSimulated = false);
}
