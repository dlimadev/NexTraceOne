using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.ContractPipeline.Shared;

using GenerateServerFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateServerFromContract.GenerateServerFromContract;
using GenerateMockFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateMockServer.GenerateMockServer;
using GeneratePostmanFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GeneratePostmanCollection.GeneratePostmanCollection;
using GenerateTestsFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateContractTests.GenerateContractTests;
using GenerateSdkFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateClientSdkFromContract.GenerateClientSdkFromContract;

namespace NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.OrchestrateContractPipeline;

/// <summary>Orquestra o pipeline completo Contract-to-Code, executando os artefactos seleccionados.</summary>
public static class OrchestrateContractPipeline
{
    /// <summary>Comando para orquestrar o pipeline de contrato.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string ContractJson,
        string ServiceName,
        string TargetLanguage,
        bool GenerateServer = true,
        bool GenerateMockServer = false,
        bool GeneratePostman = false,
        bool GenerateTests = false,
        bool GenerateClientSdk = false) : ICommand<Response>;

    /// <summary>Validação do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetLanguage).NotEmpty();
        }
    }

    /// <summary>Handler que orquestra todos os geradores seleccionados.</summary>
    public sealed class Handler(ISender sender) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var artifacts = new List<PipelineArtifact>();

            if (request.GenerateServer)
            {
                var result = await sender.Send(new GenerateServerFeature.Command(request.ContractVersionId, request.TargetLanguage, request.ServiceName), cancellationToken);
                if (result.IsSuccess)
                    artifacts.Add(new PipelineArtifact("ServerStubs", result.Value.Files));
            }

            if (request.GenerateMockServer)
            {
                var result = await sender.Send(new GenerateMockFeature.Command(request.ContractVersionId, request.ContractJson, "wiremock"), cancellationToken);
                if (result.IsSuccess)
                    artifacts.Add(new PipelineArtifact("MockServer", result.Value.Files));
            }

            if (request.GeneratePostman)
            {
                var result = await sender.Send(new GeneratePostmanFeature.Command(request.ContractVersionId, request.ContractJson, request.ServiceName), cancellationToken);
                if (result.IsSuccess)
                    artifacts.Add(new PipelineArtifact("PostmanCollection", [new GeneratedFile("collection.json", result.Value.CollectionJson, "json", "Postman Collection")]));
            }

            if (request.GenerateTests)
            {
                var result = await sender.Send(new GenerateTestsFeature.Command(request.ContractVersionId, request.ContractJson, request.ServiceName, "xunit"), cancellationToken);
                if (result.IsSuccess)
                    artifacts.Add(new PipelineArtifact("ContractTests", result.Value.Files));
            }

            if (request.GenerateClientSdk)
            {
                var result = await sender.Send(new GenerateSdkFeature.Command(request.ContractVersionId, request.TargetLanguage, $"{request.ServiceName}Client"), cancellationToken);
                if (result.IsSuccess)
                    artifacts.Add(new PipelineArtifact("ClientSdk", result.Value.Files));
            }

            var totalFiles = artifacts.Sum(a => a.Files.Count);
            return Result<Response>.Success(new Response(
                ContractVersionId: request.ContractVersionId,
                TotalFiles: totalFiles,
                GeneratedArtifacts: artifacts,
                PreviewNote: PipelinePreviewNote.Text));
        }
    }

    /// <summary>Artefacto gerado pelo pipeline.</summary>
    public sealed record PipelineArtifact(string ArtifactType, IReadOnlyList<GeneratedFile> Files);

    /// <summary>Resposta do orquestrador do pipeline.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        int TotalFiles,
        IReadOnlyList<PipelineArtifact> GeneratedArtifacts,
        string PreviewNote);
}
