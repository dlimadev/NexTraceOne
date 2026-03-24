# Notifications — Operations and Incidents

## Eventos Operacionais

| Tipo | Severidade | Requer Ação | Handler |
|------|-----------|-------------|---------|
| IncidentCreated | Critical | Sim | IncidentNotificationHandler |
| IncidentEscalated | Critical | Sim | IncidentNotificationHandler |
| IncidentResolved | Info | Não | IncidentNotificationHandler |
| AnomalyDetected | Warning | Sim | IncidentNotificationHandler |
| HealthDegradation | Warning | Sim | IncidentNotificationHandler |

## Templates

### IncidentCreated
- **Título**: `Incident created — {ServiceName}`
- **Mensagem**: `A new incident with severity {IncidentSeverity} has been created for service {ServiceName}. Investigate and take action.`
- **Deep link**: `/incidents/{IncidentId}`

### IncidentEscalated
- **Título**: `Incident escalated — {ServiceName}`
- **Mensagem**: `An incident for service {ServiceName} has been escalated from {PreviousSeverity} to {NewSeverity}. Immediate attention required.`
- **Deep link**: `/incidents/{IncidentId}`

### IncidentResolved
- **Título**: `Incident resolved — {ServiceName}`
- **Mensagem**: `The incident for service {ServiceName} has been resolved by {ResolvedBy}.`
- **Deep link**: `/incidents/{IncidentId}`

### AnomalyDetected
- **Título**: `Anomaly detected — {ServiceName}`
- **Mensagem**: `A {AnomalyType} anomaly has been detected for service {ServiceName}: {Description}. Investigate promptly.`
- **Deep link**: `/operations/anomalies/{AnomalyId}`

### HealthDegradation
- **Título**: `Health degradation — {ServiceName}`
- **Mensagem**: `Service {ServiceName} health has degraded from {PreviousStatus} to {CurrentStatus}. Monitor and investigate.`
- **Deep link**: `/services/{ServiceId}/health`

## Destinatários

- **Owner do serviço** (OwnerUserId) — principal destinatário para todos os eventos operacionais
- Equipa operacional — resolução futura via RecipientTeamIds ou RecipientRoles

## Canais

- **InApp**: Sempre (todos os eventos)
- **Email**: Incidentes criados, escalados, anomalias e degradações (severidade ≥ Warning)
- **Teams**: Incidentes críticos (severidade = Critical)

## Regras

1. Incidentes resolvidos usam severidade Info — não geram alertas externos por padrão
2. Anomalias incluem tipo formatado (runtime, performance, drift) para contexto
3. Health degradation compara estado anterior e atual para clareza
4. Todos os eventos incluem deep link para navegação direta
5. Eventos sem OwnerUserId ou TenantId são descartados com log warning
