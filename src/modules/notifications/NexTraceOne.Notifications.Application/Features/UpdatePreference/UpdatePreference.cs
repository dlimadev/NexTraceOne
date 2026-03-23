using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Features.UpdatePreference;

/// <summary>
/// Feature: UpdatePreference — atualiza a preferência de notificação do utilizador autenticado.
/// Valida que a categoria e canal são enums válidos.
/// Rejeita tentativas de desativar notificações obrigatórias.
/// </summary>
public static class UpdatePreference
{
    /// <summary>Comando para atualizar uma preferência de notificação.</summary>
    public sealed record Command(
        string Category,
        string Channel,
        bool Enabled) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Category)
                .NotEmpty()
                .Must(c => Enum.TryParse<NotificationCategory>(c, ignoreCase: true, out _))
                .WithMessage("Invalid notification category.");

            RuleFor(x => x.Channel)
                .NotEmpty()
                .Must(c => Enum.TryParse<DeliveryChannel>(c, ignoreCase: true, out _))
                .WithMessage("Invalid delivery channel.");
        }
    }

    /// <summary>Handler que atualiza a preferência do utilizador após validações de negócio.</summary>
    public sealed class Handler(
        INotificationPreferenceService preferenceService,
        IMandatoryNotificationPolicy mandatoryPolicy,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Guid.TryParse(currentUser.Id, out var userId))
                return Error.Unauthorized(
                    "Notification.InvalidUserId",
                    "Current user identifier is not a valid GUID.");

            var category = Enum.Parse<NotificationCategory>(request.Category, ignoreCase: true);
            var channel = Enum.Parse<DeliveryChannel>(request.Channel, ignoreCase: true);

            // Rejeitar desativação de canais obrigatórios
            if (!request.Enabled)
            {
                // Verificar se o canal está na lista de canais obrigatórios para esta categoria.
                // Usa Critical como severidade de referência — se é obrigatório para Critical, não pode ser desativado.
                var mandatoryChannels = mandatoryPolicy.GetMandatoryChannels(
                    string.Empty, category, NotificationSeverity.Critical);

                if (mandatoryChannels.Contains(channel))
                {
                    return Error.Validation(
                        "Notification.MandatoryChannel",
                        "Cannot disable mandatory notification channel {0} for category {1}.",
                        channel.ToString(), category.ToString());
                }
            }

            await preferenceService.UpdatePreferenceAsync(
                currentTenant.Id, userId, category, channel, request.Enabled, cancellationToken);

            return new Response(true);
        }
    }

    /// <summary>Resposta do comando de atualização.</summary>
    public sealed record Response(bool Success);
}
