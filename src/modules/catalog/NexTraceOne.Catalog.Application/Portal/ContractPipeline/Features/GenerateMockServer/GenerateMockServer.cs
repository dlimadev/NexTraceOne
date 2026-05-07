using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Shared;

namespace NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateMockServer;

/// <summary>Gera configuração de mock server (WireMock ou json-server) a partir de contrato OpenAPI.</summary>
public static class GenerateMockServer
{
    /// <summary>Comando para gerar configuração de mock server.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string ContractJson,
        string MockServerType = "wiremock") : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.ContractJson).NotEmpty();
            RuleFor(x => x.MockServerType)
                .Must(t => t is "wiremock" or "json-server")
                .WithMessage("MockServerType must be 'wiremock' or 'json-server'.");
        }
    }

    /// <summary>Handler que gera ficheiros de mock server.</summary>
    public sealed class Handler(ILogger<Handler> logger) : ICommandHandler<Command, Response>
    {
        private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (files, instructions) = request.MockServerType == "wiremock"
                ? GenerateWireMock(request.ContractVersionId, request.ContractJson)
                : GenerateJsonServer(request.ContractVersionId, request.ContractJson);

            return Task.FromResult(Result<Response>.Success(new Response(
                ContractVersionId: request.ContractVersionId,
                MockServerType: request.MockServerType,
                Files: files,
                Instructions: instructions,
                PreviewNote: PipelinePreviewNote.Text)));
        }

        private (IReadOnlyList<GeneratedFile> Files, string Instructions) GenerateWireMock(Guid id, string contractJson)
        {
            var files = new List<GeneratedFile>();
            try
            {
                using var doc = JsonDocument.Parse(contractJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        foreach (var method in path.Value.EnumerateObject())
                        {
                            var stub = new
                            {
                                request = new { method = method.Name.ToUpperInvariant(), url = path.Name },
                                response = new { status = 200, headers = new { ContentType = "application/json" }, body = "{\"message\": \"mock response\"}" }
                            };
                            var fileName = $"__files/wiremock/{path.Name.Replace("/", "_").TrimStart('_')}_{method.Name}.json";
                            files.Add(new GeneratedFile(fileName, JsonSerializer.Serialize(stub, s_options), "json", $"WireMock stub for {method.Name.ToUpperInvariant()} {path.Name}"));
                        }
                    }
                }
            }
            catch (JsonException ex) { logger.LogWarning(ex, "Failed to parse contract JSON for WireMock stub generation. ContractVersionId={ContractVersionId}", id); }

            var instructions = "Start WireMock with: java -jar wiremock-standalone.jar --port 8080 --root-dir .";
            return (files, instructions);
        }

        private (IReadOnlyList<GeneratedFile> Files, string Instructions) GenerateJsonServer(Guid id, string contractJson)
        {
            var db = new Dictionary<string, object>();
            var routes = new Dictionary<string, string>();

            try
            {
                using var doc = JsonDocument.Parse(contractJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        var resource = path.Name.TrimStart('/').Split('/').First();
                        if (!string.IsNullOrEmpty(resource) && !db.ContainsKey(resource))
                        {
                            db[resource] = new[] { new { id = 1 } };
                            routes[$"/api/v1/{resource}"] = $"/{resource}";
                        }
                    }
                }
            }
            catch (JsonException ex) { logger.LogWarning(ex, "Failed to parse contract JSON for json-server generation. ContractVersionId={ContractVersionId}", id); }

            var files = new List<GeneratedFile>
            {
                new("db.json", JsonSerializer.Serialize(db.Count > 0 ? (object)db : new { items = Array.Empty<object>() }, s_options), "json", "json-server database"),
                new("routes.json", JsonSerializer.Serialize(routes.Count > 0 ? (object)routes : new { }, s_options), "json", "json-server routes")
            };

            var instructions = "Start json-server with: npx json-server --watch db.json --routes routes.json --port 3001";
            return (files, instructions);
        }
    }

    /// <summary>Resposta com ficheiros de mock server gerados.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string MockServerType,
        IReadOnlyList<GeneratedFile> Files,
        string Instructions,
        string PreviewNote);
}
