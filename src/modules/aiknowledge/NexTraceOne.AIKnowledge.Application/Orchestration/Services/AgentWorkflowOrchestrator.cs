using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Services;

/// <summary>
/// Implementação do orquestrador multi-agent com suporte a sequencial e paralelo.
/// Steps com o mesmo ParallelGroupId são executados em paralelo (fan-out/fan-in).
/// Retry: até 3 tentativas com backoff exponencial (1s, 2s, 4s).
/// Persistência: todas as execuções são auditadas em AgentWorkflowExecution.
/// </summary>
public sealed class AgentWorkflowOrchestrator : IAgentWorkflowOrchestrator
{
    private readonly IAiAgentRuntimeService _runtimeService;
    private readonly IAgentWorkflowExecutionRepository _executionRepository;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AgentWorkflowOrchestrator> _logger;
    private readonly IWorkflowReplanningService? _replanningService;

    private const int MaxRetries = 3;
    private const int BaseRetryDelayMs = 1000;
    private const int DefaultStepTimeoutSeconds = 120;

    public AgentWorkflowOrchestrator(
        IAiAgentRuntimeService runtimeService,
        IAgentWorkflowExecutionRepository executionRepository,
        IDateTimeProvider clock,
        ILogger<AgentWorkflowOrchestrator> logger,
        IWorkflowReplanningService? replanningService = null)
    {
        _runtimeService = runtimeService;
        _executionRepository = executionRepository;
        _clock = clock;
        _logger = logger;
        _replanningService = replanningService;
    }

    public async Task<Result<AgentWorkflowResult>> ExecuteSequentialAsync(
        AgentWorkflowDefinition workflow,
        string initialInput,
        string? callerTeamId = null,
        bool enableAdaptiveReplanning = false,
        CancellationToken ct = default)
    {
        if (workflow.Steps.Count == 0)
        {
            return new Error("Workflow.Empty", "Workflow must contain at least one step.", ErrorType.Validation);
        }

        var execution = AgentWorkflowExecution.Start(
            workflow.Name,
            initialInput,
            workflow.Steps.Count,
            _clock.UtcNow,
            callerTeamId);

        await _executionRepository.AddAsync(execution, ct);

        var parallelGroups = GroupStepsByParallelism(workflow.Steps);

        _logger.LogInformation(
            "Starting multi-agent workflow '{WorkflowName}' with {StepCount} steps in {GroupCount} groups. Replanning={Replanning}. ExecutionId={ExecutionId}",
            workflow.Name, workflow.Steps.Count, parallelGroups.Count, enableAdaptiveReplanning, execution.Id.Value);

        var stepResults = new List<AgentWorkflowStepResult>(workflow.Steps.Count);
        var currentInput = initialInput;
        var overallStopwatch = Stopwatch.StartNew();
        var totalRetries = 0;
        var globalStepIndex = 0;
        var hasReplanned = false;

        while (globalStepIndex < workflow.Steps.Count)
        {
            var remainingGroups = parallelGroups.Skip(globalStepIndex / workflow.Steps.Count * parallelGroups.Count).ToList();
            // Recalculate groups from current position
            var remainingSteps = workflow.Steps.Skip(globalStepIndex).ToList();
            var groupsFromHere = GroupStepsByParallelism(remainingSteps);

            foreach (var group in groupsFromHere)
            {
                var isParallel = group.Count > 1;

                if (isParallel)
                {
                    _logger.LogDebug(
                        "Workflow '{WorkflowName}': executing parallel group with {StepCount} steps. ExecutionId={ExecutionId}",
                        workflow.Name, group.Count, execution.Id.Value);
                }

                var groupResult = await ExecuteGroupAsync(
                    workflow.Name, group, currentInput, globalStepIndex, callerTeamId, isParallel, ct);

                stepResults.AddRange(groupResult.Results);
                totalRetries += groupResult.TotalRetries;
                globalStepIndex += group.Count;

                if (!groupResult.Success)
                {
                    // Adaptive replanning attempt
                    if (enableAdaptiveReplanning && !hasReplanned && _replanningService is not null)
                    {
                        var failedStep = group.FirstOrDefault(s => stepResults.LastOrDefault(r => r.AgentId == s.AgentId && !r.Success) is not null)
                            ?? group[0];

                        _logger.LogInformation(
                            "Workflow '{WorkflowName}' failed at step {StepIndex}. Attempting adaptive replanning. ExecutionId={ExecutionId}",
                            workflow.Name, globalStepIndex, execution.Id.Value);

                        var replanned = await _replanningService.ReplanAsync(
                            workflow,
                            stepResults.Where(r => r.Success).ToList(),
                            failedStep,
                            groupResult.ErrorMessage ?? "Unknown error",
                            currentInput,
                            ct);

                        if (replanned is not null && replanned.Steps.Count > 0)
                        {
                            hasReplanned = true;
                            workflow = replanned;
                            parallelGroups = GroupStepsByParallelism(workflow.Steps);
                            // Keep completed results, continue with new plan from beginning
                            globalStepIndex = 0;
                            totalRetries = 0;

                            _logger.LogInformation(
                                "Workflow '{WorkflowName}' successfully replanned with {StepCount} steps. Continuing execution. ExecutionId={ExecutionId}",
                                workflow.Name, workflow.Steps.Count, execution.Id.Value);

                            break; // Break inner loop to restart with new plan
                        }
                    }

                    overallStopwatch.Stop();
                    execution.RecordProgress(
                        SerializeStepResults(stepResults),
                        stepResults.Count(r => r.Success),
                        totalRetries,
                        currentInput);
                    execution.Fail(
                        groupResult.ErrorMessage ?? "Unknown error",
                        overallStopwatch.ElapsedMilliseconds,
                        _clock.UtcNow);
                    await _executionRepository.UpdateAsync(execution, ct);

                    _logger.LogWarning(
                        "Workflow '{WorkflowName}' failed in group with {RetryCount} retries. ExecutionId={ExecutionId}",
                        workflow.Name, groupResult.TotalRetries, execution.Id.Value);

                    return new Error(
                        "Workflow.StepFailed",
                        $"Workflow failed after {groupResult.TotalRetries} retries: {groupResult.ErrorMessage}",
                        ErrorType.Business);
                }

                currentInput = groupResult.CombinedOutput;

                if (isParallel)
                {
                    _logger.LogDebug(
                        "Workflow '{WorkflowName}': parallel group completed. Combined output length: {OutputLength}. ExecutionId={ExecutionId}",
                        workflow.Name, currentInput.Length, execution.Id.Value);
                }
            }

            if (globalStepIndex >= workflow.Steps.Count)
                break;
        }

        overallStopwatch.Stop();

        execution.RecordProgress(
            SerializeStepResults(stepResults),
            stepResults.Count,
            totalRetries,
            currentInput);
        execution.Complete(overallStopwatch.ElapsedMilliseconds, _clock.UtcNow);
        await _executionRepository.UpdateAsync(execution, ct);

        _logger.LogInformation(
            "Workflow '{WorkflowName}' completed in {ElapsedMs}ms. {SuccessCount}/{TotalCount} steps succeeded. TotalRetries={TotalRetries}. ExecutionId={ExecutionId}",
            workflow.Name, overallStopwatch.ElapsedMilliseconds,
            stepResults.Count, stepResults.Count, totalRetries, execution.Id.Value);

        return new AgentWorkflowResult(
            Success: true,
            StepResults: stepResults,
            FinalOutput: currentInput);
    }

