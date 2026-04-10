using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Templates.Abstractions;
using NexTraceOne.Catalog.Domain.Templates.Errors;

namespace NexTraceOne.Catalog.Application.Templates.Features.ActivateServiceTemplate;

/// <summary>
/// Feature: ActivateServiceTemplate — reativa um template de serviço previamente desativado.
///
/// Um template ativo fica imediatamente disponível para scaffolding por developers.
///
/// Persona primária: Platform Admin.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ActivateServiceTemplate
{
    /// <summary>Comando de ativação de template.</summary>
    public sealed record Command(Guid TemplateId) : ICommand<Response>;

    /// <summary>Valida o comando de ativação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithMessage("TemplateId is required.");
        }
    }

    /// <summary>Handler que localiza e ativa o template.</summary>
    public sealed class Handler(IServiceTemplateRepository repository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = await repository.GetByIdAsync(request.TemplateId, cancellationToken);
            if (template is null)
                return ServiceTemplateErrors.NotFound(request.TemplateId);

            template.Activate();
            await repository.UpdateAsync(template, cancellationToken);

            return Result<Response>.Success(new Response(template.Id.Value, template.Slug, IsActive: true));
        }
    }

    /// <summary>Confirmação de ativação.</summary>
    public sealed record Response(Guid TemplateId, string Slug, bool IsActive);
}
