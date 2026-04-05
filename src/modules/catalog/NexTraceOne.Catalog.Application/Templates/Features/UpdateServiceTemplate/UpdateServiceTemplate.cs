using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Errors;

namespace NexTraceOne.Catalog.Application.Templates.Features.UpdateServiceTemplate;

/// <summary>
/// Feature: UpdateServiceTemplate — atualiza os metadados de um template de serviço existente.
///
/// Campos atualizáveis: display name, descrição, versão, domínio padrão, equipa padrão,
/// tags, políticas de governança, contrato base, manifesto de scaffolding e repositório.
///
/// Campos imutáveis após criação: slug, serviceType, language.
///
/// Persona primária: Platform Admin, Architect.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class UpdateServiceTemplate
{
    /// <summary>Comando de atualização de template de serviço.</summary>
    public sealed record Command(
        Guid TemplateId,
        string DisplayName,
        string Description,
        string Version,
        string DefaultDomain,
        string DefaultTeam,
        IReadOnlyList<string>? Tags = null,
        IReadOnlyList<Guid>? GovernancePolicyIds = null,
        string? BaseContractSpec = null,
        string? ScaffoldingManifestJson = null,
        string? RepositoryTemplateUrl = null,
        string? RepositoryTemplateBranch = null) : ICommand<Response>;

    /// <summary>Valida o comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(32);
            RuleFor(x => x.DefaultDomain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DefaultTeam).NotEmpty().MaximumLength(200);

            RuleFor(x => x.Tags)
                .Must(t => t is null || t.Count <= 20)
                .WithMessage("Maximum 20 tags per template.");

            RuleFor(x => x.GovernancePolicyIds)
                .Must(g => g is null || g.Count <= 50)
                .WithMessage("Maximum 50 governance policies per template.");
        }
    }

    /// <summary>Handler que localiza o template e aplica a atualização.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = await repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template is null)
                return ServiceTemplateErrors.NotFound(request.TemplateId);

            template.Update(
                displayName: request.DisplayName,
                description: request.Description,
                version: request.Version,
                defaultDomain: request.DefaultDomain,
                defaultTeam: request.DefaultTeam,
                tags: request.Tags,
                governancePolicyIds: request.GovernancePolicyIds,
                baseContractSpec: request.BaseContractSpec,
                scaffoldingManifestJson: request.ScaffoldingManifestJson,
                repositoryTemplateUrl: request.RepositoryTemplateUrl,
                repositoryTemplateBranch: request.RepositoryTemplateBranch);

            await repository.UpdateAsync(template, cancellationToken);

            return Result<Response>.Success(new Response(
                TemplateId: template.Id.Value,
                Slug: template.Slug,
                DisplayName: template.DisplayName,
                Version: template.Version,
                UpdatedAt: template.UpdatedAt));
        }
    }

    /// <summary>Resposta com confirmação da atualização.</summary>
    public sealed record Response(
        Guid TemplateId,
        string Slug,
        string DisplayName,
        string Version,
        DateTimeOffset? UpdatedAt);
}
