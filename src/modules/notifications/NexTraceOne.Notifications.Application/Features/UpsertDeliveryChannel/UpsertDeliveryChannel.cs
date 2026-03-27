using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.UpsertDeliveryChannel;

/// <summary>
/// Feature: UpsertDeliveryChannel — cria ou atualiza a configuração de um canal de entrega.
/// </summary>
public static class UpsertDeliveryChannel
{
    /// <summary>Comando para criar ou atualizar a configuração de um canal.</summary>
    public sealed record Command(
        Guid? Id,
        string ChannelType,
        string DisplayName,
        bool IsEnabled,
        string? ConfigurationJson) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChannelType)
                .NotEmpty()
                .Must(c => Enum.TryParse<DeliveryChannel>(c, ignoreCase: true, out _))
                .WithMessage("Invalid delivery channel type.");

            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que cria ou atualiza a configuração do canal.</summary>
    public sealed class Handler(
        IDeliveryChannelConfigurationStore store,
        ICurrentTenant tenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var channelType = Enum.Parse<DeliveryChannel>(request.ChannelType, ignoreCase: true);

            // Atualizar configuração existente por Id
            if (request.Id.HasValue)
            {
                var existing = await store.GetByIdAsync(
                    new DeliveryChannelConfigurationId(request.Id.Value),
                    cancellationToken);

                if (existing is null)
                    return Error.NotFound(
                        "DeliveryChannelConfiguration.NotFound",
                        "Delivery channel configuration {0} not found.",
                        request.Id.Value.ToString());

                if (existing.TenantId != tenant.Id)
                    return Error.Forbidden(
                        "DeliveryChannelConfiguration.Forbidden",
                        "Access denied to delivery channel configuration {0}.",
                        request.Id.Value.ToString());

                existing.Update(request.DisplayName, request.IsEnabled, request.ConfigurationJson);
                await store.SaveChangesAsync(cancellationToken);
                return new Response(existing.Id.Value, false);
            }

            // Verificar se já existe configuração para este tipo de canal no tenant (upsert por tipo)
            var current = await store.GetByChannelTypeAsync(tenant.Id, channelType, cancellationToken);
            if (current is not null)
            {
                current.Update(request.DisplayName, request.IsEnabled, request.ConfigurationJson);
                await store.SaveChangesAsync(cancellationToken);
                return new Response(current.Id.Value, false);
            }

            // Criar nova configuração
            var config = DeliveryChannelConfiguration.Create(
                tenant.Id,
                channelType,
                request.DisplayName,
                request.IsEnabled,
                request.ConfigurationJson);

            await store.AddAsync(config, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);

            return new Response(config.Id.Value, true);
        }
    }

    /// <summary>Resposta do comando de upsert de canal.</summary>
    public sealed record Response(Guid Id, bool Created);
}
