using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CommercialCatalog.Application.Abstractions;
using NexTraceOne.CommercialCatalog.Domain.Entities;
using NexTraceOne.CommercialCatalog.Domain.Errors;

namespace NexTraceOne.CommercialCatalog.Application.Features.CreateFeaturePack;

/// <summary>
/// Feature: CreateFeaturePack — operação de vendor ops para criar um pacote de funcionalidades.
///
/// Permite definir conjuntos reutilizáveis de capabilities que podem ser associados
/// a múltiplos planos comerciais. Cada item do pacote representa uma capability
/// com limite padrão opcional.
///
/// Permissão requerida: licensing:vendor:featurepack:create
/// </summary>
public static class CreateFeaturePack
{
    /// <summary>Comando para criação de um novo pacote de funcionalidades.</summary>
    public sealed record Command(
        string Code,
        string Name,
        string? Description,
        IReadOnlyList<ItemInput> Items) : ICommand<Response>;

    /// <summary>Entrada de um item do pacote com código, nome e limite opcional.</summary>
    public sealed record ItemInput(
        string CapabilityCode,
        string CapabilityName,
        int? DefaultLimit);

    /// <summary>Valida a entrada do comando de criação de pacote.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.CapabilityCode).NotEmpty().MaximumLength(100);
                item.RuleFor(i => i.CapabilityName).NotEmpty().MaximumLength(200);
                item.RuleFor(i => i.DefaultLimit)
                    .GreaterThan(0)
                    .When(i => i.DefaultLimit.HasValue);
            });
        }
    }

    /// <summary>
    /// Handler que cria um pacote de funcionalidades com seus itens.
    /// Valida unicidade do código antes de persistir.
    /// </summary>
    public sealed class Handler(
        IFeaturePackRepository featurePackRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await featurePackRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (existing is not null)
            {
                return CommercialCatalogErrors.FeaturePackCodeAlreadyExists(request.Code);
            }

            var featurePack = FeaturePack.Create(
                request.Code,
                request.Name,
                request.Description);

            foreach (var item in request.Items)
            {
                featurePack.AddItem(item.CapabilityCode, item.CapabilityName, item.DefaultLimit);
            }

            await featurePackRepository.AddAsync(featurePack, cancellationToken);

            return new Response(featurePack.Id.Value, featurePack.Code, featurePack.Name);
        }
    }

    /// <summary>Resposta da criação de pacote de funcionalidades.</summary>
    public sealed record Response(Guid FeaturePackId, string Code, string Name);
}
