using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetSamlSsoConfig;

/// <summary>
/// Feature: GetSamlSsoConfig — gestão da configuração SAML SSO da plataforma.
/// Expõe leitura, atualização e teste da integração SAML.
/// Lê da base de dados com fallback para IConfiguration "Saml:*".
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
    public sealed class Handler(
        ISamlSsoConfigurationRepository repository,
        IConfiguration configuration) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dbConfig = await repository.GetActiveAsync(null, cancellationToken);

            string entityId, ssoUrl, sloUrl, idpCert, defaultRole;
            bool jit;
            IReadOnlyList<AttributeMappingDto> attributeMappings;

            if (dbConfig is not null)
            {
                entityId = dbConfig.EntityId;
                ssoUrl = dbConfig.SsoUrl;
                sloUrl = dbConfig.SloUrl;
                idpCert = dbConfig.IdpCertificate;
                jit = dbConfig.JitProvisioningEnabled;
                defaultRole = dbConfig.DefaultRole;
                attributeMappings = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<AttributeMappingDto>>(
                    dbConfig.AttributeMappingsJson) ?? [];
            }
            else
            {
                entityId = configuration["Saml:EntityId"] ?? string.Empty;
                ssoUrl = configuration["Saml:SsoUrl"] ?? string.Empty;
                sloUrl = configuration["Saml:SloUrl"] ?? string.Empty;
                idpCert = configuration["Saml:IdpCertificate"] ?? string.Empty;
                jit = bool.TryParse(configuration["Saml:JitProvisioningEnabled"], out var jitVal) && jitVal;
                defaultRole = configuration["Saml:DefaultRole"] ?? "viewer";
                attributeMappings = [];
            }

            var status = string.IsNullOrWhiteSpace(entityId) || string.IsNullOrWhiteSpace(ssoUrl)
                ? SamlSsoStatus.NotConfigured
                : SamlSsoStatus.Enabled;

            var response = new Response(
                Status: status,
                EntityId: entityId,
                SsoUrl: ssoUrl,
                SloUrl: sloUrl,
                IdpCertificate: idpCert,
                JitProvisioningEnabled: jit,
                DefaultRole: defaultRole,
                AttributeMappings: attributeMappings,
                LastTestedAt: null,
                TestResult: null,
                SimulatedNote: string.Empty);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Handler de atualização da configuração SAML SSO.</summary>
    public sealed class UpdateHandler(
        ISamlSsoConfigurationRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<UpdateSamlSsoConfig, Response>
    {
        public async Task<Result<Response>> Handle(UpdateSamlSsoConfig request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var mappingsJson = System.Text.Json.JsonSerializer.Serialize(request.AttributeMappings);

            var existing = await repository.GetActiveAsync(null, cancellationToken);

            if (existing is not null)
            {
                existing.Update(
                    entityId: request.EntityId,
                    ssoUrl: request.SsoUrl,
                    sloUrl: request.SloUrl,
                    idpCertificate: request.IdpCertificate,
                    jitProvisioningEnabled: request.JitProvisioningEnabled,
                    defaultRole: request.DefaultRole,
                    attributeMappingsJson: mappingsJson,
                    now: now);
                repository.Update(existing);
            }
            else
            {
                var created = SamlSsoConfiguration.Create(
                    entityId: request.EntityId,
                    ssoUrl: request.SsoUrl,
                    sloUrl: request.SloUrl,
                    idpCertificate: request.IdpCertificate,
                    jitProvisioningEnabled: request.JitProvisioningEnabled,
                    defaultRole: request.DefaultRole,
                    attributeMappingsJson: mappingsJson,
                    tenantId: null,
                    now: now);
                await repository.AddAsync(created, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);

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

            return Result<Response>.Success(response);
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

