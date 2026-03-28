using System.Text.Json;
using NexTraceOne.Integrations.Application.Abstractions;

namespace NexTraceOne.Integrations.Application.Parsing;

/// <summary>
/// Implementação genérica de <see cref="IIngestionPayloadParser"/>.
/// Suporta múltiplos aliases de campo (case-insensitive) para acomodar diferentes
/// sistemas de CI/CD sem configuração extra.
/// </summary>
public sealed class GenericIngestionPayloadParser : IIngestionPayloadParser
{
    // Aliases de campos reconhecidos — ordem define prioridade de matching
    private static readonly string[] ServiceNameAliases   = ["serviceName", "service_name", "service"];
    private static readonly string[] EnvironmentAliases   = ["environment", "env"];
    private static readonly string[] VersionAliases       = ["version"];
    private static readonly string[] CommitShaAliases     = ["commitSha", "commit_sha", "commit", "sha"];
    private static readonly string[] ChangeTypeAliases    = ["changeType", "change_type", "type"];
    private static readonly string[] TimestampAliases     = ["timestamp", "deployedAt", "deployed_at", "createdAt", "created_at", "eventTime", "event_time"];

    // Campos reservados que não devem ir para AdditionalMetadata
    private static readonly HashSet<string> ReservedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "serviceName", "service_name", "service",
        "environment", "env",
        "version",
        "commitSha", "commit_sha", "commit", "sha",
        "changeType", "change_type", "type",
        "timestamp", "deployedAt", "deployed_at", "createdAt", "created_at", "eventTime", "event_time"
    };

    /// <inheritdoc/>
    public IngestionParsedResult ParseDeployPayload(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return Failure("Payload is empty or null");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawPayload);
        }
        catch (JsonException ex)
        {
            return Failure($"Malformed JSON: {ex.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Failure("Payload root element is not a JSON object");
            }

            var root = document.RootElement;

            var serviceName  = TryGetString(root, ServiceNameAliases);
            var environment  = TryGetString(root, EnvironmentAliases);
            var version      = TryGetString(root, VersionAliases);
            var commitSha    = TryGetString(root, CommitShaAliases);
            var changeType   = TryGetString(root, ChangeTypeAliases);
            var timestampRaw = TryGetString(root, TimestampAliases);

            DateTimeOffset? timestamp = null;
            if (timestampRaw is not null && DateTimeOffset.TryParse(timestampRaw, out var parsedTs))
            {
                timestamp = parsedTs;
            }

            // At least one recognizable semantic field must be present
            if (serviceName is null && environment is null && version is null
                && commitSha is null && changeType is null && timestamp is null)
            {
                return Failure("No recognizable semantic fields found in payload");
            }

            var additionalMetadata = ExtractAdditionalMetadata(root);

            return new IngestionParsedResult(
                ServiceName: serviceName,
                Environment: environment,
                Version: version,
                CommitSha: commitSha,
                ChangeType: changeType,
                Timestamp: timestamp,
                AdditionalMetadata: additionalMetadata,
                IsSuccessful: true,
                ErrorMessage: null);
        }
    }

    private static string? TryGetString(JsonElement root, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            // JsonElement.TryGetProperty is case-sensitive; iterate properties for case-insensitive matching
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, alias, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static Dictionary<string, string> ExtractAdditionalMetadata(JsonElement root)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            if (ReservedFields.Contains(property.Name))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                var value = property.Value.GetString();
                if (value is not null)
                {
                    metadata[property.Name] = value;
                }
            }
            else if (property.Value.ValueKind != JsonValueKind.Object
                     && property.Value.ValueKind != JsonValueKind.Array)
            {
                metadata[property.Name] = property.Value.ToString();
            }
        }

        return metadata;
    }

    private static IngestionParsedResult Failure(string errorMessage) =>
        new(ServiceName: null,
            Environment: null,
            Version: null,
            CommitSha: null,
            ChangeType: null,
            Timestamp: null,
            AdditionalMetadata: [],
            IsSuccessful: false,
            ErrorMessage: errorMessage);
}
