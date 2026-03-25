using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NexTraceOne.BuildingBlocks.Security.CookieSession;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security;

/// <summary>
/// Registra autenticação JWT e API key, autorização baseada em permissões, resolução de tenant
/// e componentes de segurança transversais.
///
/// A configuração JWT é lida primeiro de "Jwt:*" (padrão do appsettings) e depois
/// de "Security:Jwt:*" como fallback, garantindo compatibilidade entre módulos.
///
/// Autenticação dual: utiliza um PolicyScheme ("Smart") como esquema padrão que
/// seleciona automaticamente entre API key (quando header X-Api-Key presente) e JWT Bearer
/// (para todos os demais requests). Isso permite integrações sistema-a-sistema via API key
/// sem impactar o fluxo interativo existente baseado em JWT.
///
/// API keys são configuradas na seção "Security:ApiKeys" do appsettings.json.
/// Cada key vincula um clientId a um tenant e conjunto de permissões.
///
/// O modelo de autorização utiliza policies dinâmicas baseadas em permissão granular:
/// cada endpoint pode exigir uma permissão específica via RequirePermission("permission.code"),
/// resolvida automaticamente pelo <see cref="PermissionPolicyProvider"/>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Prioridade: Jwt:* (raiz, padrão do appsettings) → Security:Jwt:* (fallback) → default
        var issuer = configuration["Jwt:Issuer"]
            ?? configuration["Security:Jwt:Issuer"]
            ?? "NexTraceOne";

        var audience = configuration["Jwt:Audience"]
            ?? configuration["Security:Jwt:Audience"]
            ?? "NexTraceOne.Clients";

        // Segurança: a chave JWT DEVE ser configurada externamente em todos os ambientes.
        // Configurar via variável de ambiente Jwt__Secret, dotnet user-secrets ou gestor de segredos.
        var signingKey = configuration["Jwt:Secret"]
            ?? configuration["Security:Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "JWT signing key is not configured. Set 'Jwt:Secret' or 'Security:Jwt:SigningKey' in configuration, " +
                "or define it via the 'Jwt__Secret' environment variable or dotnet user-secrets. " +
                "A signing key is mandatory in all environments. " +
                "Generate a strong key with: openssl rand -base64 48");
        }

        services.Configure<CookieSessionOptions>(configuration.GetSection(CookieSessionOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<CurrentTenantAccessor>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenantAccessor>());
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        // PolicyScheme "Smart" é o esquema padrão: detecta header X-Api-Key para
        // rotear automaticamente entre API key e JWT Bearer, garantindo que ambos
        // os mecanismos coexistam sem configuração adicional nos endpoints.
        const string smartSchemeName = "Smart";

        services.AddAuthentication(smartSchemeName)
            .AddPolicyScheme(smartSchemeName, "JWT or API Key", policyOptions =>
            {
                policyOptions.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Headers.ContainsKey("X-Api-Key"))
                    {
                        return ApiKeyAuthenticationOptions.SchemeName;
                    }

                    return JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrWhiteSpace(context.Token))
                        {
                            return Task.CompletedTask;
                        }

                        var cookieSessionOptions = configuration
                            .GetSection(CookieSessionOptions.SectionName)
                            .Get<CookieSessionOptions>() ?? new CookieSessionOptions();

                        if (!cookieSessionOptions.Enabled)
                        {
                            return Task.CompletedTask;
                        }

                        var tokenFromCookie = context.Request.Cookies[cookieSessionOptions.AccessTokenCookieName];
                        if (!string.IsNullOrWhiteSpace(tokenFromCookie))
                        {
                            context.Token = tokenFromCookie;
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.SchemeName,
                options =>
                {
                    var apiKeysSection = configuration.GetSection("Security:ApiKeys");
                    options.ConfiguredKeys = apiKeysSection.Get<List<ApiKeyConfiguration>>() ?? [];
                });

        // Autorização baseada em permissões granulares — policies dinâmicas via PermissionPolicyProvider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        return services;
    }
}
