using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.RulesetGovernance.Entities;

namespace NexTraceOne.ChangeGovernance.Application.RulesetGovernance.Features.ActivateSpectralPackage;

/// <summary>
/// Feature: ActivateSpectralPackage — instala e activa um pacote Spectral do marketplace.
/// Cria um Ruleset Default com o conteúdo padrão do pacote seleccionado.
/// Idempotente: se já existe um ruleset com o mesmo nome de pacote, reactiva-o.
///
/// CC-08: Contract Linting Marketplace.
/// </summary>
public static class ActivateSpectralPackage
{
    private static readonly Dictionary<string, (string Content, string Description)> PackageContents = new()
    {
        ["enterprise"] = (
            """{"rules":{"no-breaking-changes":{"given":"$","severity":"error"},"require-api-version":{"given":"$.info","severity":"warn"}}}""",
            "Enterprise Governance Pack — breaking change detection, versioning conventions."),
        ["security"] = (
            """{"rules":{"require-auth":{"given":"$.paths.*.*","severity":"error"},"no-pii-in-path":{"given":"$.paths","severity":"warn"}}}""",
            "API Security Pack — OWASP API Top 10, PII exposure, authentication."),
        ["accessibility"] = (
            """{"rules":{"require-error-schema":{"given":"$.paths.*.*.responses.4*","severity":"warn"},"require-pagination":{"given":"$.paths.*.get","severity":"info"}}}""",
            "API Accessibility Pack — HTTP conventions, error formats, pagination."),
        ["internal-platform"] = (
            """{"rules":{"nexttrace-naming":{"given":"$.paths","severity":"warn"},"require-contract-metadata":{"given":"$.info","severity":"error"}}}""",
            "NexTraceOne Platform Conventions — internal standards and naming."),
    };

    public sealed record Command(string PackageId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PackageId)
                .NotEmpty()
                .Must(id => PackageContents.ContainsKey(id))
                .WithMessage("PackageId must be one of: enterprise, security, accessibility, internal-platform.");
        }
    }

    public sealed class Handler(
        IRulesetRepository repository,
        IRulesetGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var packageName = $"spectral-marketplace/{request.PackageId}";
            var (content, description) = PackageContents[request.PackageId];

            var existing = await repository.FindByNameAsync(packageName, cancellationToken);
            if (existing is not null)
            {
                existing.Activate();
                await unitOfWork.CommitAsync(cancellationToken);
                return new Response(existing.Id.Value, packageName, activated: false, reactivated: true);
            }

            var ruleset = Ruleset.Create(packageName, description, content, RulesetType.Default, clock.UtcNow);
            repository.Add(ruleset);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(ruleset.Id.Value, packageName, activated: true, reactivated: false);
        }
    }

    public sealed record Response(Guid RulesetId, string PackageName, bool Activated, bool Reactivated);
}
