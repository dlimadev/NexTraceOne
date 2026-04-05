using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPolicyAsCode;

/// <summary>
/// Feature: GetPolicyAsCode — obtém os detalhes de uma política como código pelo nome técnico.
/// </summary>
public static class GetPolicyAsCode
{
    public sealed record Query(string PolicyName) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(IPolicyAsCodeRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var definition = await repository.GetByNameAsync(request.PolicyName, cancellationToken);
            if (definition is null)
                return Error.NotFound("POLICY_AS_CODE_NOT_FOUND", "Policy definition '{0}' not found.", request.PolicyName);

            return Result<Response>.Success(new Response(
                definition.Id.Value,
                definition.Name,
                definition.DisplayName,
                definition.Description,
                definition.Version,
                definition.Format,
                definition.DefinitionContent,
                definition.EnforcementMode,
                definition.Status,
                definition.SimulatedAffectedServices,
                definition.SimulatedNonCompliantServices,
                definition.LastSimulatedAt,
                definition.RegisteredBy));
        }
    }

    public sealed record Response(
        Guid Id,
        string Name,
        string DisplayName,
        string? Description,
        string Version,
        PolicyDefinitionFormat Format,
        string DefinitionContent,
        PolicyEnforcementMode EnforcementMode,
        PolicyDefinitionStatus Status,
        int? SimulatedAffectedServices,
        int? SimulatedNonCompliantServices,
        DateTimeOffset? LastSimulatedAt,
        string RegisteredBy);
}
