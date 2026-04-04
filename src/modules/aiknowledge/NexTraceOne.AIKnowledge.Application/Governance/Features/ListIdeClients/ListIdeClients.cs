using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListIdeClients;

/// <summary>
/// Feature: ListIdeClients — lista registos de clientes IDE autorizados.
/// Permite filtragem por utilizador, tipo de cliente e estado.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListIdeClients
{
    /// <summary>Query de listagem de clientes IDE registados.</summary>
    public sealed record Query(
        string? UserId,
        string? ClientType,
        bool? IsActive,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Validador da query ListIdeClients.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).MaximumLength(200).When(x => x.UserId is not null);
            RuleFor(x => x.ClientType).MaximumLength(200).When(x => x.ClientType is not null);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        }
    }

    /// <summary>Handler que lista clientes IDE com filtros.</summary>
    public sealed class Handler(
        IAiIdeClientRegistrationRepository clientRegistrationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var parsedClientType = request.ClientType is not null
                ? Enum.TryParse<AIClientType>(request.ClientType, ignoreCase: true, out var ct) ? ct : (AIClientType?)null
                : null;

            var clients = await clientRegistrationRepository.ListAsync(
                request.UserId,
                parsedClientType,
                request.IsActive,
                request.PageSize,
                cancellationToken);

            var items = clients
                .Select(c => new ClientItem(
                    c.Id.Value,
                    c.UserId,
                    c.UserDisplayName,
                    c.ClientType.ToString(),
                    c.ClientVersion,
                    c.DeviceIdentifier,
                    c.LastAccessAt,
                    c.IsActive,
                    c.RevocationReason))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de clientes IDE.</summary>
    public sealed record Response(
        IReadOnlyList<ClientItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um registo de cliente IDE.</summary>
    public sealed record ClientItem(
        Guid RegistrationId,
        string UserId,
        string UserDisplayName,
        string ClientType,
        string? ClientVersion,
        string? DeviceIdentifier,
        DateTimeOffset? LastAccessAt,
        bool IsActive,
        string? RevocationReason);
}
