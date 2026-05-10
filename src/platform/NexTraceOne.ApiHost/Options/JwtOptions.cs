namespace NexTraceOne.ApiHost.Options;

/// <summary>
/// Options class for Jwt configuration section.
/// Bound via AddOptions&lt;JwtOptions&gt;().BindConfiguration("Jwt").ValidateOnStart()
/// to provide early startup validation of JWT settings.
/// </summary>
public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
