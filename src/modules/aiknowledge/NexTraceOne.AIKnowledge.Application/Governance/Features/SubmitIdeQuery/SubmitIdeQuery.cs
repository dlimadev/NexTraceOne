using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitIdeQuery;

/// <summary>
/// Feature: SubmitIdeQuery — regista uma nova consulta de AI pair programming governado a partir do IDE.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SubmitIdeQuery
{
    /// <summary>Comando de submissão de consulta IDE.</summary>
    public sealed record Command(
        string IdeClient,
        string IdeClientVersion,
        string QueryTypeValue,
        string QueryText,
        string? QueryContext,
        string ModelUsed) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de submissão de consulta IDE.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IdeClient).NotEmpty().MaximumLength(50);
            RuleFor(x => x.IdeClientVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.QueryTypeValue).NotEmpty()
                .Must(v => v is "ContractSuggestion" or "BreakingChangeAlert" or "OwnershipLookup"
                    or "TestGeneration" or "GeneralQuery" or "CodeGeneration")
                .WithMessage("QueryTypeValue must be 'ContractSuggestion', 'BreakingChangeAlert', 'OwnershipLookup', 'TestGeneration', 'GeneralQuery', or 'CodeGeneration'.");
            RuleFor(x => x.QueryText).NotEmpty().MaximumLength(10000);
            RuleFor(x => x.ModelUsed).NotEmpty().MaximumLength(300);
        }
    }

    /// <summary>Handler que regista uma nova consulta IDE governada.</summary>
    public sealed class Handler(
        IIdeQuerySessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Enum.TryParse<IdeQueryType>(request.QueryTypeValue, ignoreCase: true, out var queryType))
                return Error.Validation("IdeQuery.InvalidQueryType", $"'{request.QueryTypeValue}' is not a valid IDE query type.");

            var session = IdeQuerySession.Create(
                userId: currentUser.Id,
                ideClient: request.IdeClient,
                ideClientVersion: request.IdeClientVersion,
                queryType: queryType,
                queryText: request.QueryText,
                queryContext: request.QueryContext,
                modelUsed: request.ModelUsed,
                tenantId: currentTenant.Id,
                submittedAt: dateTimeProvider.UtcNow);

            await sessionRepository.AddAsync(session, cancellationToken);

            return new Response(session.Id.Value);
        }
    }

    /// <summary>Resposta da submissão da consulta IDE.</summary>
    public sealed record Response(Guid SessionId);
}
