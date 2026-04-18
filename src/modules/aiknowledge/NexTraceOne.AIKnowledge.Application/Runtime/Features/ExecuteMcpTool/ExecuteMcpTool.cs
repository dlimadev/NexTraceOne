using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteMcpTool;

/// <summary>
/// Feature: ExecuteMcpTool — executa uma tool através do protocolo MCP com governança integrada.
/// Valida existência da tool, executa via IToolExecutor e retorna resultado no formato MCP.
/// A governança de quota é gerida a nível do endpoint (rate limiting).
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ExecuteMcpTool
{
    /// <summary>Comando de execução de uma tool MCP.</summary>
    public sealed record Command(
        string ToolName,
        string ArgumentsJson) : ICommand<Response>;

    /// <summary>Valida o comando de execução de tool MCP.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ToolName)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Tool name is required and must not exceed 200 characters.");

            RuleFor(x => x.ArgumentsJson)
                .MaximumLength(32_768)
                .WithMessage("Arguments payload must not exceed 32 KB.");
        }
    }

    /// <summary>Handler que executa a tool e retorna resultado no formato MCP.</summary>
    public sealed class Handler(
        IToolRegistry toolRegistry,
        IToolExecutor toolExecutor) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // ── Verificar se a tool existe ───────────────────────────────
            if (!toolRegistry.Exists(request.ToolName))
                return Error.NotFound(
                    "AI.Mcp.ToolNotFound",
                    "Tool '{0}' is not registered in the MCP server.",
                    request.ToolName);

            // ── Executar a tool via executor central ─────────────────────
            var toolResult = await toolExecutor.ExecuteAsync(
                new ToolCallRequest(request.ToolName, request.ArgumentsJson ?? "{}"),
                cancellationToken);

            // ── Mapear para formato MCP content ──────────────────────────
            var contentText = toolResult.Success
                ? toolResult.Output
                : $"Tool execution failed: {toolResult.ErrorMessage}";

            return new Response(
                Content: [new McpContentItem("text", contentText)],
                IsError: !toolResult.Success,
                ToolName: toolResult.ToolName,
                DurationMs: toolResult.DurationMs);
        }
    }

    /// <summary>Resposta no formato MCP de resultado de tool call.</summary>
    public sealed record Response(
        IReadOnlyList<McpContentItem> Content,
        bool IsError,
        string ToolName,
        long DurationMs);

    /// <summary>Item de conteúdo retornado pelo tool call MCP.</summary>
    public sealed record McpContentItem(
        string Type,
        string Text);
}
