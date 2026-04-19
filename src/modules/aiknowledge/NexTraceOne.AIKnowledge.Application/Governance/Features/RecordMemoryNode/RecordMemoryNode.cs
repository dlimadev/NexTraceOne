using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RecordMemoryNode;

/// <summary>
/// Feature: RecordMemoryNode — regista um nó de memória organizacional.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RecordMemoryNode
{
    private static readonly string[] ValidNodeTypes = ["decision", "incident", "contract_evolution", "pattern_learned", "adr"];

    public sealed record Command(
        string NodeType,
        string Subject,
        string Title,
        string Content,
        string Context,
        string ActorId,
        string[] Tags,
        string SourceType,
        string? SourceId,
        Guid TenantId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NodeType).NotEmpty()
                .Must(t => ValidNodeTypes.Contains(t))
                .WithMessage("NodeType must be one of: decision, incident, contract_evolution, pattern_learned, adr.");
            RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(
        IOrganizationalMemoryRepository memoryRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var node = OrganizationalMemoryNode.Create(
                request.NodeType,
                request.Subject,
                request.Title,
                request.Content,
                request.Context,
                request.ActorId,
                request.Tags,
                request.SourceType,
                request.SourceId,
                request.TenantId,
                DateTimeOffset.UtcNow);

            memoryRepository.Add(node);
            await unitOfWork.CommitAsync(ct);

            return new Response(node.Id.Value, node.NodeType, node.Subject, node.RecordedAt);
        }
    }

    public sealed record Response(Guid NodeId, string NodeType, string Subject, DateTimeOffset RecordedAt);
}
