using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Entities;
using NexTraceOne.Catalog.Domain.Templates.Enums;
using NexTraceOne.Catalog.Domain.Templates.Errors;

namespace NexTraceOne.Catalog.Application.Templates.Features.CreateServiceTemplate;

/// <summary>
/// Feature: CreateServiceTemplate — cria um novo template de serviço governado.
///
/// Um template define o padrão de criação de novos serviços, incluindo:
///   - Stack tecnológica e tipo de serviço
///   - Ownership e domínio padrão
///   - Políticas de governança aplicadas automaticamente
///   - Contrato base (OpenAPI/AsyncAPI/WSDL)
///   - Manifesto de scaffolding (estrutura de ficheiros)
///
/// Validações:
///   - Slug único (não pode existir outro template com mesmo slug)
///   - Slug em lowercase com hífens (kebab-case)
///   - Versão no formato semver ou date-string
///
/// Valor: garante que cada novo serviço nasce com contratos, governança e ownership
/// definidos desde o primeiro commit.
/// Persona primária: Platform Admin, Architect.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateServiceTemplate
{
    /// <summary>Comando de criação de template de serviço.</summary>
    public sealed record Command(
        string Slug,
        string DisplayName,
        string Description,
        string Version,
        TemplateServiceType ServiceType,
        TemplateLanguage Language,
        string DefaultDomain,
        string DefaultTeam,
        IReadOnlyList<string>? Tags = null,
        IReadOnlyList<Guid>? GovernancePolicyIds = null,
        string? BaseContractSpec = null,
        string? ScaffoldingManifestJson = null,
        string? RepositoryTemplateUrl = null,
        string? RepositoryTemplateBranch = null,
        Guid? TenantId = null) : ICommand<Response>;

    /// <summary>Valida o comando de criação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly System.Text.RegularExpressions.Regex SlugPattern =
            new(@"^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$", System.Text.RegularExpressions.RegexOptions.Compiled);

        public Validator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty()
                .MaximumLength(64)
                .Must(s => SlugPattern.IsMatch(s))
                .WithMessage("Slug must be lowercase kebab-case (e.g. 'payment-api').");

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

    /// <summary>Handler que valida unicidade de slug e cria o template.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Verificar unicidade do slug
            var exists = await repository.ExistsBySlugAsync(request.Slug, cancellationToken);
            if (exists)
                return ServiceTemplateErrors.DuplicateSlug(request.Slug);

            var template = ServiceTemplate.Create(
                slug: request.Slug,
                displayName: request.DisplayName,
                description: request.Description,
                version: request.Version,
                serviceType: request.ServiceType,
                language: request.Language,
                defaultDomain: request.DefaultDomain,
                defaultTeam: request.DefaultTeam,
                tags: request.Tags,
                governancePolicyIds: request.GovernancePolicyIds,
                baseContractSpec: request.BaseContractSpec,
                scaffoldingManifestJson: request.ScaffoldingManifestJson,
                repositoryTemplateUrl: request.RepositoryTemplateUrl,
                repositoryTemplateBranch: request.RepositoryTemplateBranch,
                tenantId: request.TenantId);

            await repository.AddAsync(template, cancellationToken);

            return Result<Response>.Success(new Response(
                TemplateId: template.Id.Value,
                Slug: template.Slug,
                DisplayName: template.DisplayName,
                Version: template.Version,
                ServiceType: template.ServiceType,
                Language: template.Language));
        }
    }

    /// <summary>Resposta com os dados do template criado.</summary>
    public sealed record Response(
        Guid TemplateId,
        string Slug,
        string DisplayName,
        string Version,
        TemplateServiceType ServiceType,
        TemplateLanguage Language);
}
