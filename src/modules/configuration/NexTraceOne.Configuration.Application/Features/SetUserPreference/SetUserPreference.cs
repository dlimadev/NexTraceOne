using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.SetUserPreference;

/// <summary>
/// Feature: SetUserPreference — define ou atualiza uma preferência do utilizador.
/// Persiste no scope User vinculado ao utilizador autenticado.
/// Respeita os limites de personalização da plataforma (sidebar items, widgets, etc.).
/// </summary>
public static class SetUserPreference
{
    /// <summary>Comando para definir uma preferência do utilizador.</summary>
    public sealed record Command(string Key, string Value) : ICommand<PreferenceResponse>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Key).NotEmpty().MaximumLength(256)
                .Must(k => k.StartsWith("platform.", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("user.", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("default.", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("table.", StringComparison.OrdinalIgnoreCase)
                         || k.StartsWith("ui.", StringComparison.OrdinalIgnoreCase))
                .WithMessage("User preferences must start with a recognized prefix (platform., user., default., table., ui.)");
            RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
        }
    }

    /// <summary>Handler que persiste a preferência do utilizador.</summary>
    public sealed class Handler(
        IConfigurationEntryRepository entryRepository,
        IConfigurationDefinitionRepository definitionRepository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, PreferenceResponse>
    {
        public async Task<Result<PreferenceResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated to set preferences.");

            var userId = currentUser.Id;

            // Verify definition exists and allows User scope
            var definition = await definitionRepository.GetByKeyAsync(request.Key, cancellationToken);
            if (definition is null)
                return Error.NotFound("Preference.DefinitionNotFound",
                    $"No configuration definition found for key '{request.Key}'.");

            if (!definition.AllowedScopes.Contains(ConfigurationScope.User))
                return Error.Validation("Preference.ScopeNotAllowed",
                    $"Configuration '{request.Key}' does not allow User scope.");

            // Check if entry already exists for this user
            var existing = await entryRepository.GetByKeyAndScopeAsync(
                request.Key, ConfigurationScope.User, userId, cancellationToken);

            if (existing is not null)
            {
                existing.UpdateValue(
                    value: request.Value,
                    structuredValueJson: null,
                    updatedBy: currentUser.Id,
                    changeReason: $"User preference updated by {currentUser.Name}");
                await entryRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                var entry = ConfigurationEntry.Create(
                    definition.Id,
                    request.Key,
                    ConfigurationScope.User,
                    currentUser.Id,
                    scopeReferenceId: userId,
                    value: request.Value,
                    changeReason: $"User preference set by {currentUser.Name}");

                await entryRepository.AddAsync(entry, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new PreferenceResponse(request.Key, request.Value, userId);
        }
    }

    /// <summary>Resposta da gravação de preferência.</summary>
    public sealed record PreferenceResponse(string Key, string Value, string UserId);
}
