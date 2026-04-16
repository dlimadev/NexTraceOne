using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Contracts.DTOs;

namespace NexTraceOne.Configuration.Application.Features.GetAuditHistoryByPrefix;

/// <summary>
/// Feature: GetAuditHistoryByPrefix — retorna a trilha de auditoria de todas as chaves de configuração
/// que começam com um determinado prefixo de namespace.
///
/// Uso típico: recuperar o histórico de alterações de todos os parâmetros do módulo de release
/// (e.g. prefixo "change.release.") para exibição na página de auditoria de parâmetros.
///
/// Valores sensíveis são mascarados antes de serem devolvidos.
/// </summary>
public static class GetAuditHistoryByPrefix
{
    /// <summary>
    /// Query para recuperar o histórico de auditoria por prefixo de chave.
    /// </summary>
    /// <param name="KeyPrefix">Prefixo da chave de configuração (e.g. "change.release.").</param>
    /// <param name="Limit">Número máximo de entradas a devolver (padrão 100).</param>
    public sealed record Query(
        string KeyPrefix,
        int Limit = 100) : IQuery<List<ConfigurationAuditEntryDto>>;

    /// <summary>Handler que busca as entradas de auditoria pelo prefixo e mascara valores sensíveis.</summary>
    public sealed class Handler(
        IConfigurationAuditRepository auditRepository,
        IConfigurationSecurityService securityService)
        : IQueryHandler<Query, List<ConfigurationAuditEntryDto>>
    {
        public async Task<Result<List<ConfigurationAuditEntryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var limit = request.Limit > 0 ? request.Limit : 100;
            var entries = await auditRepository.GetByKeyPrefixAsync(request.KeyPrefix, limit, cancellationToken);

            var dtos = entries.Select(e => new ConfigurationAuditEntryDto(
                Key: e.Key,
                Scope: e.Scope.ToString(),
                ScopeReferenceId: e.ScopeReferenceId,
                Action: e.Action,
                PreviousValue: e.IsSensitive && e.PreviousValue is not null
                    ? securityService.MaskValue(e.PreviousValue)
                    : e.PreviousValue,
                NewValue: e.IsSensitive && e.NewValue is not null
                    ? securityService.MaskValue(e.NewValue)
                    : e.NewValue,
                PreviousVersion: e.PreviousVersion,
                NewVersion: e.NewVersion,
                ChangedBy: e.ChangedBy,
                ChangedAt: e.ChangedAt,
                ChangeReason: e.ChangeReason,
                IsSensitive: e.IsSensitive)).ToList();

            return dtos;
        }
    }
}
