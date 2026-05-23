namespace NexTraceOne.IdentityAccess.Application.ConfigurationKeys;

/// <summary>
/// Configuração das origens permitidas para redirecionamento após login (returnTo).
/// Previne open redirect: apenas origens explicitamente listadas são aceites além de paths relativos.
/// </summary>
public sealed class AllowedOriginsOptions
{
    public const string SectionName = "Security:AllowedOrigins";

    /// <summary>
    /// Origens absolutas permitidas para returnTo (e.g., https://app.nextraceone.com).
    /// Paths relativos (começando com '/') são sempre permitidos independentemente desta lista.
    /// </summary>
    public string[] Allowed { get; set; } = [];
}
