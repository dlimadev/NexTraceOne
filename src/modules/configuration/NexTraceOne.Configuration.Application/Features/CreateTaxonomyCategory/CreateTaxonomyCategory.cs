using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateTaxonomyCategory;

/// <summary>Feature: CreateTaxonomyCategory — cria uma categoria de taxonomia para o tenant.</summary>
public static class CreateTaxonomyCategory
{
    public sealed record Command(
        string TenantId,
        string Name,
        string Description,
        bool IsRequired) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        }
    }

    public sealed class Handler(
        ITaxonomyRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var category = TaxonomyCategory.Create(request.TenantId, request.Name, request.Description, request.IsRequired, clock.UtcNow);
            await repository.AddCategoryAsync(category, cancellationToken);
            return Result<Response>.Success(new Response(category.Id.Value, category.Name, category.IsRequired));
        }
    }

    public sealed record Response(Guid CategoryId, string Name, bool IsRequired);
}
