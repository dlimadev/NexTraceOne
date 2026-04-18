using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.GetMcpServerInfo;

/// <summary>
/// Feature: GetMcpServerInfo — retorna metadados do servidor MCP integrado ao módulo AIKnowledge.
/// Inclui versão do protocolo, capacidades suportadas e número de tools disponíveis.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetMcpServerInfo
{
    /// <summary>Query de consulta das informações do servidor MCP.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que constrói a resposta com os metadados do servidor MCP.</summary>
    public sealed class Handler(
        IToolRegistry toolRegistry) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var tools = toolRegistry.GetAll();
            var categories = tools
                .Select(t => t.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult<Result<Response>>(new Response(
                ServerName: "NexTraceOne MCP Server",
                ProtocolVersion: "2024-11-05",
                ServerVersion: "1.0.0",
                Description: "NexTraceOne AI governance and operational intelligence tools exposed via the Model Context Protocol. Enables external agents and developer tools to interact with services, contracts, changes and incidents.",
                Capabilities: new McpCapabilities(
                    Tools: new McpToolsCapability(ListChanged: false),
                    Prompts: null,
                    Resources: null),
                ToolCount: tools.Count,
                Categories: categories,
                EndpointUrl: "/api/v1/ai/mcp"));
        }
    }

    /// <summary>Resposta com metadados completos do servidor MCP.</summary>
    public sealed record Response(
        string ServerName,
        string ProtocolVersion,
        string ServerVersion,
        string Description,
        McpCapabilities Capabilities,
        int ToolCount,
        IReadOnlyList<string> Categories,
        string EndpointUrl);

    /// <summary>Capacidades suportadas pelo servidor MCP.</summary>
    public sealed record McpCapabilities(
        McpToolsCapability? Tools,
        McpPromptsCapability? Prompts,
        McpResourcesCapability? Resources);

    /// <summary>Capacidade de tools do servidor MCP.</summary>
    public sealed record McpToolsCapability(bool ListChanged);

    /// <summary>Capacidade de prompts do servidor MCP.</summary>
    public sealed record McpPromptsCapability(bool ListChanged);

    /// <summary>Capacidade de resources do servidor MCP.</summary>
    public sealed record McpResourcesCapability(bool Subscribe, bool ListChanged);
}
