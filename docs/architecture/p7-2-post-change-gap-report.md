# P7.2 — Post-Change Gap Report

## O que foi resolvido nesta fase

| Item | Estado |
|---|---|
| `NotificationDelivery` expandido com `LastAttemptAt` e `NextRetryAt` | ✅ Implementado |
| `DeliveryStatus.RetryScheduled` adicionado | ✅ Implementado |
| `NotificationDelivery.ScheduleRetry()` implementado | ✅ Implementado |
| Retry deferido em `ExternalDeliveryService` (sem `Task.Delay` bloqueante) | ✅ Implementado |
| `NotificationDeliveryRetryJob` (BackgroundService) | ✅ Implementado |
| `INotificationDeliveryStore.ListScheduledForRetryAsync` | ✅ Implementado |
| `GetDeliveryHistory` handler + endpoint | ✅ Implementado |
| `GetDeliveryStatus` handler + endpoint | ✅ Implementado |
| `EmailNotificationDispatcher` consome `SmtpConfiguration` persistida | ✅ Implementado (com fallback) |
| Migration `P7_2_DeliveryRetryHistory` | ✅ Gerada |
| Frontend: tipos `DeliveryHistoryResponse`, `DeliveryStatusResponse` | ✅ Adicionados |
| Frontend: `getDeliveryHistory`, `getDeliveryStatus` API calls | ✅ Adicionadas |
| Frontend: `useDeliveryHistory`, `useDeliveryStatus` hooks | ✅ Adicionados |
| 13 novos testes passando (455 total) | ✅ Passando |
| Compilação do solution completo | ✅ Build succeeded. 0 Error(s) |

---

## O que ficou pendente após P7.2

### P7.3 — Criptografia de Senha SMTP

O `EmailNotificationDispatcher` passa `EncryptedPassword` diretamente ao `SmtpClient.Credentials` sem decifrar. O campo existe com o nome `EncryptedPassword` como sinal de intenção, mas a cifra não está implementada.

**Pending:**
- Implementar `ISmtpPasswordEncryptor` (ex.: AES-256 com chave de environment variable).
- `UpsertSmtpConfiguration.Handler` deve cifrar antes de persistir.
- `EmailNotificationDispatcher.ResolveSmtpSettingsAsync()` deve decifrar antes de usar.

### P7.3 — UI para Delivery History na NotificationCenterPage

O hook `useDeliveryHistory` existe, mas não há componente de UI que o consuma. A `NotificationCenterPage` não mostra estado de entrega por canal.

**Pending:**
- Adicionar painel "Delivery Status" na `NotificationCenterPage` ou no detalhe de uma notificação.
- Mostrar: canal, status, retryCount, lastAttemptAt, nextRetryAt, errorMessage.
- Destaque visual para `RetryScheduled` (badge) e `Failed` (cor de alerta).

### P7.3 — Destinatário de Email Resolvido no ExternalDeliveryService

O `ExternalDeliveryService` passa `recipientAddress: null` ao criar o `NotificationDelivery`. O `EmailNotificationDispatcher` verifica e retorna `false` se não há endereço → delivery é marcada como `Skipped`.

**Pending:**
- Integrar `INotificationRecipientResolver` no `ExternalDeliveryService` para resolver o endereço de email do `recipientUserId` antes de criar o delivery record.
- Sem esta integração, o email nunca é enviado na prática.

### P7.3 — Integração de Templates Persistidos no OrquestRador

O `NotificationOrchestrator` continua a usar o `NotificationTemplateResolver` em memória. Os templates persistidos na tabela `ntf_templates` (P7.1) não são consultados.

**Pending:**
- Atualizar `NotificationOrchestrator` para tentar resolver template persistido antes do resolver em memória.
- Fallback: se não existir template persistido ativo para `(EventType, Channel, Locale)`, usar o template em memória.

### P7.3 — Seeder de Templates Built-in

A tabela `ntf_templates` está vazia. Não existe seeder que pré-popule templates padrão.

**Pending:**
- Criar `NotificationTemplateSeeder` com templates para eventos principais: `IncidentCreated`, `ApprovalPending`, `BreakGlassActivated`, etc.

### P7.3 — Routing Engine com Configuração Persistida

O `NotificationRoutingEngine` continua a usar `IOptions<NotificationChannelOptions>` para decidir se Email e Teams estão habilitados. Não consulta a `DeliveryChannelConfiguration` persistida (P7.1).

**Pending:**
- Atualizar `NotificationRoutingEngine` para consultar `IDeliveryChannelConfigurationStore` e verificar o estado do canal persistido.
- Fallback para appsettings se não houver configuração persistida.

### Fase futura — Integração Slack/Webhook

- Delivery para Slack não implementado nesta fase.
- Webhook genérico configurável não implementado nesta fase.

### Fase futura — Endpoint de Retry Manual

Não existe endpoint para forçar retry de uma delivery falhada manualmente (ex.: um administrador quer reforçar entrega após corrigir SMTP).

**Pending:**
- `POST /api/v1/notifications/{id}/deliveries/{deliveryId}/retry` — endpoint para administradores.

### Fase futura — Métricas de Delivery

Não existem métricas de delivery agregadas por canal (taxa de sucesso, taxa de falha, p95 de tentativas, etc.).

---

## Limitações Residuais Conhecidas

1. **Email nunca é enviado na prática**: o `ExternalDeliveryService` cria deliveries com `recipientAddress: null`. O dispatcher retorna `false` → `MarkSkipped`. Requer integração com `INotificationRecipientResolver`.

2. **Senha SMTP não cifrada**: armazenada em claro na coluna `EncryptedPassword`. Deve ser endereçado em P7.3 antes de uso em produção.

3. **Templates em memória ainda são a fonte**: o orquestrador não consulta templates persistidos. Os templates da `ntf_templates` ficam inativos até P7.3.

4. **Routing ainda usa appsettings**: `DeliveryChannelConfiguration` persistida não afeta decisões de routing até P7.3.

---

## Próximos Passos Recomendados (P7.3)

1. Implementar `ISmtpPasswordEncryptor` com AES-256 e integrar em `UpsertSmtpConfiguration` e `EmailNotificationDispatcher`.
2. Integrar `INotificationRecipientResolver` no `ExternalDeliveryService` para resolver `recipientAddress`.
3. Atualizar `NotificationOrchestrator` para consultar templates persistidos.
4. Atualizar `NotificationRoutingEngine` para usar `IDeliveryChannelConfigurationStore`.
5. Criar `NotificationTemplateSeeder` com templates padrão.
6. Criar componente de UI de delivery history na `NotificationCenterPage`.
7. Adicionar endpoint de retry manual para administradores.
