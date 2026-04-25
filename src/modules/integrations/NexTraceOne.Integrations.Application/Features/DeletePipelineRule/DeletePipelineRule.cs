using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Application.Features.DeletePipelineRule;

/// <summary>
/// Feature: DeletePipelineRule — remove uma regra de pipeline existente.
/// Ownership: módulo Integrations (Pipeline).
/// </summary>
public static class DeletePipelineRule
{
    /// <summary>Comando para remover uma regra de pipeline.</summary>
    public sealed record Command(
        string TenantId,
        Guid RuleId) : ICommand<Response>;

    /// <summary>Validador do comando DeletePipelineRule.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.RuleId).NotEmpty();
        }
    }

    /// <summary>Handler que remove uma regra de pipeline.</summary>
    public sealed class Handler(
        ITenantPipelineRuleRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var rule = await repository.GetByIdAsync(new TenantPipelineRuleId(request.RuleId), cancellationToken);
            if (rule is null)
                return Result<Response>.Failure(Error.NotFound("PipelineRule.NotFound", $"Pipeline rule '{request.RuleId}' not found."));

            await repository.DeleteAsync(rule, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(Deleted: true, RuleId: request.RuleId));
        }
    }

    /// <summary>Resposta do comando DeletePipelineRule.</summary>
    public sealed record Response(bool Deleted, Guid RuleId);
}
