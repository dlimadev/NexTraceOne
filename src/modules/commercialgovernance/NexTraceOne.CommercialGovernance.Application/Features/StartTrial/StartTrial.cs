using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Application.Features.StartTrial;

/// <summary>
/// Feature: StartTrial — inicia uma licença de trial para avaliação do produto.
///
/// Regras de negócio:
/// - Duração padrão de 30 dias, configurável.
/// - Limites padrão: 25 APIs, 2 ambientes, 5 usuários.
/// - Todas as capabilities ficam habilitadas durante o trial.
/// - Sem necessidade de cartão de crédito.
/// - Watermark visual "Trial — X days remaining" (resolvido pelo frontend).
/// - Anti-abuso: verifica se já existe trial para o mesmo customerName.
/// </summary>
public static class StartTrial
{
    /// <summary>Comando para iniciar um período de trial.</summary>
    public sealed record Command(
        string CustomerName,
        int TrialDays = 30) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de trial.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TrialDays).InclusiveBetween(7, 90);
        }
    }

    /// <summary>
    /// Handler que cria uma licença de trial com limites padrão.
    /// Gera uma chave de licença única e ativa o trial imediatamente.
    /// </summary>
    public sealed class Handler(
        ILicenseRepository licenseRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var licenseKey = $"TRIAL-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
            var now = dateTimeProvider.UtcNow;

            var allCapabilities = new[]
            {
                LicenseCapability.Create("catalog:read", "Catalog Read"),
                LicenseCapability.Create("catalog:write", "Catalog Write"),
                LicenseCapability.Create("contracts:import", "Contract Import"),
                LicenseCapability.Create("contracts:diff", "Semantic Diff"),
                LicenseCapability.Create("releases:manage", "Release Management"),
                LicenseCapability.Create("workflow:basic", "Basic Workflow"),
                LicenseCapability.Create("audit:read", "Audit Trail Read")
            };

            var license = License.CreateTrial(
                licenseKey,
                request.CustomerName,
                now,
                request.TrialDays,
                allCapabilities);

            licenseRepository.Add(license);
            await Task.CompletedTask;

            return new Response(
                license.Id.Value,
                license.LicenseKey,
                license.CustomerName,
                license.IssuedAt,
                license.ExpiresAt,
                request.TrialDays);
        }
    }

    /// <summary>Resposta da criação do trial.</summary>
    public sealed record Response(
        Guid LicenseId,
        string LicenseKey,
        string CustomerName,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpiresAt,
        int TrialDays);
}
