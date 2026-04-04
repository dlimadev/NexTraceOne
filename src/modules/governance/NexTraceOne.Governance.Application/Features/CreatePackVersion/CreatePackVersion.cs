using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.CreatePackVersion;

/// <summary>
/// Feature: CreatePackVersion — cria uma nova versão de um governance pack.
/// Persiste a versão com regras, enforcement mode e metadados de auditoria.
/// </summary>
public static class CreatePackVersion
{
    /// <summary>Comando para criar uma nova versão de governance pack.</summary>
    public sealed record Command(
        string PackId,
        string Version,
        string DefaultEnforcementMode,
        string? ChangeDescription,
        string CreatedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.DefaultEnforcementMode).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ChangeDescription).MaximumLength(2000)
                .When(x => x.ChangeDescription is not null);
            RuleFor(x => x.CreatedBy).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que cria uma nova versão real com persistência.</summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernancePackVersionRepository versionRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var pack = await packRepository.GetByIdAsync(new GovernancePackId(packGuid), cancellationToken);
            if (pack is null)
                return Error.NotFound("PACK_NOT_FOUND", "Governance pack '{0}' not found.", request.PackId);

            if (!Enum.TryParse<EnforcementMode>(request.DefaultEnforcementMode, ignoreCase: true, out var enforcementMode))
                return Error.Validation("INVALID_ENFORCEMENT_MODE", "Enforcement mode '{0}' is not valid.", request.DefaultEnforcementMode);

            var version = GovernancePackVersion.Create(
                packId: pack.Id,
                version: request.Version,
                rules: Array.Empty<GovernanceRuleBinding>(),
                defaultEnforcementMode: enforcementMode,
                changeDescription: request.ChangeDescription,
                createdBy: request.CreatedBy);

            await versionRepository.AddAsync(version, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                VersionId: version.Id.Value.ToString(),
                PackId: pack.Id.Value.ToString(),
                Version: version.Version,
                DefaultEnforcementMode: version.DefaultEnforcementMode.ToString(),
                ChangeDescription: version.ChangeDescription,
                CreatedBy: version.CreatedBy,
                CreatedAt: version.CreatedAt));
        }
    }

    /// <summary>Resposta com os detalhes da versão criada.</summary>
    public sealed record Response(
        string VersionId,
        string PackId,
        string Version,
        string DefaultEnforcementMode,
        string? ChangeDescription,
        string CreatedBy,
        DateTimeOffset CreatedAt);
}
