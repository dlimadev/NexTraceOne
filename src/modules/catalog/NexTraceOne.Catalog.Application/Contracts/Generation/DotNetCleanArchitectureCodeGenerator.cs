using System.Globalization;
using System.Text;

namespace NexTraceOne.Catalog.Application.Contracts.Generation;

/// <summary>Ficheiro de código gerado (caminho relativo + conteúdo).</summary>
public sealed record GeneratedCodeFile(string Path, string Content);

/// <summary>Opções de geração de código.</summary>
/// <param name="ServiceName">Nome do serviço em kebab-case (ex: payment-api).</param>
/// <param name="RootNamespace">Namespace raiz; quando null, derivado do ServiceName em PascalCase.</param>
public sealed record CodeGenerationOptions(string ServiceName, string? RootNamespace = null);

/// <summary>
/// Gerador determinístico de código .NET (padrão Clean Architecture) a partir de um
/// <see cref="OpenApiContractModel"/>. Não depende de IA.
///
/// Emite, de forma determinística:
///   - um record DTO por schema (camada Contracts);
///   - um ficheiro de endpoints (Minimal API) por tag/grupo de operações (camada API).
///
/// O mapeamento de tipos OpenAPI → C# é puro e testável. A IA, quando configurada, pode
/// posteriormente enriquecer o corpo dos handlers — mas nunca é necessária.
/// </summary>
public static class DotNetCleanArchitectureCodeGenerator
{
    /// <summary>Gera os ficheiros de código a partir do modelo de contrato.</summary>
    public static IReadOnlyList<GeneratedCodeFile> Generate(
        OpenApiContractModel model, CodeGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);

        var rootNamespace = string.IsNullOrWhiteSpace(options.RootNamespace)
            ? ToPascalCase(options.ServiceName)
            : options.RootNamespace!;

        var files = new List<GeneratedCodeFile>();

        // ── DTOs (um por schema) ────────────────────────────────────────────
        foreach (var schema in model.Schemas)
            files.Add(GenerateDto(schema, rootNamespace, options.ServiceName));

        // ── Endpoints (agrupados por tag) ───────────────────────────────────
        var groups = model.Operations
            .GroupBy(o => string.IsNullOrWhiteSpace(o.Tag) ? "Api" : o.Tag!)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in groups)
            files.Add(GenerateEndpoints(group.Key, group.ToList(), rootNamespace, options.ServiceName));

        return files;
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private static GeneratedCodeFile GenerateDto(SchemaModel schema, string rootNamespace, string serviceName)
    {
        var typeName = ToPascalCase(schema.Name);
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Contracts;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>DTO gerado a partir do schema OpenAPI '{schema.Name}'.</summary>");
        sb.AppendLine($"public sealed record {typeName}");
        sb.AppendLine("{");

        for (var i = 0; i < schema.Properties.Count; i++)
        {
            var prop = schema.Properties[i];
            var propName = ToPascalCase(prop.Name);
            var clrType = MapToClrType(prop.NeutralType, prop.Required);

            if (!string.Equals(propName, prop.Name, StringComparison.Ordinal))
                sb.AppendLine($"    [JsonPropertyName(\"{prop.Name}\")]");
            sb.AppendLine($"    public {clrType} {propName} {{ get; init; }}");
            if (i < schema.Properties.Count - 1)
                sb.AppendLine();
        }

        sb.AppendLine("}");

        var path = $"src/{serviceName}.Contracts/{typeName}.cs";
        return new GeneratedCodeFile(path, sb.ToString());
    }

    // ── Endpoints ─────────────────────────────────────────────────────────

    private static GeneratedCodeFile GenerateEndpoints(
        string group, IReadOnlyList<OperationModel> operations, string rootNamespace, string serviceName)
    {
        var groupName = ToPascalCase(group);
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine("using Microsoft.AspNetCore.Routing;");
        sb.AppendLine();
        sb.AppendLine($"using {rootNamespace}.Contracts;");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.Api.Endpoints;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Endpoints gerados a partir do contrato OpenAPI para o grupo '{group}'.</summary>");
        sb.AppendLine($"public static class {groupName}Endpoints");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>Regista os endpoints do grupo '{group}'.</summary>");
        sb.AppendLine($"    public static void Map{groupName}Endpoints(IEndpointRouteBuilder app)");
        sb.AppendLine("    {");

        foreach (var op in operations)
        {
            var operationName = ToPascalCase(op.OperationId);
            var mapMethod = MapMethodName(op.Method);
            var hasRequest = !string.IsNullOrWhiteSpace(op.RequestSchemaName);
            var requestParam = hasRequest
                ? $"{ToPascalCase(op.RequestSchemaName!)} request"
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(op.Summary))
                sb.AppendLine($"        // {op.Summary}");
            if (!string.IsNullOrWhiteSpace(op.ResponseSchemaName))
                sb.AppendLine($"        // Resposta esperada: {ToPascalCase(op.ResponseSchemaName!)}");

            sb.AppendLine($"        app.{mapMethod}(\"{op.Path}\", ({requestParam}) =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            // TODO: implementar a lógica de {operationName}.");
            sb.AppendLine("            return Results.Ok();");
            sb.AppendLine($"        }})\n        .WithName(\"{operationName}\");");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        var path = $"src/{serviceName}.Api/Endpoints/{groupName}Endpoints.cs";
        return new GeneratedCodeFile(path, sb.ToString());
    }

    // ── Mapeamento de tipos OpenAPI (neutro) → C# ───────────────────────────

    /// <summary>
    /// Mapeia o tipo neutro para o tipo C#. Tipos não obrigatórios ficam anuláveis ('?').
    /// </summary>
    internal static string MapToClrType(string neutralType, bool required)
    {
        var clr = MapCore(neutralType);
        return required ? clr : clr + "?";
    }

    private static string MapCore(string neutralType)
    {
        if (neutralType.StartsWith("array:", StringComparison.Ordinal))
        {
            var item = MapCore(neutralType["array:".Length..]);
            return $"IReadOnlyList<{item}>";
        }

        if (neutralType.StartsWith("ref:", StringComparison.Ordinal))
            return ToPascalCase(neutralType["ref:".Length..]);

        return neutralType switch
        {
            "string" => "string",
            "integer" => "int",
            "long" => "long",
            "number" => "double",
            "boolean" => "bool",
            "date-time" => "DateTimeOffset",
            "date" => "DateOnly",
            "uuid" => "Guid",
            _ => "object"
        };
    }

    private static string MapMethodName(string httpMethod) => httpMethod.ToUpperInvariant() switch
    {
        "GET" => "MapGet",
        "POST" => "MapPost",
        "PUT" => "MapPut",
        "DELETE" => "MapDelete",
        "PATCH" => "MapPatch",
        _ => "MapMethods"
    };

    // ── Utilitários ─────────────────────────────────────────────────────────

    /// <summary>Converte um identificador (kebab, snake, camel ou com espaços) para PascalCase.</summary>
    internal static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var parts = value.Split(['-', '_', ' ', '.', '/', '{', '}'], StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            sb.Append(char.ToUpper(part[0], CultureInfo.InvariantCulture));
            if (part.Length > 1)
                sb.Append(part[1..]);
        }
        return sb.Length == 0 ? value : sb.ToString();
    }
}
