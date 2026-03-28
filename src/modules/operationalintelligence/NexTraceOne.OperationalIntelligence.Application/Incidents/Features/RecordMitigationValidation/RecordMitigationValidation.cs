using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;

/// <summary>
/// Feature: RecordMitigationValidation — persiste o resultado de uma validação pós-mitigação,
/// incluindo o estado, resultado observado e verificações individuais.
/// </summary>
public static class RecordMitigationValidation
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Comando para registar a validação de um workflow de mitigação.</summary>
    public sealed record Command(
        string IncidentId,
        string WorkflowId,
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<ValidationCheckInput>? Checks) : ICommand<Response>;

    /// <summary>Entrada de verificação individual para registo de validação.</summary>
    public sealed record ValidationCheckInput(string CheckName, bool IsPassed, string? ObservedValue);

    /// <summary>Valida os campos obrigatórios do comando de registo de validação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Status).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que persiste a validação do workflow de mitigação via repositório dedicado.
    /// Usa IIncidentStore apenas para verificar a existência do incidente.
    /// </summary>
    public sealed class Handler(
        IIncidentStore store,
        IMitigationValidationRepository validationRepository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!store.IncidentExists(request.IncidentId))
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            if (!Guid.TryParse(request.WorkflowId, out var wfGuid))
                return IncidentErrors.IncidentNotFound(request.WorkflowId);

            var validatedAt = clock.UtcNow;

            var checksJson = request.Checks is { Count: > 0 }
                ? JsonSerializer.Serialize(
                    request.Checks.Select(c => new ValidationCheckJson
                    {
                        CheckName = c.CheckName,
                        IsPassed = c.IsPassed,
                        ObservedValue = c.ObservedValue,
                    }).ToList(),
                    JsonOptions)
                : null;

            var log = MitigationValidationLog.Create(
                MitigationValidationLogId.New(),
                request.IncidentId,
                wfGuid,
                request.Status,
                request.ObservedOutcome,
                request.ValidatedBy,
                validatedAt,
                checksJson);

            await validationRepository.AddAsync(log, cancellationToken);

            return Result<Response>.Success(new Response(wfGuid, request.Status, validatedAt));
        }
    }

    /// <summary>Resposta do registo de validação do workflow de mitigação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus Status,
        DateTimeOffset ValidatedAt);

    private sealed class ValidationCheckJson
    {
        [JsonPropertyName("checkName")] public string CheckName { get; set; } = string.Empty;
        [JsonPropertyName("isPassed")] public bool IsPassed { get; set; }
        [JsonPropertyName("observedValue")] public string? ObservedValue { get; set; }
    }
}
