using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.BuildingBlocks.Security.Authorization;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security;

/// <summary>
/// Registra autenticação JWT, autorização baseada em permissões, resolução de tenant
/// e componentes de segurança transversais.
///
/// A configuração JWT é lida primeiro de "Jwt:*" (padrão do appsettings) e depois
/// de "Security:Jwt:*" como fallback, garantindo compatibilidade entre módulos.
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

        // Segurança: a chave JWT DEVE ser configurada externamente em ambientes não-Development.
        // Em Development, permite fallback para chave conhecida — apenas para conveniência local.
        // Em qualquer outro ambiente, a ausência da chave impede a inicialização para evitar
        // que tokens possam ser forjados com uma chave publicamente conhecida.
        var signingKey = configuration["Jwt:Secret"]
            ?? configuration["Security:Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "JWT signing key is not configured. Set 'Jwt:Secret' or 'Security:Jwt:SigningKey' in configuration, " +
                    "or define it via environment variables. A signing key is mandatory in non-development environments.");
            }

            signingKey = "development-signing-key-development-signing-key-1234567890";
        }

        services.AddHttpContextAccessor();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<CurrentTenantAccessor>();
        services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenantAccessor>());
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            });

        // Autorização baseada em permissões granulares — policies dinâmicas via PermissionPolicyProvider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        return services;
    }
}
