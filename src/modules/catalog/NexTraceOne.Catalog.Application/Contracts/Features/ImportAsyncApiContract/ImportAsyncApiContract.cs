using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ImportAsyncApiContract;

/// <summary>
/// Feature: ImportAsyncApiContract — importa e regista um contrato Event/AsyncAPI com workflow real.
/// Distingue-se do ImportContract genérico: para além de criar a ContractVersion com Protocol=AsyncApi,
/// extrai metadados AsyncAPI específicos (channels, operações, mensagens, servidores, versão AsyncAPI)
/// via AsyncApiMetadataExtractor e persiste-os num EventContractDetail independente.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class ImportAsyncApiContract
{
    /// <summary>
    /// Comando de importação de contrato AsyncAPI com metadados específicos.
    /// </summary>
    public sealed record Command(
        Guid ApiAssetId,
        string SemVer,
        string AsyncApiContent,
        string ImportedFrom,
        string? DefaultContentType = null) : ICommand<Response>;

    /// <summary>
    /// Valida a entrada do comando de importação AsyncAPI.
    /// O conteúdo deve ser JSON válido com o campo "asyncapi" presente.
    /// </summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.SemVer).NotEmpty().MaximumLength(50);
            RuleFor(x => x.AsyncApiContent).NotEmpty()
                .MaximumLength(5_242_880)
                .WithMessage("AsyncAPI content exceeds maximum allowed size of 5MB.")
                .Must(IsAsyncApiContent)
                .WithMessage("Content must be a valid AsyncAPI document (JSON with 'asyncapi' field).");
            RuleFor(x => x.ImportedFrom).NotEmpty().MaximumLength(500);
            RuleFor(x => x.DefaultContentType).MaximumLength(200).When(x => x.DefaultContentType is not null);
        }

        private static bool IsAsyncApiContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;
            var trimmed = content.TrimStart();
            return (trimmed.StartsWith('{') || trimmed.StartsWith('['))
                && trimmed.Contains("\"asyncapi\"", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Handler que importa um contrato AsyncAPI com workflow real:
    /// 1. Verifica unicidade da versão para o ativo de API
    /// 2. Cria ContractVersion com Protocol=AsyncApi
    /// 3. Extrai metadados AsyncAPI via AsyncApiMetadataExtractor
    /// 4. Persiste EventContractDetail com os metadados extraídos
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IEventContractDetailRepository eventDetailRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // 1. Verifica unicidade
            var existing = await contractVersionRepository.GetByApiAssetAndSemVerAsync(
                request.ApiAssetId, request.SemVer, cancellationToken);

            if (existing is not null)
                return ContractsErrors.AlreadyExists(request.SemVer, request.ApiAssetId.ToString());

            // 2. Cria ContractVersion com Protocol=AsyncApi
            var importResult = ContractVersion.Import(
                request.ApiAssetId,
                request.SemVer,
                request.AsyncApiContent,
                "json",
                request.ImportedFrom,
                ContractProtocol.AsyncApi);

            if (importResult.IsFailure)
                return importResult.Error;

            var contractVersion = importResult.Value;
            contractVersionRepository.Add(contractVersion);

            // 3. Extrai metadados AsyncAPI específicos
            var metadata = AsyncApiMetadataExtractor.Extract(request.AsyncApiContent);

            // Override do defaultContentType se fornecido explicitamente
            var defaultContentType = request.DefaultContentType ?? metadata.DefaultContentType;

            // 4. Cria EventContractDetail com metadados extraídos
            var detailResult = EventContractDetail.Create(
                contractVersion.Id,
                metadata.Title,
                metadata.AsyncApiVersion,
                metadata.ChannelsJson,
                metadata.MessagesJson,
                metadata.ServersJson,
                defaultContentType);

            if (detailResult.IsFailure)
                return detailResult.Error;

            eventDetailRepository.Add(detailResult.Value);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                ContractVersionId: contractVersion.Id.Value,
                ApiAssetId: contractVersion.ApiAssetId,
                SemVer: contractVersion.SemVer,
                Title: metadata.Title,
                AsyncApiVersion: metadata.AsyncApiVersion,
                DefaultContentType: defaultContentType,
                ChannelsJson: metadata.ChannelsJson,
                MessagesJson: metadata.MessagesJson,
                ServersJson: metadata.ServersJson,
                ImportedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da importação AsyncAPI, incluindo metadados de evento extraídos.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string SemVer,
        string Title,
        string AsyncApiVersion,
        string DefaultContentType,
        string ChannelsJson,
        string MessagesJson,
        string ServersJson,
        DateTimeOffset ImportedAt);
}
