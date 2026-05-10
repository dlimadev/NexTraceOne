namespace NexTraceOne.ApiHost.Options;

/// <summary>
/// Options class for ConnectionStrings configuration section.
/// Bound via AddOptions&lt;ConnectionStringsOptions&gt;().BindConfiguration("ConnectionStrings").ValidateOnStart()
/// to detect placeholder values before the app starts accepting traffic.
/// </summary>
public sealed class ConnectionStringsOptions
{
    /// <summary>Primary application database connection string.</summary>
    public string NexTraceOne { get; set; } = string.Empty;
}
