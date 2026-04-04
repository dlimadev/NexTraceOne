using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.UpdateGovernancePack;

/// <summary>
/// Feature: UpdateGovernancePack — atualiza propriedades de um governance pack existente.
/// </summary>
public static class UpdateGovernancePack
{
    /// <summary>Comando para atualizar um governance pack existente.</summary>
    public sealed record Command(
        string PackId,
        string? DisplayName,
        string? Description,
        string? Category) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DisplayName).MaximumLength(200)
                .When(x => x.DisplayName is not null);
            RuleFor(x => x.Description).MaximumLength(2000)
                .When(x => x.Description is not null);
            RuleFor(x => x.Category).MaximumLength(100)
                .When(x => x.Category is not null);
        }
    }

    /// <summary>Handler que atualiza o governance pack e retorna o ID confirmado.</summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var pack = await packRepository.GetByIdAsync(new GovernancePackId(packGuid), cancellationToken);
            if (pack is null)
                return Error.NotFound("PACK_NOT_FOUND", "Governance pack '{0}' not found.", request.PackId);

            // Parse da categoria se fornecida
            GovernanceRuleCategory category = pack.Category;
            if (!string.IsNullOrEmpty(request.Category))
            {
                if (!Enum.TryParse<GovernanceRuleCategory>(request.Category, ignoreCase: true, out category))
                    return Error.Validation("INVALID_CATEGORY", "Category '{0}' is not valid.", request.Category);
            }

            pack.Update(
                displayName: request.DisplayName ?? pack.DisplayName,
                description: request.Description ?? pack.Description,
                category: category);

            await packRepository.UpdateAsync(pack, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            var response = new Response(PackId: pack.Id.Value.ToString());

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com o ID do governance pack atualizado.</summary>
    public sealed record Response(string PackId);
}