    private static List<List<AgentWorkflowStep>> GroupStepsByParallelism(IReadOnlyList<AgentWorkflowStep> steps)
    {
        var groups = new List<List<AgentWorkflowStep>>();
        var currentGroup = new List<AgentWorkflowStep>();
        int? currentGroupId = null;

        foreach (var step in steps)
        {
            if (step.ParallelGroupId.HasValue)
            {
                if (currentGroupId == step.ParallelGroupId.Value)
                {
                    currentGroup.Add(step);
                }
                else
                {
                    if (currentGroup.Count > 0) groups.Add(currentGroup);
                    currentGroup = [step];
                    currentGroupId = step.ParallelGroupId.Value;
                }
            }
            else
            {
                if (currentGroup.Count > 0) groups.Add(currentGroup);
                groups.Add([step]);
                currentGroup = [];
                currentGroupId = null;
            }
        }

        if (currentGroup.Count > 0) groups.Add(currentGroup);
        return groups;
    }

    private async Task<GroupResult> ExecuteGroupAsync(
        string workflowName,
        IReadOnlyList<AgentWorkflowStep> group,
        string input,
        int globalStepIndex,
        string? callerTeamId,
        bool isParallel,
        CancellationToken ct)
    {
        if (!isParallel)
        {
            var result = await ExecuteStepWithRetryAsync(
                workflowName, group[0], input, globalStepIndex, callerTeamId, ct);
            return new GroupResult(
                Success: result.Success,
                Results: [result],
                CombinedOutput: result.Output,
                TotalRetries: result.RetryCount,
                ErrorMessage: result.ErrorMessage);
        }

        // Parallel execution: all steps in the group receive the same input
        // Use a shared timeout for the entire group (max of individual timeouts, or default)
        var groupTimeoutSeconds = group.Max(s => s.StepTimeoutSeconds) ?? DefaultStepTimeoutSeconds;
        using var groupCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        groupCts.CancelAfter(TimeSpan.FromSeconds(groupTimeoutSeconds));

        var tasks = group.Select((step, idx) =>
            ExecuteStepWithRetryAsync(workflowName, step, input, globalStepIndex + idx, callerTeamId, groupCts.Token));

        try
        {
            var results = await Task.WhenAll(tasks);

            var failed = results.FirstOrDefault(r => !r.Success);
            if (failed is not null)
            {
                return new GroupResult(
                    Success: false,
                    Results: results.ToList(),
                    CombinedOutput: string.Empty,
                    TotalRetries: results.Sum(r => r.RetryCount),
                    ErrorMessage: failed.ErrorMessage);
            }

            var combinedOutput = string.Join("\n\n", results.Select(r => $"[{r.AgentName}]\n{r.Output}"));
            return new GroupResult(
                Success: true,
                Results: results.ToList(),
                CombinedOutput: combinedOutput,
                TotalRetries: results.Sum(r => r.RetryCount),
                ErrorMessage: null);
        }
        catch (OperationCanceledException) when (groupCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Workflow '{WorkflowName}' parallel group timed out after {TimeoutSeconds}s. ExecutionId={ExecutionId}",
                workflowName, groupTimeoutSeconds, Guid.NewGuid());

            return new GroupResult(
                Success: false,
                Results: [],
                CombinedOutput: string.Empty,
                TotalRetries: 0,
                ErrorMessage: $"Parallel group timed out after {groupTimeoutSeconds}s");
        }
    }

    private async Task<AgentWorkflowStepResult> ExecuteStepWithRetryAsync(
        string workflowName,
        AgentWorkflowStep step,
        string previousOutput,
        int stepIndex,
        string? callerTeamId,
        CancellationToken ct)
    {
        var stepInput = BuildStepInput(step, previousOutput, stepIndex);
        var retryCount = 0;
        Exception? lastException = null;
        var timeoutSeconds = step.StepTimeoutSeconds ?? DefaultStepTimeoutSeconds;

        while (retryCount <= MaxRetries)
        {
            var stepStopwatch = Stopwatch.StartNew();
            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            stepCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                var agentId = new AiAgentId(step.AgentId);
                var result = await _runtimeService.ExecuteAsync(
                    agentId,
                    stepInput,
                    modelIdOverride: null,
                    contextJson: null,
                    callerTeamId: callerTeamId,
                    cancellationToken: stepCts.Token);

                stepStopwatch.Stop();

                if (result.IsSuccess)
                {
                    return new AgentWorkflowStepResult(
                        step.AgentId,
                        result.Value.AgentName,
                        stepInput,
                        result.Value.Output,
                        stepStopwatch.ElapsedMilliseconds,
                        Success: true,
                        RetryCount: retryCount);
                }

                lastException = new InvalidOperationException(result.Error.Message);
                _logger.LogWarning(
                    "Workflow '{WorkflowName}' step {StepIndex} attempt {Attempt}/{MaxAttempts} failed: {Error}",
                    workflowName, stepIndex + 1, retryCount + 1, MaxRetries + 1, result.Error.Message);
            }
            catch (OperationCanceledException ex) when (stepCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                stepStopwatch.Stop();
                lastException = new TimeoutException($"Step timed out after {timeoutSeconds}s", ex);
                _logger.LogWarning(
                    "Workflow '{WorkflowName}' step {StepIndex} attempt {Attempt}/{MaxAttempts} timed out after {TimeoutSeconds}s",
                    workflowName, stepIndex + 1, retryCount + 1, MaxRetries + 1, timeoutSeconds);
            }
            catch (Exception ex)
            {
                stepStopwatch.Stop();
                lastException = ex;
                _logger.LogWarning(ex,
                    "Workflow '{WorkflowName}' step {StepIndex} attempt {Attempt}/{MaxAttempts} crashed",
                    workflowName, stepIndex + 1, retryCount + 1, MaxRetries + 1);
            }

            retryCount++;
            if (retryCount <= MaxRetries)
            {
                var delay = BaseRetryDelayMs * (1 << (retryCount - 1));
                _logger.LogDebug(
                    "Retrying workflow '{WorkflowName}' step {StepIndex} in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
                    workflowName, stepIndex + 1, delay, retryCount + 1, MaxRetries + 1);
                await Task.Delay(delay, ct);
            }
        }

        return new AgentWorkflowStepResult(
            step.AgentId,
            "Unknown",
            stepInput,
            string.Empty,
            0,
            Success: false,
            ErrorMessage: lastException?.Message ?? "Step failed after maximum retries",
            RetryCount: retryCount - 1);
    }

    private static string BuildStepInput(AgentWorkflowStep step, string previousOutput, int stepIndex)
    {
        if (string.IsNullOrWhiteSpace(step.InputTemplate))
            return previousOutput;

        return step.InputTemplate
            .Replace("{previousOutput}", previousOutput, StringComparison.Ordinal)
            .Replace("{stepIndex}", stepIndex.ToString(), StringComparison.Ordinal);
    }

    private static string SerializeStepResults(IReadOnlyList<AgentWorkflowStepResult> results)
    {
        try
        {
            return JsonSerializer.Serialize(results, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch
        {
            return "[]";
        }
    }

    private sealed record GroupResult(
        bool Success,
        IReadOnlyList<AgentWorkflowStepResult> Results,
        string CombinedOutput,
        int TotalRetries,
        string? ErrorMessage);
}
