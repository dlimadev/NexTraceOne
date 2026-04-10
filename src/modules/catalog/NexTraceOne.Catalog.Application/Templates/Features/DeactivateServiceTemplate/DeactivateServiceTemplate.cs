using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Errors;

namespace NexTraceOne.Catalog.Application.Templates.Features.DeactivateServiceTemplate;

/// <summary>
/// Feature: DeactivateServiceTemplate — desativa um template de serviço.
///
/// Um template desativado deixa de aparecer na listagem pública e não pode ser usado
/// para scaffolding até ser reativado. Templates já usados não são afetados retroativamente.
///
/// Persona primária: Platform Admin.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class DeactivateServiceTemplate
{
    /// <summary>Comando de desativação de template.</summary>
    public sealed record Command(Guid TemplateId) : ICommand<Response>;

    /// <summary>Valida o comando de desativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithMessage("TemplateId is required.");
        }
    }

    /// <summary>Handler que localiza e desativa o template.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = await repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template is null)
                return ServiceTemplateErrors.NotFound(request.TemplateId);

            template.Deactivate();
            await repository.UpdateAsync(template, cancellationToken);

            return Result<Response>.Success(new Response(template.Id.Value, template.Slug, IsActive: false));
        }
    }

    /// <summary>Confirmação de desativação.</summary>
    public sealed record Response(Guid TemplateId, string Slug, bool IsActive);
}
