# Notifications — Admin & Troubleshooting

## Controles Administrativos

### Serviços Disponíveis

| Serviço | Responsabilidade | Uso |
|---------|------------------|-----|
| INotificationMetricsService | Métricas operacionais | Dashboard, reporting |
| INotificationHealthProvider | Estado de saúde | Monitoring, alertas |
| INotificationCatalogGovernance | Governança do catálogo | Auditorias, validação |
| INotificationAuditService | Trilha de auditoria | Compliance, investigação |

### Consultas Administrativas

#### Métricas da Plataforma
```csharp
var metrics = await metricsService.GetPlatformMetricsAsync(
    tenantId, from, until, cancellationToken);
// → TotalGenerated, ByCategory, BySeverity, DeliveriesByChannel, TotalFailed
```

#### Métricas de Interação
```csharp
var interactions = await metricsService.GetInteractionMetricsAsync(
    tenantId, from, until, cancellationToken);
// → ReadRate, AcknowledgeRate, TotalEscalated, TotalSnoozed
```

#### Health da Plataforma
```csharp
var health = await healthProvider.GetHealthAsync(cancellationToken);
// → OverallStatus, Components (InAppStore, DeliveryPipeline, Email, Teams)
```

#### Governança do Catálogo
```csharp
var summary = await governance.GetGovernanceSummaryAsync(cancellationToken);
// → TotalEventTypes, TypesWithTemplate, TypesWithoutTemplate, MandatoryTypes
```

## Superfícies de Observação

### API Endpoints Existentes
| Endpoint | Descrição |
|----------|-----------|
| GET /notifications | Lista com filtros por status, category, severity |
| GET /notifications/unread-count | Contagem de não lidas |
| GET /notifications/preferences | Preferências do utilizador |

### Endpoints Recomendados para Fase Futura
| Endpoint | Descrição |
|----------|-----------|
| GET /admin/notifications/metrics | Métricas da plataforma |
| GET /admin/notifications/health | Estado de saúde |
| GET /admin/notifications/governance | Sumário de governança |
| GET /admin/notifications/delivery-failures | Falhas recentes |

## Troubleshooting

### Cenários e Diagnóstico

#### 1. Notificações não estão a ser geradas
- Verificar `TotalGenerated` nas métricas → se 0, o orchestrator não está a ser invocado
- Verificar event handlers registados no DI
- Verificar se os integration events estão a ser publicados pelos módulos de origem

#### 2. Emails não estão a ser entregues
- Verificar `EmailChannel` health → se Degraded/Unhealthy, há falhas
- Verificar configuração `Notifications:Channels:Email` (Enabled, From, SmtpHost)
- Verificar delivery log para erros específicos

#### 3. Teams não está a funcionar
- Verificar `TeamsChannel` health
- Verificar configuração `Notifications:Channels:Teams` (Enabled, WebhookUrl)
- Verificar delivery log para HTTP errors

#### 4. Backlog de deliveries crescente
- Verificar `DeliveryPipeline` health → PendingCount
- Possível causa: canal externo indisponível
- Verificar falhas recentes por canal

#### 5. Notificações obrigatórias não entregues
- Verificar `MandatoryNotificationPolicy` para o tipo de evento
- Verificar se preferências do utilizador estão a ser corretamente overridden
- Verificar routing engine logs

#### 6. Excesso de ruído (spam)
- Verificar métricas de qualidade → `AveragePerUserPerDay`
- Verificar `TopNoisyTypes` → quais tipos geram mais volume
- Verificar regras de deduplicação e suppressão
- Considerar activar digest para categorias de baixa urgência

### Health Check Integration
O `NotificationHealthProvider` pode ser integrado com:
- ASP.NET Health Checks middleware
- Kubernetes liveness/readiness probes
- Dashboard de operações do NexTraceOne
- Alertas de degradação do módulo
