using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetGracefulShutdownConfig;

/// <summary>
/// Feature: GetGracefulShutdownConfig — configuração de encerramento gracioso da plataforma.
/// Lê de IConfiguration "Platform:GracefulShutdown:*".
/// </summary>
public static class GetGracefulShutdownConfig
{
    /// <summary>Query sem parâmetros — retorna configuração de graceful shutdown.</summary>
    public sealed record Query() : IQuery<GracefulShutdownConfigResponse>;

    /// <summary>Comando para atualizar a configuração de graceful shutdown.</summary>
    public sealed record UpdateGracefulShutdownConfig(
        int RequestDrainTimeoutSeconds,
        int OutboxDrainTimeoutSeconds,
        bool HealthCheckReturns503OnShutdown,
        bool AuditShutdownEvents) : ICommand<GracefulShutdownConfigResponse>;

    /// <summary>Handler de leitura da configuração de graceful shutdown.</summary>
    public sealed class Handler(IConfiguration configuration, IDateTimeProvider clock) : IQueryHandler<Query, GracefulShutdownConfigResponse>
    {
        public Task<Result<GracefulShutdownConfigResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var requestDrain = int.TryParse(configuration["Platform:GracefulShutdown:RequestDrainTimeoutSeconds"], out var rd) ? rd : 30;
            var outboxDrain = int.TryParse(configuration["Platform:GracefulShutdown:OutboxDrainTimeoutSeconds"], out var od) ? od : 15;
            var health503 = !bool.TryParse(configuration["Platform:GracefulShutdown:HealthCheckReturns503OnShutdown"], out var h5) || h5;
            var auditEvents = !bool.TryParse(configuration["Platform:GracefulShutdown:AuditShutdownEvents"], out var ae) || ae;

            var response = new GracefulShutdownConfigResponse(
                RequestDrainTimeoutSeconds: requestDrain,
                OutboxDrainTimeoutSeconds: outboxDrain,
                HealthCheckReturns503OnShutdown: health503,
                AuditShutdownEvents: auditEvents,
                UpdatedAt: clock.UtcNow);

            return Task.FromResult(Result<GracefulShutdownConfigResponse>.Success(response));
        }
    }

    /// <summary>Handler de atualização da configuração de graceful shutdown.</summary>
    public sealed class UpdateHandler(IDateTimeProvider clock) : ICommandHandler<UpdateGracefulShutdownConfig, GracefulShutdownConfigResponse>
    {
        public Task<Result<GracefulShutdownConfigResponse>> Handle(UpdateGracefulShutdownConfig request, CancellationToken cancellationToken)
        {
            var response = new GracefulShutdownConfigResponse(
                RequestDrainTimeoutSeconds: request.RequestDrainTimeoutSeconds,
                OutboxDrainTimeoutSeconds: request.OutboxDrainTimeoutSeconds,
                HealthCheckReturns503OnShutdown: request.HealthCheckReturns503OnShutdown,
                AuditShutdownEvents: request.AuditShutdownEvents,
                UpdatedAt: clock.UtcNow);

            return Task.FromResult(Result<GracefulShutdownConfigResponse>.Success(response));
        }
    }

    /// <summary>Resposta com configuração de graceful shutdown.</summary>
    public sealed record GracefulShutdownConfigResponse(
        int RequestDrainTimeoutSeconds,
        int OutboxDrainTimeoutSeconds,
        bool HealthCheckReturns503OnShutdown,
        bool AuditShutdownEvents,
        DateTimeOffset UpdatedAt);
}
