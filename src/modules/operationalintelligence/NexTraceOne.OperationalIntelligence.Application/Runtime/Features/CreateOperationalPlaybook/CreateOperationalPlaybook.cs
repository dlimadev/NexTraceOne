using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.CreateOperationalPlaybook;

/// <summary>
/// Feature: CreateOperationalPlaybook — cria e persiste um playbook operacional no estado Draft.
/// O playbook contém passos ordenados, ligações a serviços e runbooks, e tags de categorização.
///
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateOperationalPlaybook
{
    /// <summary>Comando para criar um playbook operacional.</summary>
    public sealed record Command(
        string Name,
        string? Description,
        string Steps,
        string? LinkedServiceIds,
        string? LinkedRunbookIds,
        string? Tags) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de criação de playbook.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Steps).NotEmpty();
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        }
    }

    /// <summary>
    /// Handler que cria e persiste o playbook operacional no estado Draft.
    /// </summary>
    public sealed class Handler(
        IOperationalPlaybookRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            var playbook = OperationalPlaybook.Create(
                name: request.Name,
                description: request.Description,
                steps: request.Steps,
                linkedServiceIds: request.LinkedServiceIds,
                linkedRunbookIds: request.LinkedRunbookIds,
                tags: request.Tags,
                tenantId: currentTenant.Id.ToString(),
                createdAt: now,
                createdBy: currentUser.Id);

            await repository.AddAsync(playbook, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                playbook.Id.Value,
                playbook.Name,
                playbook.Description,
                playbook.Version,
                playbook.Status.ToString(),
                now));
        }
    }

    /// <summary>Resposta com os dados do playbook criado.</summary>
    public sealed record Response(
        Guid PlaybookId,
        string Name,
        string? Description,
        int Version,
        string Status,
        DateTimeOffset CreatedAt);
}
