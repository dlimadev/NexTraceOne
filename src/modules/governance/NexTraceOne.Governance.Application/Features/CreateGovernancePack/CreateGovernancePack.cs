using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.CreateGovernancePack;

/// <summary>
/// Feature: CreateGovernancePack — cria um novo governance pack na plataforma.
/// Retorna o ID do pack criado para referência imediata.
/// </summary>
public static class CreateGovernancePack
{
    /// <summary>Comando para criar um novo governance pack.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string? Description,
        string Category) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000)
                .When(x => x.Description is not null);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que cria um novo governance pack e retorna o ID gerado.</summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verifica se já existe pack com o mesmo nome
            var existing = await packRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
                return Error.Conflict("PACK_NAME_EXISTS", "Governance pack with name '{0}' already exists.", request.Name);

            // Parse da categoria
            if (!Enum.TryParse<GovernanceRuleCategory>(request.Category, ignoreCase: true, out var category))
                return Error.Validation("INVALID_CATEGORY", "Category '{0}' is not valid.", request.Category);

            var pack = GovernancePack.Create(
                name: request.Name,
                displayName: request.DisplayName,
                description: request.Description,
                category: category);

            await packRepository.AddAsync(pack, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(PackId: pack.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID do governance pack criado.</summary>
    public sealed record Response(string PackId);
}
