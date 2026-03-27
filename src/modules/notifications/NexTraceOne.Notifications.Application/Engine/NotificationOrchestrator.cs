using Microsoft.Extensions.Logging;

using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Contracts.ServiceInterfaces;
using NexTraceOne.Notifications.Domain.Entities;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Engine;

/// <summary>
/// Orquestrador central da engine de notificações do NexTraceOne.
/// Recebe pedidos de notificação, decide se notifica, resolve template e destinatários,
/// aplica deduplicação básica, persiste na central interna, e agenda entrega externa
/// para canais elegíveis (email, Teams) via IExternalDeliveryService.
/// Ponto único de decisão — nenhum módulo deve criar notificações fora desta engine.
/// P7.3: regista eventos auditáveis após criação de cada notificação.
/// </summary>
public sealed class NotificationOrchestrator(
    INotificationStore store,
    INotificationTemplateResolver templateResolver,
    INotificationDeduplicationService deduplicationService,
    INotificationAuditService notificationAuditService,
    IExternalDeliveryService? externalDeliveryService,
    ILogger<NotificationOrchestrator> logger) : INotificationOrchestrator
{
    /// <inheritdoc/>
    public async Task<NotificationResult> ProcessAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validação básica
        if (string.IsNullOrWhiteSpace(request.EventType))
            return new NotificationResult(false, "EventType is required.");

        if (string.IsNullOrWhiteSpace(request.SourceModule))
            return new NotificationResult(false, "SourceModule is required.");

        if (!request.TenantId.HasValue || request.TenantId.Value == Guid.Empty)
            return new NotificationResult(false, "TenantId is required.");

        // 2. Resolver destinatários
        var recipientIds = ResolveRecipients(request);
        if (recipientIds.Count == 0)
        {
            logger.LogWarning(
                "No recipients resolved for notification {EventType} from {SourceModule}. Skipping.",
                request.EventType, request.SourceModule);
            return new NotificationResult(false, "No recipients could be resolved.");
        }

        // 3. Resolver template (título, mensagem, categoria, severidade)
        var parameters = ExtractParameters(request);
        var template = ResolveTemplate(request, parameters);

        // 4. Criar notificações por destinatário com deduplicação
        var createdIds = new List<Guid>();
        foreach (var recipientId in recipientIds)
        {
            // 4a. Deduplicação básica
            var isDuplicate = await deduplicationService.IsDuplicateAsync(
                request.TenantId.Value,
                recipientId,
                request.EventType,
                request.SourceEntityId,
                windowMinutes: 5,
                cancellationToken);

            if (isDuplicate)
            {
                logger.LogDebug(
                    "Skipping duplicate notification {EventType} for user {UserId} and entity {EntityId}.",
                    request.EventType, recipientId, request.SourceEntityId);
                continue;
            }

            // 4b. Criar notificação com rastreabilidade completa
            var notification = Notification.Create(
                tenantId: request.TenantId.Value,
                recipientUserId: recipientId,
                eventType: request.EventType,
                category: template.Category,
                severity: template.Severity,
                title: template.Title,
                message: template.Message,
                sourceModule: request.SourceModule,
                sourceEntityType: request.SourceEntityType,
                sourceEntityId: request.SourceEntityId,
                environmentId: request.EnvironmentId,
                actionUrl: request.ActionUrl,
                requiresAction: template.RequiresAction,
                payloadJson: request.PayloadJson,
                expiresAt: request.ExpiresAt,
                sourceEventId: request.SourceEventId);

            await store.AddAsync(notification, cancellationToken);
            createdIds.Add(notification.Id.Value);

            // 4c. Registar evento auditável — criação de notificação (P7.3)
            await RecordAuditAsync(
                entry: new NotificationAuditEntry
                {
                    TenantId = request.TenantId.Value,
                    ActionType = template.Severity >= NotificationSeverity.Critical
                        ? NotificationAuditActions.CriticalNotificationGenerated
                        : NotificationAuditActions.NotificationGenerated,
                    ResourceId = notification.Id.Value.ToString(),
                    ResourceType = "Notification",
                    Description = $"Notification created: {request.EventType} from {request.SourceModule}" +
                                  $"{(request.SourceEventId is not null ? $" (sourceEventId={request.SourceEventId})" : string.Empty)}",
                    PayloadJson = request.PayloadJson
                },
                cancellationToken);

            // 4d. Agendar entrega externa (email, Teams) se o serviço estiver disponível
            if (externalDeliveryService is not null)
            {
                try
                {
                    await externalDeliveryService.ProcessExternalDeliveryAsync(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "External delivery failed for notification {NotificationId}. Internal notification persisted.",
                        notification.Id.Value);
                    // Não falhar a notificação interna por causa de falha de entrega externa
                }
            }

            logger.LogInformation(
                "Notification created: Type={EventType}, Recipient={UserId}, Module={SourceModule}, Entity={EntityType}/{EntityId}",
                request.EventType, recipientId, request.SourceModule,
                request.SourceEntityType, request.SourceEntityId);
        }

        // 5. Persistir todas as notificações
        if (createdIds.Count > 0)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return new NotificationResult(true) { NotificationIds = createdIds };
    }

    /// <summary>
    /// Resolve os destinatários a partir do pedido.
    /// Roteamento básico: usa utilizadores explícitos.
    /// Fallback: se nenhum destinatário for especificado e há roles, loga aviso.
    /// </summary>
    private static List<Guid> ResolveRecipients(NotificationRequest request)
    {
        var recipients = new HashSet<Guid>();

        // Destinatários explícitos (prioritário)
        if (request.RecipientUserIds is { Count: > 0 })
        {
            foreach (var userId in request.RecipientUserIds)
            {
                if (userId != Guid.Empty)
                    recipients.Add(userId);
            }
        }

        return [.. recipients];
    }

    /// <summary>
    /// Resolve o template — usa o template resolver se há tipo conhecido,
    /// caso contrário usa título/mensagem do próprio request.
    /// </summary>
    private ResolvedNotificationTemplate ResolveTemplate(
        NotificationRequest request,
        IReadOnlyDictionary<string, string> parameters)
    {
        // Se o request já traz título e mensagem, aceitar como override
        if (!string.IsNullOrWhiteSpace(request.Title) && !string.IsNullOrWhiteSpace(request.Message))
        {
            var category = ParseEnum<NotificationCategory>(request.Category, NotificationCategory.Informational);
            var severity = ParseEnum<NotificationSeverity>(request.Severity, NotificationSeverity.Info);
            return new ResolvedNotificationTemplate(
                request.Title,
                request.Message,
                category,
                severity,
                request.RequiresAction);
        }

        // Resolver via template engine
        return templateResolver.Resolve(request.EventType, parameters);
    }

    /// <summary>
    /// Extrai parâmetros contextuais do request para uso nos templates.
    /// </summary>
    private static Dictionary<string, string> ExtractParameters(NotificationRequest request)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(request.SourceEntityType))
            parameters["EntityName"] = request.SourceEntityType;

        if (!string.IsNullOrWhiteSpace(request.SourceEntityId))
            parameters["EntityId"] = request.SourceEntityId;

        if (!string.IsNullOrWhiteSpace(request.SourceModule))
            parameters["SourceModule"] = request.SourceModule;

        // Tentar extrair parâmetros adicionais do PayloadJson
        if (!string.IsNullOrWhiteSpace(request.PayloadJson))
        {
            try
            {
                var payload = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(request.PayloadJson);
                if (payload is not null)
                {
                    foreach (var (key, value) in payload)
                    {
                        parameters.TryAdd(key, value.ToString());
                    }
                }
            }
            catch
            {
                // Payload malformado — ignorar, não bloquear notificação
            }
        }

        return parameters;
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : fallback;

    /// <summary>
    /// Regista evento auditável de forma best-effort (não bloqueia o fluxo principal em caso de falha).
    /// P7.3: chamado após criação de notificação para fechar a trilha de auditoria.
    /// </summary>
    private async Task RecordAuditAsync(NotificationAuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await notificationAuditService.RecordAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Audit record failed for notification action {ActionType}/{ResourceId}. Non-blocking.",
                entry.ActionType, entry.ResourceId);
        }
    }
}
