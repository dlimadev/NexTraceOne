namespace NexTraceOne.BuildingBlocks.Security.Authentication;

/// <summary>
/// Serviço de geração e validação de JWT tokens.
/// Suporta: access token (curta duração), refresh token (longa duração).
/// Claims incluídos: sub, email, name, tenant_id, permissions.
/// </summary>
public sealed class JwtTokenService
{
    // TODO: Implementar GenerateAccessToken, GenerateRefreshToken, ValidateToken
}
