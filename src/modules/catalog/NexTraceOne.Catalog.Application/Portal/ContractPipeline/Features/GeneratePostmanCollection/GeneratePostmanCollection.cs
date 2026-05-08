using System.Text.Json;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Shared;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GeneratePostmanCollection;

/// <summary>Converte um contrato OpenAPI em Postman Collection v2.1.</summary>
public static class GeneratePostmanCollection
{
    /// <summary>Comando para gerar coleção Postman.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string CollectionName) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.CollectionName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que gera a coleção Postman carregando a spec da base de dados.</summary>
    public sealed class Handler(IContractVersionRepository contractVersionRepository) : ICommandHandler<Command, Response>
    {
        private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var contractVersion = await contractVersionRepository.GetByIdAsync(
                new ContractVersionId(request.ContractVersionId), cancellationToken);

            if (contractVersion is null)
                return Error.NotFound("contract.version.not_found", "Contract version not found.");

            var specContent = contractVersion.SpecContent;
            var items = new List<object>();
            var endpointCount = 0;
            var baseUrl = "{{baseUrl}}";

            try
            {
                using var doc = JsonDocument.Parse(specContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("paths", out var paths))
                {
                    foreach (var path in paths.EnumerateObject())
                    {
                        foreach (var method in path.Value.EnumerateObject())
                        {
                            var opName = method.Name.ToUpperInvariant();
                            var summary = method.Value.TryGetProperty("summary", out var s) ? s.GetString() ?? $"{opName} {path.Name}" : $"{opName} {path.Name}";

                            items.Add(new
                            {
                                name = summary,
                                request = new
                                {
                                    method = opName,
                                    header = new[] { new { key = "Content-Type", value = "application/json" } },
                                    url = new
                                    {
                                        raw = $"{baseUrl}{path.Name}",
                                        host = new[] { baseUrl },
                                        path = path.Name.TrimStart('/').Split('/')
                                    }
                                }
                            });
                            endpointCount++;
                        }
                    }
                }
            }
            catch (JsonException) { /* returns empty collection on unparseable spec */ }

            var collection = new
            {
                info = new
                {
                    name = request.CollectionName,
                    schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
                    _postman_id = Guid.NewGuid()
                },
                item = items,
                variable = new[] { new { key = "baseUrl", value = "http://localhost:5000/api/v1", type = "string" } }
            };

            var collectionJson = JsonSerializer.Serialize(collection, s_options);
            return Result<Response>.Success(new Response(
                ContractVersionId: request.ContractVersionId,
                CollectionJson: collectionJson,
                EndpointCount: endpointCount,
                PreviewNote: PipelinePreviewNote.Text));
        }
    }

    /// <summary>Resposta com coleção Postman em JSON.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string CollectionJson,
        int EndpointCount,
        string PreviewNote);
}
