using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SyncContracts;

/// <summary>
/// Feature: SyncContracts — endpoint de sincronização em lote para integração externa.
/// Permite que sistemas de CI/CD, API Gateways e plataformas externas importem múltiplos
/// contratos em uma única operação autenticada sistema-a-sistema.
/// Usa idempotência por chave composta (ApiAssetId + SemVer): se a versão já existe,
/// o item é marcado como Skipped em vez de falhar.
/// Itens com erro não bloqueiam o processamento dos demais (fault isolation por item).
/// Máximo de 50 itens por lote para garantir latência aceitável.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SyncContracts
{
    /// <summary>
    /// Comando de sincronização em lote de contratos externos.
    /// SourceSystem identifica a origem da integração (ex: "github-actions", "jenkins").
    /// CorrelationId é propagado para rastreabilidade ponta a ponta nos logs.
    /// </summary>
    public sealed record Command(
        IReadOnlyList<ContractSyncItem> Items,
        string SourceSystem,
        string? CorrelationId) : ICommand<Response>;

    /// <summary>
    /// Item individual de sincronização de contrato.
    /// A combinação ApiAssetId + SemVer é a chave de idempotência:
    /// versões já existentes não são sobrescritas — retornam status Skipped.
    /// Protocol é opcional: quando omitido, o sistema tenta detectar automaticamente.
    /// </summary>
    public sealed record ContractSyncItem(
        Guid ApiAssetId,
        string SemVer,
        string SpecContent,
        string Format,
        string ImportedFrom,
        ContractProtocol Protocol = ContractProtocol.OpenApi);

    /// <summary>
    /// Valida o comando de sincronização e cada item individualmente.
    /// Rejeita o lote inteiro se ultrapassar 50 itens ou se os campos obrigatórios estiverem ausentes.
    /// Aceita formatos json, yaml e xml para suportar todos os protocolos.
    /// </summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Items)
                .NotNull()
                .NotEmpty()
                .WithMessage("At least one contract sync item is required.");

            RuleFor(x => x.Items.Count)
                .LessThanOrEqualTo(50)
                .WithMessage("Maximum of 50 items per sync batch.");

            RuleFor(x => x.SourceSystem)
                .NotEmpty()
                .MaximumLength(200);

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ApiAssetId).NotEmpty();
                item.RuleFor(i => i.SemVer).NotEmpty().MaximumLength(50);
                item.RuleFor(i => i.SpecContent)
                    .NotEmpty()
                    .MaximumLength(5_242_880)
                    .WithMessage("Spec content exceeds maximum allowed size of 5MB.");
                item.RuleFor(i => i.Format)
                    .NotEmpty()
                    .Must(f => f is "json" or "yaml" or "xml")
                    .WithMessage("Format must be 'json', 'yaml' or 'xml'.");
                item.RuleFor(i => i.ImportedFrom).NotEmpty().MaximumLength(500);
                item.RuleFor(i => i.Protocol).IsInEnum();
            });
        }
    }

    /// <summary>
    /// Handler que processa a sincronização de contratos em lote.
    /// Para cada item: verifica idempotência, detecta protocolo automaticamente
    /// e importa a versão se ela ainda não existir.
    /// Itens com falha são isolados — não bloqueiam o processamento dos demais.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = new List<SyncItemResult>();

            foreach (var item in request.Items)
            {
                var itemResult = await ProcessItemAsync(item, request.SourceSystem, cancellationToken);
                results.Add(itemResult);
            }

            // Commit único após processar todos os itens para reduzir roundtrips ao banco
            await unitOfWork.CommitAsync(cancellationToken);

            var created = results.Count(r => r.Status == SyncStatus.Created);
            var skipped = results.Count(r => r.Status == SyncStatus.Skipped);
            var failed = results.Count(r => r.Status == SyncStatus.Failed);

            return new Response(
                TotalProcessed: results.Count,
                Created: created,
                Skipped: skipped,
                Failed: failed,
                CorrelationId: request.CorrelationId,
                ProcessedAt: dateTimeProvider.UtcNow,
                Items: results.AsReadOnly());
        }

        /// <summary>
        /// Processa um único item de sincronização com isolamento de falha.
        /// Detecta protocolo automaticamente quando o valor padrão é informado.
        /// Se a versão já existir para o ativo/semVer, retorna Skipped sem sobrescrever.
        /// </summary>
        private async Task<SyncItemResult> ProcessItemAsync(
            ContractSyncItem item,
            string sourceSystem,
            CancellationToken cancellationToken)
        {
            try
            {
                var existing = await repository.GetByApiAssetAndSemVerAsync(
                    item.ApiAssetId, item.SemVer, cancellationToken);

                if (existing is not null)
                {
                    return new SyncItemResult(
                        item.ApiAssetId, item.SemVer, SyncStatus.Skipped,
                        existing.Id.Value, null);
                }

                // Detecta protocolo automaticamente quando informado como padrão
                var protocol = DetectProtocol(item.SpecContent, item.Protocol);

                var importResult = ContractVersion.Import(
                    item.ApiAssetId,
                    item.SemVer,
                    item.SpecContent,
                    item.Format,
                    $"{sourceSystem}:{item.ImportedFrom}",
                    protocol);

                if (importResult.IsFailure)
                {
                    return new SyncItemResult(
                        item.ApiAssetId, item.SemVer, SyncStatus.Failed,
                        null, importResult.Error.Message);
                }

                repository.Add(importResult.Value);

                return new SyncItemResult(
                    item.ApiAssetId, item.SemVer, SyncStatus.Created,
                    importResult.Value.Id.Value, null);
            }
            catch (Exception ex)
            {
                // Isolamento de falha: erros inesperados por item não bloqueiam o lote
                return new SyncItemResult(
                    item.ApiAssetId, item.SemVer, SyncStatus.Failed,
                    null, ex.Message);
            }
        }

        /// <summary>
        /// Detecta o protocolo a partir do conteúdo da especificação quando o padrão é informado.
        /// Mesma lógica do ImportContract para consistência no módulo.
        /// </summary>
        private static ContractProtocol DetectProtocol(string specContent, ContractProtocol specified)
        {
            if (specified != ContractProtocol.OpenApi)
                return specified;

            var trimmed = specContent.TrimStart();

            if (trimmed.StartsWith('<') && (trimmed.Contains("wsdl:definitions", StringComparison.OrdinalIgnoreCase)
                                            || trimmed.Contains("<definitions", StringComparison.OrdinalIgnoreCase)))
                return ContractProtocol.Wsdl;

            // Usa AsSpan para evitar alocação de substring ao inspecionar apenas o cabeçalho
            // do spec — relevante em cenários de lote onde este método é chamado N vezes
            var headerSpan = specContent.Length > 4096
                ? specContent.AsSpan(0, 4096)
                : specContent.AsSpan();

            if (headerSpan.Contains("\"asyncapi\"", StringComparison.OrdinalIgnoreCase)
                || headerSpan.Contains("asyncapi:", StringComparison.OrdinalIgnoreCase))
                return ContractProtocol.AsyncApi;

            if (headerSpan.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase)
                || headerSpan.Contains("swagger:", StringComparison.OrdinalIgnoreCase))
                return ContractProtocol.Swagger;

            return specified;
        }
    }

    /// <summary>Status do processamento de um item de sincronização.</summary>
    public enum SyncStatus
    {
        /// <summary>Contrato importado com sucesso — versão nova criada.</summary>
        Created,

        /// <summary>Versão já existia para o ativo/semVer — ignorado por idempotência.</summary>
        Skipped,

        /// <summary>Falha ao importar — detalhes no campo ErrorMessage.</summary>
        Failed,
    }

    /// <summary>Resultado do processamento de um item individual de sincronização.</summary>
    public sealed record SyncItemResult(
        Guid ApiAssetId,
        string SemVer,
        SyncStatus Status,
        Guid? ContractVersionId,
        string? ErrorMessage);

    /// <summary>
    /// Resposta da sincronização em lote com resumo e detalhes por item.
    /// Permite ao caller identificar exatamente quais itens foram criados, ignorados ou falharam.
    /// </summary>
    public sealed record Response(
        int TotalProcessed,
        int Created,
        int Skipped,
        int Failed,
        string? CorrelationId,
        DateTimeOffset ProcessedAt,
        IReadOnlyList<SyncItemResult> Items);
}
