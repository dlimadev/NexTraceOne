using Microsoft.Extensions.Configuration;

namespace NexTraceOne.BuildingBlocks.Infrastructure.Configuration;

/// <summary>
/// Extensões de configuração para obtenção segura de valores obrigatórios.
/// Garante fail-fast quando configuração crítica estiver ausente, em vez
/// de silenciosamente usar valores padrão inseguros.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Obtém uma connection string pelo nome, com fallback opcional para uma chave secundária.
    /// Lança <see cref="InvalidOperationException"/> se nenhuma das chaves estiver configurada.
    /// </summary>
    /// <param name="configuration">Instância de <see cref="IConfiguration"/>.</param>
    /// <param name="name">Nome principal da connection string (ex: "IdentityDatabase").</param>
    /// <param name="fallbackName">
    /// Nome opcional de fallback (ex: "NexTraceOne" para o consolidado de operações).
    /// O fallback é aceite como parte da estratégia de consolidação de bases de dados do NexTraceOne
    /// (ver ADR-001). Não deve ser usado como fallback para credenciais genéricas.
    /// </param>
    /// <returns>A connection string resolvida.</returns>
    /// <exception cref="InvalidOperationException">
    /// Lançada se a connection string não estiver configurada em nenhuma das chaves.
    /// </exception>
    public static string GetRequiredConnectionString(
        this IConfiguration configuration,
        string name,
        string? fallbackName = null)
    {
        var value = configuration.GetConnectionString(name);

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            var fallbackValue = configuration.GetConnectionString(fallbackName);
            if (!string.IsNullOrWhiteSpace(fallbackValue))
            {
                return fallbackValue;
            }
        }

        var message = fallbackName is not null
            ? $"Connection string '{name}' (or fallback '{fallbackName}') is not configured. " +
              $"Set 'ConnectionStrings__{name}' as an environment variable or provision it via a secrets manager. " +
              "Do not use hardcoded credentials."
            : $"Connection string '{name}' is not configured. " +
              $"Set 'ConnectionStrings__{name}' as an environment variable or provision it via a secrets manager. " +
              "Do not use hardcoded credentials.";

        throw new InvalidOperationException(message);
    }
}
