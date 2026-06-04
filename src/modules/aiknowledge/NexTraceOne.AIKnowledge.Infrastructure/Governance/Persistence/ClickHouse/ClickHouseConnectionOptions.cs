namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.ClickHouse;

/// <summary>
/// Opções de configuração para conexão com ClickHouse.
/// </summary>
public sealed record ClickHouseConnectionOptions(
    string Host,
    string Port,
    string Database,
    string? Username,
    string? Password)
{
    /// <summary>
    /// Parseia uma connection string no formato "Host=...;Port=...;Database=...;Username=...;Password=..."
    /// </summary>
    public static ClickHouseConnectionOptions FromConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');

        string? Extract(string key)
        {
            var part = parts.FirstOrDefault(p => p.Trim().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            return part?.Substring(part.IndexOf('=') + 1).Trim();
        }

        return new ClickHouseConnectionOptions(
            Host: Extract("Host") ?? "localhost",
            Port: Extract("Port") ?? "8123",
            Database: Extract("Database") ?? "default",
            Username: Extract("Username"),
            Password: Extract("Password"));
    }
}
