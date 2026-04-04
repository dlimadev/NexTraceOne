using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Enums;
using NexTraceOne.Catalog.Domain.Templates.Errors;

namespace NexTraceOne.Catalog.Application.Templates.Features.GetServiceTemplate;

/// <summary>
/// Feature: GetServiceTemplate — devolve o detalhe de um template de serviço por id ou slug.
/// Persona primária: Developer, Architect, Platform Admin.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceTemplate
{
    /// <summary>Query para obter um template por id ou por slug.</summary>
    public sealed record Query(
        Guid? TemplateId,
        string? Slug) : IQuery<Response>;

    /// <summary>Valida que pelo menos um identificador é fornecido.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(q => q.TemplateId.HasValue || !string.IsNullOrWhiteSpace(q.Slug))
                .WithMessage("Either TemplateId or Slug must be provided.");

            RuleFor(x => x.Slug)
                .MaximumLength(64)
                .When(x => x.Slug is not null);
        }
    }

    /// <summary>Handler que resolve o template por id ou slug.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var template = request.TemplateId.HasValue
                ? await repository.GetByIdAsync(request.TemplateId.Value, cancellationToken)
                : await repository.GetBySlugAsync(request.Slug!, cancellationToken);

            if (template is null)
            {
                return request.TemplateId.HasValue
                    ? ServiceTemplateErrors.NotFound(request.TemplateId.Value)
                    : ServiceTemplateErrors.NotFoundBySlug(request.Slug!);
            }

            return Result<Response>.Success(new Response(
                TemplateId: template.Id.Value,
                Slug: template.Slug,
                DisplayName: template.DisplayName,
                Description: template.Description,
                Version: template.Version,
                ServiceType: template.ServiceType,
                Language: template.Language,
                DefaultDomain: template.DefaultDomain,
                DefaultTeam: template.DefaultTeam,
                Tags: template.Tags,
                GovernancePolicyIds: template.GovernancePolicyIds,
                HasBaseContract: template.BaseContractSpec is not null,
                HasScaffoldingManifest: template.ScaffoldingManifestJson is not null,
                RepositoryTemplateUrl: template.RepositoryTemplateUrl,
                RepositoryTemplateBranch: template.RepositoryTemplateBranch,
                IsActive: template.IsActive,
                UsageCount: template.UsageCount,
                TenantId: template.TenantId,
                CreatedAt: template.CreatedAt,
                UpdatedAt: template.UpdatedAt));
        }
    }

    /// <summary>Detalhe completo de um template de serviço.</summary>
    public sealed record Response(
        Guid TemplateId,
        string Slug,
        string DisplayName,
        string Description,
        string Version,
        TemplateServiceType ServiceType,
        TemplateLanguage Language,
        string DefaultDomain,
        string DefaultTeam,
        IReadOnlyList<string> Tags,
        IReadOnlyList<Guid> GovernancePolicyIds,
        bool HasBaseContract,
        bool HasScaffoldingManifest,
        string? RepositoryTemplateUrl,
        string? RepositoryTemplateBranch,
        bool IsActive,
        int UsageCount,
        Guid? TenantId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
