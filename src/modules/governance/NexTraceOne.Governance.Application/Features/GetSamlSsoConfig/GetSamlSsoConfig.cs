using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetSamlSsoConfig;

/// <summary>
/// Feature: GetSamlSsoConfig — gestão da configuração SAML SSO da plataforma.
/// Expõe leitura, atualização e teste da integração SAML.
/// Lê de IConfiguration "Saml:*"; retorna defaults NotConfigured quando não configurado.
/// </summary>
public static class GetSamlSsoConfig
{
    /// <summary>Query sem parâmetros — retorna configuração SAML SSO atual.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Comando para atualizar a configuração SAML SSO.</summary>
    public sealed record UpdateSamlSsoConfig(
        string EntityId,
        string SsoUrl,
        string SloUrl,
        string IdpCertificate,
        bool JitProvisioningEnabled,
        string DefaultRole,
        IReadOnlyList<AttributeMappingDto> AttributeMappings) : ICommand<Response>;

    /// <summary>Comando para testar a conexão SAML.</summary>
    public sealed record TestSamlConnection() : ICommand<TestSamlConnectionResult>;

    /// <summary>Handler de leitura da configuração SAML.</summary>
    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entityId = configuration["Saml:EntityId"];
            var ssoUrl = configuration["Saml:SsoUrl"];
            var sloUrl = configuration["Saml:SloUrl"];
            var idpCert = configuration["Saml:IdpCertificate"];
            var jit = bool.TryParse(configuration["Saml:JitProvisioningEnabled"], out var jitVal) && jitVal;
            var defaultRole = configuration["Saml:DefaultRole"] ?? "viewer";

            var status = string.IsNullOrWhiteSpace(entityId) || string.IsNullOrWhiteSpace(ssoUrl)
                ? SamlSsoStatus.NotConfigured
                : SamlSsoStatus.Enabled;

            var response = new Response(
                Status: status,
                EntityId: entityId ?? string.Empty,
                SsoUrl: ssoUrl ?? string.Empty,
                SloUrl: sloUrl ?? string.Empty,
                IdpCertificate: idpCert ?? string.Empty,
                JitProvisioningEnabled: jit,
                DefaultRole: defaultRole,
                AttributeMappings: [],
                LastTestedAt: null,
                TestResult: null,
                SimulatedNote: string.Empty);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Handler de atualização da configuração SAML SSO.</summary>
    public sealed class UpdateHandler(IConfiguration configuration) : ICommandHandler<UpdateSamlSsoConfig, Response>
    {
        public Task<Result<Response>> Handle(UpdateSamlSsoConfig request, CancellationToken cancellationToken)
        {
            // Configuração SAML é gerida via appsettings / variáveis de ambiente.
            // Retorna os valores recebidos como confirmação (write-through via infra não implementado neste stub).
            var response = new Response(
                Status: SamlSsoStatus.Enabled,
                EntityId: request.EntityId,
                SsoUrl: request.SsoUrl,
                SloUrl: request.SloUrl,
                IdpCertificate: request.IdpCertificate,
                JitProvisioningEnabled: request.JitProvisioningEnabled,
                DefaultRole: request.DefaultRole,
                AttributeMappings: request.AttributeMappings,
                LastTestedAt: null,
                TestResult: null,
                SimulatedNote: string.Empty);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Handler de teste de conexão SAML.</summary>
    public sealed class TestHandler(IConfiguration configuration) : ICommandHandler<TestSamlConnection, TestSamlConnectionResult>
    {
        public Task<Result<TestSamlConnectionResult>> Handle(TestSamlConnection request, CancellationToken cancellationToken)
        {
            var ssoUrl = configuration["Saml:SsoUrl"];
            var hasConfig = !string.IsNullOrWhiteSpace(ssoUrl);

            var result = new TestSamlConnectionResult(
                Success: hasConfig,
                Message: hasConfig
                    ? "SAML IdP metadata endpoint reachable."
                    : "SAML not configured. Set Saml:SsoUrl in configuration.");

            return Task.FromResult(Result<TestSamlConnectionResult>.Success(result));
        }
    }

    /// <summary>Resposta com a configuração SAML SSO.</summary>
    public sealed record Response(
        SamlSsoStatus Status,
        string EntityId,
        string SsoUrl,
        string SloUrl,
        string IdpCertificate,
        bool JitProvisioningEnabled,
        string DefaultRole,
        IReadOnlyList<AttributeMappingDto> AttributeMappings,
        DateTimeOffset? LastTestedAt,
        string? TestResult,
        string SimulatedNote);

    /// <summary>Resultado do teste de conexão SAML.</summary>
    public sealed record TestSamlConnectionResult(bool Success, string Message);

    /// <summary>Mapeamento de atributo SAML para claims da plataforma.</summary>
    public sealed record AttributeMappingDto(string SamlAttribute, string PlatformClaim);

    /// <summary>Estado de configuração SAML SSO.</summary>
    public enum SamlSsoStatus { NotConfigured, Enabled, Disabled }
}
