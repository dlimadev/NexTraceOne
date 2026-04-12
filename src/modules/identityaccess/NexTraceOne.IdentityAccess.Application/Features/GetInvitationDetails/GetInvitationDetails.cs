using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.IdentityAccess.Application.Features.GetInvitationDetails;

/// <summary>
/// Feature: GetInvitationDetails — obtém detalhes de um convite por token.
/// Stub controlado: infraestrutura de convites ainda não implementada.
/// </summary>
public static class GetInvitationDetails
{
    public sealed record Query(string Token) : IQuery<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        }
    }

    public sealed record Response(
        string OrganizationName,
        string InvitedEmail,
        DateTimeOffset ExpiresAt);

    internal sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Infraestrutura de convites (entidade Invitation, persistência,
            // geração e envio de tokens) ainda não implementada.
            return Task.FromResult<Result<Response>>(
                Error.NotFound("invitation.not_found",
                    "Invitation not found or expired"));
        }
    }
}
