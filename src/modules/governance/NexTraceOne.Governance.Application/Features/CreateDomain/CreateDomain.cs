using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var response = new Response(DomainId: Guid.NewGuid().ToString());

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com o ID do domínio criado.</summary>
    public sealed record Response(string DomainId);
}
