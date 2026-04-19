using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ListMcpTools;

/// <summary>
/// Feature: ListMcpTools — lista todas as tools registadas no formato MCP (Model Context Protocol).
/// Converte as ToolDefinitions internas para o formato JSON Schema exigido pelo protocolo MCP 2024-11-05.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListMcpTools
{
    /// <summary>Query de listagem de tools no formato MCP.</summary>
    public sealed record Query(string? Category = null) : IQuery<Response>;

    /// <summary>Handler que converte as tools registadas para o formato MCP.</summary>
    public sealed class Handler(
        IToolRegistry toolRegistry) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var rawTools = string.IsNullOrWhiteSpace(request.Category)
                ? toolRegistry.GetAll()
                : toolRegistry.GetByCategory(request.Category);

            var mcpTools = rawTools
                .Select(t => new McpToolInfo(
                    Name: t.Name,
                    Description: t.Description,
                    InputSchema: BuildInputSchema(t.Parameters)))
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Task.FromResult<Result<Response>>(new Response(
                Tools: mcpTools,
                TotalCount: mcpTools.Count));
        }

        private static McpInputSchema BuildInputSchema(
            IReadOnlyList<ToolParameterDefinition> parameters)
        {
            var properties = parameters.ToDictionary(
                p => p.Name,
                p => new McpPropertySchema(
                    Type: MapType(p.Type),
                    Description: p.Description),
                StringComparer.OrdinalIgnoreCase);

            var required = parameters
                .Where(p => p.Required)
                .Select(p => p.Name)
                .ToList();

            return new McpInputSchema(
                Type: "object",
                Properties: properties,
                Required: required);
        }

        private static string MapType(string internalType) => internalType.ToLowerInvariant() switch
        {
            "boolean" or "bool" => "boolean",
            "integer" or "int" or "long" => "integer",
            "number" or "float" or "double" or "decimal" => "number",
            "array" or "list" => "array",
            "object" => "object",
            _ => "string"
        };
    }

    /// <summary>Resposta contendo a lista de tools no formato MCP.</summary>
    public sealed record Response(
        IReadOnlyList<McpToolInfo> Tools,
        int TotalCount);

    /// <summary>Definição de uma tool no formato MCP.</summary>
    public sealed record McpToolInfo(
        string Name,
        string Description,
        McpInputSchema InputSchema);

    /// <summary>JSON Schema de entrada de uma tool MCP (formato object).</summary>
    public sealed record McpInputSchema(
        string Type,
        IReadOnlyDictionary<string, McpPropertySchema> Properties,
        IReadOnlyList<string> Required);

    /// <summary>Schema de uma propriedade individual do input schema MCP.</summary>
    public sealed record McpPropertySchema(
        string Type,
        string Description);
}
