using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Application.Features.GetSmtpConfiguration;

/// <summary>
/// Feature: GetSmtpConfiguration — obtém a configuração SMTP do tenant autenticado.
/// A senha cifrada não é exposta na resposta.
/// </summary>
public static class GetSmtpConfiguration
{
    /// <summary>Query para obter a configuração SMTP do tenant.</summary>
    public sealed record Query : IQuery<Response?>;

    /// <summary>Handler que obtém a configuração SMTP.</summary>
    public sealed class Handler(
        ISmtpConfigurationStore store,
        ICurrentTenant tenant) : IQueryHandler<Query, Response?>
    {
        public async Task<Result<Response?>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var config = await store.GetByTenantAsync(tenant.Id, cancellationToken);

            if (config is null)
                return (Response?)null;

            return new Response(
                config.Id.Value,
                config.Host,
                config.Port,
                config.UseSsl,
                config.Username,
                config.FromAddress,
                config.FromName,
                config.BaseUrl,
                config.IsEnabled,
                config.CreatedAt,
                config.UpdatedAt);
        }
    }

    /// <summary>Resposta com a configuração SMTP (sem expor senha).</summary>
    public sealed record Response(
        Guid Id,
        string Host,
        int Port,
        bool UseSsl,
        string? Username,
        string FromAddress,
        string FromName,
        string? BaseUrl,
        bool IsEnabled,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
