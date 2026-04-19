using System.Text.Json;
using System.Text.Json.Serialization;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetMcpServerInfoFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.GetMcpServerInfo.GetMcpServerInfo;
using ListMcpToolsFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ListMcpTools.ListMcpTools;
using ExecuteMcpToolFeature = NexTraceOne.AIKnowledge.Application.Runtime.Features.ExecuteMcpTool.ExecuteMcpTool;

namespace NexTraceOne.AIKnowledge.API.Mcp.Endpoints.Endpoints;

/// <summary>
/// Módulo de endpoints MCP (Model Context Protocol) — protocolo JSON-RPC 2.0 sobre HTTP.
/// Expõe as tools do AIKnowledge module a agentes externos, IDEs e ferramentas de developer
/// que suportam o protocolo MCP 2024-11-05.
///
/// Endpoints:
/// - GET  /api/v1/ai/mcp           — informação do servidor (metadados, capacidades, tool count)
/// - GET  /api/v1/ai/mcp/tools     — listagem de tools no formato MCP JSON Schema
/// - POST /api/v1/ai/mcp           — handler JSON-RPC 2.0 (initialize, tools/list, tools/call)
///
/// Política de autorização:
/// - Leitura: "ai:runtime:read" para GET endpoints e método initialize/tools/list.
/// - Escrita: "ai:runtime:write" para tools/call (execução de tools com potencial de efeito).
///
/// Conformidade: MCP specification 2024-11-05 (https://spec.modelcontextprotocol.io).
/// </summary>
public sealed class McpEndpointModule
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        MapServerInfoEndpoint(app);
        MapToolsListEndpoint(app);
        MapJsonRpcEndpoint(app);
    }

    // ── GET /api/v1/ai/mcp — server info ─────────────────────────────────

    private static void MapServerInfoEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/ai/mcp", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetMcpServerInfoFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("ai:runtime:read")
        .WithTags("MCP");
    }

    // ── GET /api/v1/ai/mcp/tools — tools list REST endpoint ──────────────

    private static void MapToolsListEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/ai/mcp/tools", async (
            string? category,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListMcpToolsFeature.Query(category), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("ai:runtime:read")
        .WithTags("MCP");
    }

    // ── POST /api/v1/ai/mcp — JSON-RPC 2.0 handler ───────────────────────

    private static void MapJsonRpcEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/mcp", async (
            HttpContext httpContext,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            JsonRpcRequest? rpcRequest;

            try
            {
                rpcRequest = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(
                    httpContext.Request.Body,
                    JsonOptions,
                    cancellationToken);
            }
            catch
            {
                await WriteJsonRpcErrorAsync(
                    httpContext, null,
                    McpErrorCodes.ParseError, "Parse error: invalid JSON.",
                    cancellationToken);
                return;
            }

            if (rpcRequest is null || rpcRequest.JsonRpc != "2.0" || string.IsNullOrWhiteSpace(rpcRequest.Method))
            {
                await WriteJsonRpcErrorAsync(
                    httpContext, rpcRequest?.Id,
                    McpErrorCodes.InvalidRequest, "Invalid Request: missing jsonrpc version or method.",
                    cancellationToken);
                return;
            }

            switch (rpcRequest.Method)
            {
                case "initialize":
                    await HandleInitializeAsync(httpContext, rpcRequest, sender, cancellationToken);
                    break;

                case "tools/list":
                    await HandleToolsListAsync(httpContext, rpcRequest, sender, cancellationToken);
                    break;

                case "tools/call":
                    await HandleToolsCallAsync(httpContext, rpcRequest, sender, cancellationToken);
                    break;

                case "ping":
                    await WriteJsonRpcResultAsync(httpContext, rpcRequest.Id, new { }, cancellationToken);
                    break;

                default:
                    await WriteJsonRpcErrorAsync(
                        httpContext, rpcRequest.Id,
                        McpErrorCodes.MethodNotFound,
                        $"Method not found: '{rpcRequest.Method}' is not supported by this MCP server.",
                        cancellationToken);
                    break;
            }
        })
        .RequirePermission("ai:runtime:read")
        .RequireRateLimiting("ai")
        .WithTags("MCP");
    }

    // ── Method handlers ───────────────────────────────────────────────────

    private static async Task HandleInitializeAsync(
        HttpContext httpContext,
        JsonRpcRequest rpcRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var infoResult = await sender.Send(new GetMcpServerInfoFeature.Query(), cancellationToken);

        if (!infoResult.IsSuccess)
        {
            await WriteJsonRpcErrorAsync(
                httpContext, rpcRequest.Id,
                McpErrorCodes.InternalError, "Internal error: could not retrieve server info.",
                cancellationToken);
            return;
        }

        var info = infoResult.Value;
        await WriteJsonRpcResultAsync(httpContext, rpcRequest.Id, new
        {
            protocolVersion = info.ProtocolVersion,
            capabilities = new
            {
                tools = info.Capabilities.Tools is not null
                    ? new { listChanged = info.Capabilities.Tools.ListChanged }
                    : null,
            },
            serverInfo = new
            {
                name = info.ServerName,
                version = info.ServerVersion,
            },
        }, cancellationToken);
    }

    private static async Task HandleToolsListAsync(
        HttpContext httpContext,
        JsonRpcRequest rpcRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var listResult = await sender.Send(new ListMcpToolsFeature.Query(), cancellationToken);

        if (!listResult.IsSuccess)
        {
            await WriteJsonRpcErrorAsync(
                httpContext, rpcRequest.Id,
                McpErrorCodes.InternalError, "Internal error: could not list tools.",
                cancellationToken);
            return;
        }

        var tools = listResult.Value.Tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            inputSchema = new
            {
                type = t.InputSchema.Type,
                properties = t.InputSchema.Properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new { type = kvp.Value.Type, description = kvp.Value.Description }),
                required = t.InputSchema.Required,
            },
        });

        await WriteJsonRpcResultAsync(httpContext, rpcRequest.Id, new { tools }, cancellationToken);
    }

    private static async Task HandleToolsCallAsync(
        HttpContext httpContext,
        JsonRpcRequest rpcRequest,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Extrair nome da tool e argumentos dos params JSON-RPC
        string? toolName = null;
        string argumentsJson = "{}";

        if (rpcRequest.Params.HasValue)
        {
            if (rpcRequest.Params.Value.TryGetProperty("name", out var nameProp))
                toolName = nameProp.GetString();

            if (rpcRequest.Params.Value.TryGetProperty("arguments", out var argsProp))
                argumentsJson = argsProp.GetRawText();
        }

        if (string.IsNullOrWhiteSpace(toolName))
        {
            await WriteJsonRpcErrorAsync(
                httpContext, rpcRequest.Id,
                McpErrorCodes.InvalidParams, "Invalid params: 'name' is required for tools/call.",
                cancellationToken);
            return;
        }

        var executeResult = await sender.Send(
            new ExecuteMcpToolFeature.Command(toolName, argumentsJson),
            cancellationToken);

        if (!executeResult.IsSuccess)
        {
            await WriteJsonRpcErrorAsync(
                httpContext, rpcRequest.Id,
                McpErrorCodes.ToolExecutionFailed,
                executeResult.Error?.Message ?? "Tool execution failed.",
                cancellationToken);
            return;
        }

        var toolResponse = executeResult.Value;
        await WriteJsonRpcResultAsync(httpContext, rpcRequest.Id, new
        {
            content = toolResponse.Content.Select(c => new { type = c.Type, text = c.Text }),
            isError = toolResponse.IsError,
        }, cancellationToken);
    }

    // ── JSON-RPC helpers ──────────────────────────────────────────────────

    private static async Task WriteJsonRpcResultAsync(
        HttpContext httpContext,
        JsonElement? id,
        object result,
        CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = 200;

        var envelope = new { jsonrpc = "2.0", id, result };
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(envelope, JsonOptions),
            cancellationToken);
    }

    private static async Task WriteJsonRpcErrorAsync(
        HttpContext httpContext,
        JsonElement? id,
        int code,
        string message,
        CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = 200; // JSON-RPC errors always use 200 per spec

        var envelope = new
        {
            jsonrpc = "2.0",
            id,
            error = new { code, message },
        };

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(envelope, JsonOptions),
            cancellationToken);
    }

    // ── JSON-RPC request model ─────────────────────────────────────────────

    private sealed record JsonRpcRequest(
        [property: JsonPropertyName("jsonrpc")] string JsonRpc,
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("id")] JsonElement? Id,
        [property: JsonPropertyName("params")] JsonElement? Params);

    // ── MCP error codes ───────────────────────────────────────────────────

    private static class McpErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
        public const int ToolExecutionFailed = -32002;
    }
}
