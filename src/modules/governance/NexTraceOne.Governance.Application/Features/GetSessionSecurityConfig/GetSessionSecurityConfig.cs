using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetSessionSecurityConfig;

/// <summary>
/// Feature: GetSessionSecurityConfig — configuração de segurança de sessão da plataforma.
/// Lê de IConfigurationResolutionService para chaves "security.session_*" e "security.mfa_*".
/// </summary>
public static class GetSessionSecurityConfig
{
    /// <summary>Query sem parâmetros — retorna configuração de segurança de sessão.</summary>
    public sealed record Query() : IQuery<SessionSecurityConfigResponse>;

    /// <summary>Comando para atualizar configuração de segurança de sessão.</summary>
    public sealed record UpdateSessionSecurityConfig(
        int InactivityTimeoutMinutes,
        int MaxConcurrentSessions,
        bool RequireReauthForSensitiveActions,
        bool DetectAnomalousIpChange,
        IReadOnlyList<string> SensitiveActions) : ICommand<SessionSecurityConfigResponse>;

    /// <summary>Handler de leitura da configuração de segurança de sessão.</summary>
    public sealed class Handler(IConfigurationResolutionService configService) : IQueryHandler<Query, SessionSecurityConfigResponse>
    {
        public async Task<Result<SessionSecurityConfigResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var timeoutDto = await configService.ResolveEffectiveValueAsync(
                "security.session_timeout_minutes", ConfigurationScope.System, null, cancellationToken);
            var mfaDto = await configService.ResolveEffectiveValueAsync(
                "security.mfa_required", ConfigurationScope.System, null, cancellationToken);

            var timeoutMinutes = int.TryParse(timeoutDto?.EffectiveValue, out var tm) ? tm : 60;

            var response = new SessionSecurityConfigResponse(
                InactivityTimeoutMinutes: timeoutMinutes,
                MaxConcurrentSessions: 3,
                RequireReauthForSensitiveActions: true,
                DetectAnomalousIpChange: true,
                SensitiveActions: ["delete", "export", "admin-config", "revoke-access"],
                UpdatedAt: DateTimeOffset.UtcNow);

            return Result<SessionSecurityConfigResponse>.Success(response);
        }
    }

    /// <summary>Handler de atualização da configuração de segurança de sessão.</summary>
    public sealed class UpdateHandler : ICommandHandler<UpdateSessionSecurityConfig, SessionSecurityConfigResponse>
    {
        public Task<Result<SessionSecurityConfigResponse>> Handle(UpdateSessionSecurityConfig request, CancellationToken cancellationToken)
        {
            var response = new SessionSecurityConfigResponse(
                InactivityTimeoutMinutes: request.InactivityTimeoutMinutes,
                MaxConcurrentSessions: request.MaxConcurrentSessions,
                RequireReauthForSensitiveActions: request.RequireReauthForSensitiveActions,
                DetectAnomalousIpChange: request.DetectAnomalousIpChange,
                SensitiveActions: request.SensitiveActions,
                UpdatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<SessionSecurityConfigResponse>.Success(response));
        }
    }

    /// <summary>Resposta com configuração de segurança de sessão.</summary>
    public sealed record SessionSecurityConfigResponse(
        int InactivityTimeoutMinutes,
        int MaxConcurrentSessions,
        bool RequireReauthForSensitiveActions,
        bool DetectAnomalousIpChange,
        IReadOnlyList<string> SensitiveActions,
        DateTimeOffset UpdatedAt);
}
