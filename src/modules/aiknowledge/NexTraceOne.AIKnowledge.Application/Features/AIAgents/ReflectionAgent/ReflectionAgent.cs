using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions.SemanticKernel;
using NexTraceOne.AIKnowledge.Application.Runtime.Utils;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Features.AIAgents.ReflectionAgent;

/// <summary>
/// Agente reflexivo que executa tarefas complexas com iteração Plan → Execute → Reflect → Revise.
/// Máximo 3 ciclos por padrão. Fail-open: retorna o melhor resultado obtido.
/// </summary>
public static class ReflectionAgent
{
    public const int DefaultMaxIterations = 3;
    public const int ScoreThreshold = 80;

    public sealed record Command(
        string Task,
        string? PreferredAgent = null,
        int? MaxIterations = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Task).NotEmpty().MaximumLength(5000);
            RuleFor(x => x.PreferredAgent).MaximumLength(100).When(x => x.PreferredAgent is not null);
            RuleFor(x => x.MaxIterations).InclusiveBetween(1, 10).When(x => x.MaxIterations.HasValue);
        }
    }

    public sealed record Response(
        string FinalOutput,
        int IterationCount,
        IReadOnlyList<IterationResult> History,
        bool WasRevised,
        int FinalScore,
        string SessionId);

    public sealed record IterationResult(
        int IterationNumber,
        string Plan,
        string ExecutionOutput,
        string Reflection,
        int Score,
        string Decision);

    internal sealed class Handler(
        IAiKernelService kernelService,
        IAiExecutionGateway aiExecutionGateway,
        IDateTimeProvider clock,
        ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var aiPlan = await aiExecutionGateway.PreviewExecutionAsync(
                new AiExecutionRequest(
                    FeatureKey: "aiknowledge.agent.reflection",
                    RequestType: "agent"),
                cancellationToken);

            if (!aiPlan.IsAvailable)
            {
                return Error.Business("AI.NotAvailable", aiPlan.UnavailabilityReason ?? "IA indisponível.");
            }

            var session = AgentReflectionSession.Start(
                request.Task,
                request.MaxIterations ?? DefaultMaxIterations);

            var stopwatch = Stopwatch.StartNew();
            string? bestOutput = null;
            var bestScore = 0;
            var iterations = new List<IterationResult>();

            try
            {
                var kernel = kernelService.CreateKernel(aiPlan.ProviderId, aiPlan.ModelId);
                kernel.Data["GroundingQuery"] = request.Task;

                for (var i = 1; i <= session.MaxIterations; i++)
                {
                    logger.LogInformation("ReflectionAgent iteration {Iteration}/{Max} for session {Session}",
                        i, session.MaxIterations, session.CorrelationId);

                    // ── Plan ───────────────────────────────────────────────
                    var plan = await GeneratePlanAsync(kernelService, kernel, request, iterations, cancellationToken);

                    // ── Execute ────────────────────────────────────────────
                    var executionOutput = await ExecutePlanAsync(kernelService, kernel, request, plan, cancellationToken);

                    // ── Reflect ────────────────────────────────────────────
                    var (reflection, score, decision) = await ReflectAsync(
                        kernelService, kernel, request, plan, executionOutput, cancellationToken);

                    var iterationDuration = stopwatch.ElapsedMilliseconds;
                    var iteration = new ReflectionIteration(
                        i, plan, executionOutput, reflection, score, decision, iterationDuration);
                    session.RecordIteration(iteration);

                    iterations.Add(new IterationResult(
                        i, plan, executionOutput, reflection, score, decision.ToString()));

                    // Track best output
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestOutput = executionOutput;
                    }

                    if (decision == ReflectionDecision.Complete || score >= ScoreThreshold)
                    {
                        stopwatch.Stop();
                        session.Complete(bestOutput ?? executionOutput, bestScore, stopwatch.ElapsedMilliseconds);

                        logger.LogInformation(
                            "ReflectionAgent completed in {Iterations} iterations with score {Score}",
                            i, bestScore);

                        return new Response(
                            bestOutput ?? executionOutput,
                            i,
                            iterations,
                            session.WasRevised,
                            bestScore,
                            session.CorrelationId);
                    }

                    // If this is the last iteration, return best result
                    if (i == session.MaxIterations)
                    {
                        stopwatch.Stop();
                        session.Complete(bestOutput ?? executionOutput, bestScore, stopwatch.ElapsedMilliseconds);
                        break;
                    }
                }

                return new Response(
                    bestOutput ?? "No satisfactory result obtained",
                    iterations.Count,
                    iterations,
                    session.WasRevised,
                    bestScore,
                    session.CorrelationId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "ReflectionAgent failed for task: {Task}", request.Task[..Math.Min(100, request.Task.Length)]);
                session.Fail(ex.Message, stopwatch.ElapsedMilliseconds);

                return new Response(
                    bestOutput ?? $"Execution failed: {ex.Message}",
                    iterations.Count,
                    iterations,
                    session.WasRevised,
                    bestScore,
                    session.CorrelationId);
            }
        }

        private static async Task<string> GeneratePlanAsync(
            IAiKernelService kernelService, Kernel kernel, Command request,
            IReadOnlyList<IterationResult> previousIterations,
            CancellationToken ct)
        {
            var systemPrompt = """
                You are a planning expert. Given a task, generate a concise step-by-step plan.
                Respond with ONLY the plan text, no JSON, no markdown. 2-5 steps maximum.
                Each step should be a single sentence.
                """;

            var userPrompt = $"Task: {request.Task}\n";
            if (previousIterations.Count > 0)
            {
                var last = previousIterations[^1];
                userPrompt += $"\nPrevious attempt scored {last.Score}/100. Feedback: {last.Reflection}\n";
                userPrompt += "Please generate an improved plan addressing the feedback.\n";
            }

            var messages = new List<ChatMessage> { new("user", userPrompt) };
            return await kernelService.ExecuteChatAsync(
                kernel, systemPrompt, messages, ct);
        }

        private static async Task<string> ExecutePlanAsync(
            IAiKernelService kernelService, Kernel kernel, Command request, string plan,
            CancellationToken ct)
        {
            var systemPrompt = $"""
                You are an execution agent. Follow the plan below precisely to accomplish the task.
                Provide a complete, well-structured response.

                Plan:
                {plan}
                """;

            var messages = new List<ChatMessage> { new("user", request.Task) };
            return await kernelService.ExecuteChatAsync(
                kernel, systemPrompt, messages, ct);
        }

        private static async Task<(string Reflection, int Score, ReflectionDecision Decision)> ReflectAsync(
            IAiKernelService kernelService, Kernel kernel, Command request, string plan, string executionOutput,
            CancellationToken ct)
        {
            var systemPrompt = """
                You are a critical evaluator. Analyze the execution result against the original task.
                Respond ONLY with valid JSON. No markdown, no explanations.

                Expected JSON format:
                {
                  "score": 75,
                  "reflection": "Detailed critique of what was done well and what needs improvement",
                  "decision": "complete|revise"
                }
                Score 0-100. Use "complete" if result is fully satisfactory (score >= 80). Use "revise" otherwise.
                """;

            var userPrompt = $"""
                Original task: {request.Task}

                Plan executed:
                {plan}

                Execution result:
                {executionOutput}
                """;

            var messages = new List<ChatMessage> { new("user", userPrompt) };
            var response = await kernelService.ExecuteChatAsync(
                kernel, systemPrompt, messages, ct);

            if (LlmJsonParser.TryParse<ReflectionLlmOutput>(response, out var parsed) && parsed is not null)
            {
                var score = parsed.Score is >= 0 and <= 100 ? parsed.Score : 50;
                var decision = score >= ScoreThreshold || string.Equals(parsed.Decision, "complete", StringComparison.OrdinalIgnoreCase)
                    ? ReflectionDecision.Complete
                    : ReflectionDecision.Revise;

                return (parsed.Reflection ?? "No reflection provided.", score, decision);
            }

            return ("Unable to parse reflection. Proceeding with best effort.", 50, ReflectionDecision.Revise);
        }

        private static Result<Response> CreateFallbackResponse(Command request, string sessionId)
        {
            return new Response(
                $"Unable to process task: {request.Task[..Math.Min(200, request.Task.Length)]}",
                0,
                new List<IterationResult>(),
                false,
                0,
                sessionId);
        }
    }

    private sealed class ReflectionLlmOutput
    {
        public int Score { get; set; }
        public string? Reflection { get; set; }
        public string? Decision { get; set; }
    }
}
