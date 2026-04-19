using System.Text.Json;
using System.Text.Json.Serialization;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ExportPendingTrajectories;

/// <summary>
/// Feature: ExportPendingTrajectories — exporta trajectórias com feedback para ficheiros JSON.
/// Usado pelo TrajectoryExporterJob a cada 15 minutos.
/// Cada ficheiro representa uma trajectória completa (execução + feedback) no formato esperado pelo trainer RL.
/// Estrutura VSA: Command + Handler + Response num único ficheiro.
/// </summary>
public static class ExportPendingTrajectories
{
    /// <summary>Comando de exportação batch de trajectórias pendentes.</summary>
    public sealed record Command(
        int MaxBatch,
        string ExportDirectoryPath,
        Guid? TenantId) : ICommand<Response>;

    /// <summary>Handler que exporta as trajectórias pendentes para ficheiros JSON.</summary>
    public sealed class Handler(
        IAiAgentTrajectoryFeedbackRepository feedbackRepository,
        IAiAgentExecutionRepository executionRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var pending = await feedbackRepository.ListPendingExportAsync(
                request.MaxBatch, cancellationToken);

            if (pending.Count == 0)
                return new Response(0, []);

            if (!Directory.Exists(request.ExportDirectoryPath))
                Directory.CreateDirectory(request.ExportDirectoryPath);

            var exportedFiles = new List<string>();

            foreach (var feedback in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var execution = await executionRepository.GetByIdAsync(
                    feedback.ExecutionId, cancellationToken);

                if (execution is null)
                    continue;

                var document = new TrajectoryDocument(
                    TrajectoryId: feedback.Id.Value.ToString("N"),
                    AgentId: execution.AgentId.Value.ToString("N"),
                    ModelId: execution.ModelIdUsed.ToString("N"),
                    TimestampUtc: feedback.SubmittedAt.UtcDateTime.ToString("O"),
                    Steps: DeserializeSteps(execution.Steps),
                    Feedback: new TrajectoryFeedbackSection(
                        Rating: feedback.Rating,
                        Outcome: feedback.Outcome,
                        WasCorrect: feedback.WasCorrect,
                        TimeToResolveMinutes: feedback.TimeToResolveMinutes));

                var fileName = $"trajectory_{feedback.Id.Value:N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.json";
                var filePath = Path.Combine(request.ExportDirectoryPath, fileName);

                var json = JsonSerializer.Serialize(document, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);

                feedback.MarkExported(DateTimeOffset.UtcNow);
                exportedFiles.Add(filePath);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(exportedFiles.Count, exportedFiles.AsReadOnly());
        }

        private static IReadOnlyList<object?> DeserializeSteps(string? stepsJson)
        {
            if (string.IsNullOrWhiteSpace(stepsJson))
                return [];

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<object?>>(stepsJson) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    /// <summary>Resposta da exportação de trajectórias.</summary>
    public sealed record Response(
        int ExportedCount,
        IReadOnlyList<string> ExportedFiles);
}

/// <summary>Documento de trajectória serializado para o trainer RL externo.</summary>
public sealed record TrajectoryDocument(
    [property: JsonPropertyName("trajectory_id")] string TrajectoryId,
    [property: JsonPropertyName("agent_id")] string AgentId,
    [property: JsonPropertyName("model_id")] string ModelId,
    [property: JsonPropertyName("timestamp_utc")] string TimestampUtc,
    [property: JsonPropertyName("steps")] IReadOnlyList<object?> Steps,
    [property: JsonPropertyName("feedback")] TrajectoryFeedbackSection Feedback);

/// <summary>Secção de feedback do documento de trajectória.</summary>
public sealed record TrajectoryFeedbackSection(
    [property: JsonPropertyName("rating")] int Rating,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("was_correct")] bool WasCorrect,
    [property: JsonPropertyName("time_to_resolve_minutes")] int? TimeToResolveMinutes);
