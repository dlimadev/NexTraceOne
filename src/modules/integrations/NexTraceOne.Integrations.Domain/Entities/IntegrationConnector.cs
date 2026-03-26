using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para IntegrationConnector.
/// </summary>
public sealed record IntegrationConnectorId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agregado que representa um conector de integração no Integration Hub.
/// Conectores são pontos de entrada de dados externos para o NexTraceOne:
/// CI/CD, Observability, ITSM, Gateways, Event Streams, etc.
///
/// Owner: módulo Integrations (extraído de Governance em P2.1).
/// </summary>
public sealed class IntegrationConnector : Entity<IntegrationConnectorId>
{
    /// <summary>Nome técnico único do conector (ex: "github-cicd").</summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>Tipo do conector (ex: "CI/CD", "Telemetry", "Incidents").</summary>
    public string ConnectorType { get; private set; } = string.Empty;

    /// <summary>Descrição do conector.</summary>
    public string? Description { get; private set; }

    /// <summary>Provedor do conector (ex: "GitHub", "Datadog", "PagerDuty").</summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>Endpoint de conexão.</summary>
    public string? Endpoint { get; private set; }

    /// <summary>Estado atual do conector.</summary>
    public ConnectorStatus Status { get; private set; }

    /// <summary>Estado de saúde do conector.</summary>
    public ConnectorHealth Health { get; private set; }

    /// <summary>Data/hora UTC da última execução bem-sucedida.</summary>
    public DateTimeOffset? LastSuccessAt { get; private set; }

    /// <summary>Data/hora UTC da última falha.</summary>
    public DateTimeOffset? LastErrorAt { get; private set; }

    /// <summary>Mensagem do último erro.</summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>Lag de frescura em minutos.</summary>
    public int? FreshnessLagMinutes { get; private set; }

    /// <summary>Total de execuções.</summary>
    public long TotalExecutions { get; private set; }

    /// <summary>Execuções bem-sucedidas.</summary>
    public long SuccessfulExecutions { get; private set; }

    /// <summary>Execuções com falha.</summary>
    public long FailedExecutions { get; private set; }

    /// <summary>Environment this connector operates in (ex: "Production", "Staging").</summary>
    public string Environment { get; private set; } = "Production";

    /// <summary>Authentication mode used by the connector (ex: "OAuth2 App Token", "API Key").</summary>
    public string AuthenticationMode { get; private set; } = "Not configured";

    /// <summary>Polling mode of the connector (ex: "Webhook", "Polling", "Webhook + Polling").</summary>
    public string PollingMode { get; private set; } = "Not configured";

    /// <summary>Teams allowed to use this connector (stored as JSON).</summary>
    public IReadOnlyList<string> AllowedTeams { get; private set; } = [];

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    private IntegrationConnector() { }

    /// <summary>
    /// Cria um novo conector de integração.
    /// </summary>
    public static IntegrationConnector Create(
        string name,
        string connectorType,
        string? description,
        string provider,
        string? endpoint,
        string? environment,
        string? authenticationMode,
        string? pollingMode,
        IReadOnlyList<string>? allowedTeams,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 200, nameof(name));
        Guard.Against.NullOrWhiteSpace(connectorType, nameof(connectorType));
        Guard.Against.StringTooLong(connectorType, 100, nameof(connectorType));
        Guard.Against.NullOrWhiteSpace(provider, nameof(provider));
        Guard.Against.StringTooLong(provider, 100, nameof(provider));

        return new IntegrationConnector
        {
            Id = new IntegrationConnectorId(Guid.NewGuid()),
            Name = name.Trim(),
            ConnectorType = connectorType.Trim(),
            Description = description?.Trim(),
            Provider = provider.Trim(),
            Endpoint = endpoint?.Trim(),
            Environment = environment?.Trim() ?? "Production",
            AuthenticationMode = authenticationMode?.Trim() ?? "Not configured",
            PollingMode = pollingMode?.Trim() ?? "Not configured",
            AllowedTeams = allowedTeams ?? [],
            Status = ConnectorStatus.Active,
            Health = ConnectorHealth.Unknown,
            TotalExecutions = 0,
            SuccessfulExecutions = 0,
            FailedExecutions = 0,
            CreatedAt = utcNow
        };
    }

    /// <summary>Regista uma execução bem-sucedida.</summary>
    public void RecordSuccess(DateTimeOffset utcNow)
    {
        LastSuccessAt = utcNow;
        TotalExecutions++;
        SuccessfulExecutions++;
        Health = ConnectorHealth.Healthy;
        UpdateFreshnessLag(utcNow);
        UpdatedAt = utcNow;
    }

    /// <summary>Regista uma falha de execução.</summary>
    public void RecordFailure(string? errorMessage, DateTimeOffset utcNow)
    {
        LastErrorAt = utcNow;
        LastErrorMessage = errorMessage;
        TotalExecutions++;
        FailedExecutions++;
        Health = ConnectorHealth.Unhealthy;
        UpdateFreshnessLag(utcNow);
        UpdatedAt = utcNow;
    }

    /// <summary>Marca o conector como degradado.</summary>
    public void MarkDegraded(DateTimeOffset utcNow)
    {
        Health = ConnectorHealth.Degraded;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza o lag de frescura.</summary>
    public void UpdateFreshnessLag(DateTimeOffset utcNow)
    {
        if (LastSuccessAt is null)
        {
            FreshnessLagMinutes = null;
            return;
        }

        var lag = utcNow - LastSuccessAt.Value;
        FreshnessLagMinutes = (int)lag.TotalMinutes;

        // Marca como degradado se lag > 4 horas
        if (lag.TotalHours > 4 && Health == ConnectorHealth.Healthy)
        {
            Health = ConnectorHealth.Degraded;
        }
    }

    /// <summary>Desativa o conector.</summary>
    public void Disable(DateTimeOffset utcNow)
    {
        Status = ConnectorStatus.Disabled;
        UpdatedAt = utcNow;
    }

    /// <summary>Ativa o conector.</summary>
    public void Activate(DateTimeOffset utcNow)
    {
        Status = ConnectorStatus.Active;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza o endpoint do conector.</summary>
    public void UpdateEndpoint(string? endpoint, DateTimeOffset utcNow)
    {
        Endpoint = endpoint?.Trim();
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza a configuração do conector.</summary>
    public void UpdateConfiguration(string? environment, string? authenticationMode, string? pollingMode, IReadOnlyList<string>? allowedTeams, DateTimeOffset utcNow)
    {
        if (environment is not null) Environment = environment.Trim();
        if (authenticationMode is not null) AuthenticationMode = authenticationMode.Trim();
        if (pollingMode is not null) PollingMode = pollingMode.Trim();
        if (allowedTeams is not null) AllowedTeams = allowedTeams;
        UpdatedAt = utcNow;
    }
}
