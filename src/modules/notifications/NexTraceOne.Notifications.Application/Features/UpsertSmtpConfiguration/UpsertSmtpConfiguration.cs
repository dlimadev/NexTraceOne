using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Entities;

namespace NexTraceOne.Notifications.Application.Features.UpsertSmtpConfiguration;

/// <summary>
/// Feature: UpsertSmtpConfiguration — cria ou atualiza a configuração SMTP do tenant.
/// A senha é recebida em claro; a cifra AES-256-GCM é aplicada automaticamente
/// pelo EF Core via [EncryptedField] em SmtpConfiguration.EncryptedPassword.
/// </summary>
public static class UpsertSmtpConfiguration
{
    /// <summary>Comando para criar ou atualizar a configuração SMTP.</summary>
    public sealed record Command(
        string Host,
        int Port,
        bool UseSsl,
        string FromAddress,
        string FromName,
        string? Username,
        string? Password,
        string? BaseUrl,
        bool IsEnabled) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Host).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Port).InclusiveBetween(1, 65535);
            RuleFor(x => x.FromAddress)
                .NotEmpty()
                .MaximumLength(500)
                .EmailAddress()
                .WithMessage("From address must be a valid email.");
            RuleFor(x => x.FromName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.BaseUrl).MaximumLength(2000).When(x => x.BaseUrl is not null);
        }
    }

    /// <summary>Handler que cria ou atualiza a configuração SMTP.</summary>
    public sealed class Handler(
        ISmtpConfigurationStore store,
        ICurrentTenant tenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // A senha é passada em texto claro. O EF Core aplica cifra AES-256-GCM
            // automaticamente via [EncryptedField] em SmtpConfiguration.EncryptedPassword.
            var encryptedPassword = request.Password;

            var existing = await store.GetByTenantAsync(tenant.Id, cancellationToken);

            if (existing is not null)
            {
                // Atualizar servidor SMTP
                existing.UpdateServer(request.Host, request.Port, request.UseSsl);

                // Atualizar credenciais apenas se fornecidas (password null = manter a existente)
                if (request.Password is not null)
                    existing.UpdateCredentials(request.Username, encryptedPassword);
                else
                    existing.UpdateCredentials(request.Username, existing.EncryptedPassword);

                existing.UpdateSender(request.FromAddress, request.FromName, request.BaseUrl);

                if (request.IsEnabled) existing.Enable();
                else existing.Disable();

                await store.SaveChangesAsync(cancellationToken);
                return new Response(existing.Id.Value, false);
            }

            // Criar nova configuração
            var config = SmtpConfiguration.Create(
                tenant.Id,
                request.Host,
                request.Port,
                request.UseSsl,
                request.FromAddress,
                request.FromName,
                request.Username,
                encryptedPassword,
                request.BaseUrl,
                request.IsEnabled);

            await store.AddAsync(config, cancellationToken);
            await store.SaveChangesAsync(cancellationToken);

            return new Response(config.Id.Value, true);
        }
    }

    /// <summary>Resposta do comando de upsert de configuração SMTP.</summary>
    public sealed record Response(Guid Id, bool Created);
}
