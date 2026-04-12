using System.Text;
using System.Text.Json;

using Ardalis.GuardClauses;

using FluentValidation;

using Microsoft.Extensions.Logging;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ExportContractMultiFormat;

/// <summary>
/// Feature: ExportContractMultiFormat — exporta o spec de uma versão de contrato
/// em múltiplos formatos: openapi-yaml, openapi-json, postman-v21, insomnia, curl, bruno.
/// Para formatos não-OpenAPI, gera estrutura de colecção a partir do modelo canónico.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ExportContractMultiFormat
{
    private static readonly HashSet<string> SupportedFormats =
    [
        "openapi-yaml",
        "openapi-json",
        "postman-v21",
        "insomnia",
        "curl",
        "bruno"
    ];

    /// <summary>Query de exportação multi-formato de versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId, string Format) : IQuery<Response>;

    /// <summary>Valida a entrada da query de exportação multi-formato.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.Format)
                .NotEmpty()
                .Must(f => SupportedFormats.Contains(f.ToLowerInvariant()))
                .WithMessage("Format must be one of: openapi-yaml, openapi-json, postman-v21, insomnia, curl, bruno.");
        }
    }

    /// <summary>
    /// Handler que selecciona o exporter adequado ao formato solicitado
    /// e produz o conteúdo exportado com o content-type e filename corretos.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository, ILogger<Handler> logger) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var format = request.Format.ToLowerInvariant();
            if (!SupportedFormats.Contains(format))
                return ContractsErrors.UnsupportedExportFormat(request.Format);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            ContractCanonicalModel? canonical = null;
            try { canonical = CanonicalModelBuilder.Build(version.SpecContent, version.Protocol); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to build canonical model for contract version {ContractVersionId}; continuing with null canonical model", request.ContractVersionId); }

            var safeName = (canonical?.Title ?? "contract").ToLowerInvariant().Replace(' ', '-');
            var semVer = version.SemVer;

            var (content, fileName, contentType) = format switch
            {
                "openapi-yaml" => OpenApiYamlExporter.Export(version.SpecContent, version.Format, safeName, semVer),
                "openapi-json" => OpenApiJsonExporter.Export(version.SpecContent, version.Format, safeName, semVer, logger),
                "postman-v21" => PostmanCollectionExporter.Export(version.SpecContent, canonical, safeName, semVer),
                "insomnia" => InsomniaExporter.Export(canonical, safeName, semVer),
                "curl" => CurlExporter.Export(canonical, safeName, semVer),
                "bruno" => BrunoExporter.Export(canonical, safeName, semVer),
                _ => ("", "export.txt", "text/plain")
            };

            return new Response(content, fileName, contentType);
        }
    }

    // ── Exporters ────────────────────────────────────────────────────────────────

    private static class OpenApiYamlExporter
    {
        internal static (string content, string fileName, string contentType) Export(
            string specContent, string format, string safeName, string semVer)
        {
            // Se já é YAML, devolve como está; se é JSON, retorna sem conversão (sem biblioteca YAML)
            var content = specContent;
            return (content, $"{safeName}-{semVer}.yaml", "application/x-yaml");
        }
    }

    private static class OpenApiJsonExporter
    {
        internal static (string content, string fileName, string contentType) Export(
            string specContent, string format, string safeName, string semVer, ILogger logger)
        {
            // Se já é JSON, devolve como está; se é YAML, retorna sem conversão (sem biblioteca YAML)
            string content = specContent;
            if (format == "json")
            {
                try
                {
                    using var doc = JsonDocument.Parse(specContent);
                    content = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to re-format JSON spec for export; using raw content"); content = specContent; }
            }
            return (content, $"{safeName}-{semVer}.json", "application/json");
        }
    }

    private static class PostmanCollectionExporter
    {
        internal static (string content, string fileName, string contentType) Export(
            string specContent, ContractCanonicalModel? canonical, string safeName, string semVer)
        {
            var title = canonical?.Title ?? safeName;
            var items = new List<object>();

            if (canonical?.Operations is { Count: > 0 })
            {
                foreach (var op in canonical.Operations)
                {
                    items.Add(new
                    {
                        name = string.IsNullOrWhiteSpace(op.OperationId) ? $"{op.Method} {op.Path}" : op.OperationId,
                        request = new
                        {
                            method = op.Method.ToUpperInvariant(),
                            header = new object[] { },
                            url = new
                            {
                                raw = $"{{{{baseUrl}}}}{op.Path}",
                                host = new[] { "{{baseUrl}}" },
                                path = op.Path.Trim('/').Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray()
                            },
                            description = op.Description
                        }
                    });
                }
            }

            var collection = new
            {
                info = new
                {
                    _postman_id = Guid.NewGuid().ToString(),
                    name = title,
                    schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
                    description = $"Generated from NexTraceOne — {title} v{semVer}"
                },
                item = items,
                variable = new[] { new { key = "baseUrl", value = "https://api.example.com", type = "string" } }
            };

            var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
            return (json, $"{safeName}-{semVer}-postman-v21.json", "application/json");
        }
    }

    private static class InsomniaExporter
    {
        internal static (string content, string fileName, string contentType) Export(
            ContractCanonicalModel? canonical, string safeName, string semVer)
        {
            var title = canonical?.Title ?? safeName;
            var workspaceId = $"wrk_{Guid.NewGuid():N}";
            var resources = new List<object>
            {
                new
                {
                    _id = workspaceId,
                    _type = "workspace",
                    name = title,
                    description = $"Generated from NexTraceOne — {title} v{semVer}",
                    scope = "collection"
                }
            };

            if (canonical?.Operations is { Count: > 0 })
            {
                foreach (var op in canonical.Operations)
                {
                    resources.Add(new
                    {
                        _id = $"req_{Guid.NewGuid():N}",
                        _type = "request",
                        parentId = workspaceId,
                        name = string.IsNullOrWhiteSpace(op.OperationId) ? $"{op.Method} {op.Path}" : op.OperationId,
                        method = op.Method.ToUpperInvariant(),
                        url = $"{{{{ _.baseUrl }}}}{op.Path}",
                        description = op.Description ?? "",
                        headers = new object[] { },
                        body = new { mimeType = "application/json", text = "" }
                    });
                }
            }

            var workspace = new
            {
                _type = "export",
                __export_format = 4,
                __export_date = DateTimeOffset.UtcNow.ToString("o"),
                __export_source = "NexTraceOne",
                resources
            };

            var json = JsonSerializer.Serialize(workspace, new JsonSerializerOptions { WriteIndented = true });
            return (json, $"{safeName}-{semVer}-insomnia.json", "application/json");
        }
    }

    private static class CurlExporter
    {
        internal static (string content, string fileName, string contentType) Export(
            ContractCanonicalModel? canonical, string safeName, string semVer)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# cURL commands — {canonical?.Title ?? safeName} v{semVer}");
            sb.AppendLine($"# Generated by NexTraceOne");
            sb.AppendLine();

            if (canonical?.Operations is { Count: > 0 })
            {
                foreach (var op in canonical.Operations)
                {
                    var method = op.Method.ToUpperInvariant();
                    sb.AppendLine($"# {op.OperationId}");
                    sb.Append($"curl -X {method} \"${{BASE_URL}}{op.Path}\"");
                    sb.Append(" \\\n  -H \"Content-Type: application/json\"");
                    sb.Append(" \\\n  -H \"Authorization: Bearer ${TOKEN}\"");

                    if (method is "POST" or "PUT" or "PATCH")
                        sb.Append(" \\\n  -d '{}'");

                    sb.AppendLine();
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("# No operations found in spec.");
                sb.AppendLine($"curl -X GET \"${{BASE_URL}}/api/v1/resource\" \\");
                sb.AppendLine("  -H \"Authorization: Bearer ${TOKEN}\"");
            }

            return (sb.ToString(), $"{safeName}-{semVer}-curl.sh", "text/x-sh");
        }
    }

    private static class BrunoExporter
    {
        internal static (string content, string fileName, string contentType) Export(
            ContractCanonicalModel? canonical, string safeName, string semVer)
        {
            var sb = new StringBuilder();
            var title = canonical?.Title ?? safeName;

            sb.AppendLine($"# Bruno collection — {title} v{semVer}");
            sb.AppendLine($"# Generated by NexTraceOne");
            sb.AppendLine();

            if (canonical?.Operations is { Count: > 0 })
            {
                foreach (var op in canonical.Operations)
                {
                    var method = op.Method.ToUpperInvariant();
                    var name = string.IsNullOrWhiteSpace(op.OperationId) ? $"{method} {op.Path}" : op.OperationId;

                    sb.AppendLine($"http {{");
                    sb.AppendLine($"  method: {method}");
                    sb.AppendLine($"  url: {{{{baseUrl}}}}{op.Path}");
                    sb.AppendLine($"  name: {name}");
                    sb.AppendLine($"}}");
                    sb.AppendLine();
                    sb.AppendLine($"headers {{");
                    sb.AppendLine($"  Content-Type: application/json");
                    sb.AppendLine($"  Authorization: Bearer {{{{token}}}}");
                    sb.AppendLine($"}}");
                    sb.AppendLine();

                    if (method is "POST" or "PUT" or "PATCH")
                    {
                        sb.AppendLine($"body json {{");
                        sb.AppendLine($"  {{}}");
                        sb.AppendLine($"}}");
                        sb.AppendLine();
                    }
                }
            }

            return (sb.ToString(), $"{safeName}-{semVer}.bru", "text/plain");
        }
    }

    /// <summary>Resposta da exportação multi-formato.</summary>
    public sealed record Response(
        string Content,
        string FileName,
        string ContentType);
}
