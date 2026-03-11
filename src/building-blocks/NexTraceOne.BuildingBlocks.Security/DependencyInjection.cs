using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using System.Text;

namespace NexTraceOne.BuildingBlocks.Security;

/// <summary>
/// Registra: JWT authentication, TenantResolutionMiddleware, EncryptionService,
/// AssemblyIntegrityChecker, HardwareFingerprint.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration["Security:Jwt:Issuer"] ?? "NexTraceOne";
        var audience = configuration["Security:Jwt:Audience"] ?? "NexTraceOne.Clients";
        var signingKey = configuration["Security:Jwt:SigningKey"] ?? "development-signing-key-development-signing-key-1234567890";

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

        services.AddAuthorization();

        return services;
    }
}
