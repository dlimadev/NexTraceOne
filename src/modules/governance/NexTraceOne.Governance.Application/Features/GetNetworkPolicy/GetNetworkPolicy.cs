using Microsoft.Extensions.Configuration;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetNetworkPolicy;

/// <summary>
/// Feature: GetNetworkPolicy — retorna a política de rede activa na plataforma.
/// Lê o modo de isolamento de rede (Off / Restricted / AirGap) e lista todas
/// as chamadas externas possíveis com o respectivo estado (activa, bloqueada, configurada).
/// Permite que administradores verifiquem a postura de rede sem acesso ao servidor.
/// </summary>
public static class GetNetworkPolicy
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(IConfiguration configuration) : IQueryHandler<Query, Response>
    {
        private static readonly IReadOnlyList<ExternalCallDefinition> AllExternalCalls = new List<ExternalCallDefinition>
        {
            new("OpenAI",          "AiRuntime:OpenAI:Enabled",         "AI externo (OpenAI API)",              "AiRuntime__OpenAI__Enabled"),
            new("Webhooks",        "Webhooks:OutboundEnabled",         "Notificações externas via webhook",    "Webhooks__OutboundEnabled"),
            new("SMTP",            "Smtp:Host",                        "Notificações por email",               "Smtp__Host"),
            new("OTelCollector",   "OpenTelemetry:Endpoint",           "Observabilidade (OTel Collector)",     "OpenTelemetry__Endpoint"),
            new("OllamaRemote",    "AiRuntime:Ollama:Host",            "LLM remoto em servidor separado",      "AiRuntime__Ollama__Host"),
            new("GitHubWebhook",   "Integrations:GitHub:Enabled",      "Integração CI/CD (GitHub)",            "Integrations__GitHub__Enabled"),
            new("GitLabWebhook",   "Integrations:GitLab:Enabled",      "Integração CI/CD (GitLab)",            "Integrations__GitLab__Enabled"),
            new("AzureDevOps",     "Integrations:AzureDevOps:Enabled", "Integração CI/CD (Azure DevOps)",      "Integrations__AzureDevOps__Enabled"),
        };

        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var mode = configuration["Platform:NetworkIsolation:Mode"] ?? "Off";

            var calls = AllExternalCalls.Select(def =>
            {
                var configured = IsConfigured(configuration, def.ConfigKey);
                var blocked    = mode == "AirGap" || (mode == "Restricted" && !configured);

                return new ExternalCallStatus(
                    Key:         def.Key,
                    Description: def.Description,
                    EnvVar:      def.EnvVar,
                    Configured:  configured,
                    Blocked:     blocked);
            }).ToList();

            var activeCount  = calls.Count(c => c.Configured && !c.Blocked);
            var blockedCount = calls.Count(c => c.Blocked);

            return Task.FromResult(Result<Response>.Success(new Response(
                Mode:          mode,
                ActiveCalls:   activeCount,
                BlockedCalls:  blockedCount,
                Calls:         calls,
                AuditedAt:     DateTimeOffset.UtcNow)));
        }

        private static bool IsConfigured(IConfiguration cfg, string key)
        {
            var value = cfg[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !value.Equals("false", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed record Response(
        string Mode,
        int ActiveCalls,
        int BlockedCalls,
        IReadOnlyList<ExternalCallStatus> Calls,
        DateTimeOffset AuditedAt);

    public sealed record ExternalCallStatus(
        string Key,
        string Description,
        string EnvVar,
        bool Configured,
        bool Blocked);

    private sealed record ExternalCallDefinition(
        string Key,
        string ConfigKey,
        string Description,
        string EnvVar);
}
