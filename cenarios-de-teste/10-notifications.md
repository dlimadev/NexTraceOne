# Módulo: Notifications — Cenários de Teste Funcionais

> Cobertura: Templates, Canais, Ciclo de Vida, SMTP, Delivery, Relatórios, Agendamentos

---

### TC-NOT-001 — Upsert de template de notificação (criação)

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertNotificationTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Tenant autenticado.

**Passos:**
1. Enviar comando com `Key = "release_approved"`, `Subject = "Release {{releaseName}} aprovado"`, `Body = "Olá {{userName}}, ..."`, `Channel = Email`.
2. Handler verifica que não existe template com mesma `(TenantId, Key, Channel)`.
3. Persiste e commita.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Template armazenado com `CreatedAt` preenchido pelo `AuditInterceptor`.

**Critério de Aceite:** HTTP 201 com `{ templateId }`.

---

### TC-NOT-002 — Upsert de template (atualização de existente)

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertNotificationTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Template `"release_approved" / Email` já existe para o tenant.

**Passos:**
1. Enviar mesmo `Key + Channel` com `Subject` e `Body` alterados.
2. Handler localiza e atualiza o registro existente.

**Resultado Esperado:**
- Registro atualizado; `UpdatedAt` preenchido.
- Nenhum duplicado criado.

**Critério de Aceite:** HTTP 200 com `{ templateId }` do registro original.

---

### TC-NOT-003 — Listagem de templates do tenant

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListNotificationTemplates |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:**
- 3 templates criados para tenant A; 2 para tenant B.

**Passos:**
1. Tenant A consulta `ListNotificationTemplates`.

**Resultado Esperado:**
- Retorna exatamente 3 templates.
- Nenhum template de tenant B visível.

**Critério de Aceite:** `result.Value.Count == 3`.

---

### TC-NOT-004 — Configuração SMTP válida

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertSmtpConfiguration |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Nenhuma configuração SMTP existe para o tenant.

**Passos:**
1. Enviar `Host = "smtp.exemplo.com"`, `Port = 587`, `EnableSsl = true`, `Username = "notify@empresa.com"`, `Password = "senha"`.
2. Handler valida host não vazio, porta válida (1–65535).
3. Persiste com senha criptografada via `[EncryptedField]`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- Senha armazenada cifrada no banco.

**Critério de Aceite:** HTTP 200; `GET /smtp` retorna configuração sem expor `Password`.

---

### TC-NOT-005 — Configuração SMTP com porta inválida

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertSmtpConfiguration |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Tenant autenticado.

**Passos:**
1. Enviar `Port = 0`.

**Resultado Esperado:**
- `result.IsFailure == true`, `ErrorType = Validation`.
- Mensagem: porta deve estar entre 1 e 65535.

**Critério de Aceite:** HTTP 422.

---

### TC-NOT-006 — Obtenção de configuração SMTP

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetSmtpConfiguration |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** SMTP configurado.

**Passos:**
1. Executar `GetSmtpConfiguration.Query`.

**Resultado Esperado:**
- `Host`, `Port`, `Username` retornados; `Password` **não** incluído na resposta.

**Critério de Aceite:** HTTP 200 sem campo `password`.

---

### TC-NOT-007 — Upsert de canal de entrega (Slack)

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertDeliveryChannel |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Tenant autenticado.

**Passos:**
1. Enviar `Type = Slack`, `Config = { WebhookUrl: "https://hooks.slack.com/..." }`, `Name = "Canal de Incidentes"`.
2. Handler valida `WebhookUrl` como URI válida.
3. Persiste.

**Resultado Esperado:**
- Canal criado com ID.

**Critério de Aceite:** HTTP 201; listagem retorna canal Slack.

---

### TC-NOT-008 — Listagem de canais de entrega

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListDeliveryChannels |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** 2 canais criados (Email + Slack) para o tenant.

**Passos:**
1. Executar `ListDeliveryChannels.Query`.

**Resultado Esperado:**
- Lista com 2 canais; isolamento de tenant garantido.

**Critério de Aceite:** `result.Value.Count == 2`.

---

### TC-NOT-009 — Marcar notificação como lida

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | MarkNotificationRead |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:**
- Notificação `N1` criada com `IsRead = false` para o usuário.

**Passos:**
1. Enviar `MarkNotificationRead.Command(notificationId: N1.Id)`.
2. Handler atualiza `IsRead = true`, `ReadAt = now`.

**Resultado Esperado:**
- `result.IsSuccess == true`.
- `IsRead = true` persistido.

**Critério de Aceite:** HTTP 204; GET da notificação retorna `isRead: true`.

---

### TC-NOT-010 — Marcar notificação como não lida

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | MarkNotificationUnread |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Notificação `N1` com `IsRead = true`.

**Passos:**
1. Enviar `MarkNotificationUnread.Command(N1.Id)`.

**Resultado Esperado:**
- `IsRead = false`, `ReadAt = null`.

**Critério de Aceite:** HTTP 204.

---

### TC-NOT-011 — Marcar todas as notificações como lidas

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | MarkAllNotificationsRead |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** 15 notificações não lidas para o usuário no tenant.

**Passos:**
1. Enviar `MarkAllNotificationsRead.Command`.

**Resultado Esperado:**
- Todas as 15 marcadas `IsRead = true`.

**Critério de Aceite:** `GetUnreadCount` retorna 0.

---

### TC-NOT-012 — Contagem de não lidas

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetUnreadCount |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Usuário com 7 notificações não lidas.

**Passos:**
1. Executar `GetUnreadCount.Query`.

**Resultado Esperado:**
- `result.Value.Count == 7`.

**Critério de Aceite:** HTTP 200 `{ count: 7 }`.

---

### TC-NOT-013 — Arquivar notificação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ArchiveNotification |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Notificação ativa.

**Passos:**
1. Enviar `ArchiveNotification.Command(notificationId)`.
2. Handler seta `IsArchived = true`.

**Resultado Esperado:**
- Notificação arquivada; não aparece em `ListNotifications` padrão.

**Critério de Aceite:** HTTP 204; listagem padrão exclui arquivadas.

---

### TC-NOT-014 — Dispensar notificação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | DismissNotification |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Notificação ativa.

**Passos:**
1. Enviar `DismissNotification.Command(notificationId)`.

**Resultado Esperado:**
- `IsDismissed = true`.

**Critério de Aceite:** HTTP 204.

---

### TC-NOT-015 — Sonecas (snooze) de notificação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | SnoozeNotification |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Notificação ativa.

**Passos:**
1. Enviar `SnoozeNotification.Command(notificationId, snoozeUntil: now + 2h)`.
2. Handler persiste `SnoozedUntil`.

**Resultado Esperado:**
- Notificação não aparece em listagem até `SnoozedUntil`.

**Critério de Aceite:** HTTP 204; listagem exclui snoozed.

---

### TC-NOT-016 — Busca de notificação por ID

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetNotificationById |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Notificação `N1` criada.

**Passos:**
1. Executar `GetNotificationById.Query(N1.Id)`.

**Resultado Esperado:**
- Retorna dados completos de `N1`.

**Critério de Aceite:** HTTP 200 com `id` correspondente.

---

### TC-NOT-017 — Busca de notificação de outro tenant (isolamento)

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetNotificationById |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Notificação `N_B` pertence ao tenant B; usuário autenticado como tenant A.

**Passos:**
1. Executar `GetNotificationById.Query(N_B.Id)` com token do tenant A.

**Resultado Esperado:**
- `result.IsFailure == true`, `ErrorType = NotFound`.

**Critério de Aceite:** HTTP 404 (nunca 403, para não vazar existência).

---

### TC-NOT-018 — Trilha de entrega de notificação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetNotificationTrail |
| **Tipo** | Integração |
| **Prioridade** | Alta |

**Pré-condições:** Notificação `N1` com 3 tentativas de entrega registradas.

**Passos:**
1. Executar `GetNotificationTrail.Query(N1.Id)`.

**Resultado Esperado:**
- Lista com 3 entradas de trilha (timestamp, canal, status, erro se aplicável).

**Critério de Aceite:** HTTP 200 com `trail.length == 3`.

---

### TC-NOT-019 — Relatório de efetividade de entrega

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetNotificationEffectivenessReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** 100 notificações enviadas — 80 lidas, 15 dispensadas, 5 ignoradas.

**Passos:**
1. Executar `GetNotificationEffectivenessReport.Query(period: last30Days)`.

**Resultado Esperado:**
- `OpenRate = 80%`, `DismissRate = 15%`, `IgnoreRate = 5%`.

**Critério de Aceite:** HTTP 200 com métricas calculadas.

---

### TC-NOT-020 — Relatório de entregas (delivery report)

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | GetNotificationDeliveryReport |
| **Tipo** | Integração |
| **Prioridade** | Média |

**Pré-condições:** Período com entregas via Email (50), Slack (30), falhas (5).

**Passos:**
1. Executar `GetNotificationDeliveryReport.Query`.

**Resultado Esperado:**
- Total enviadas: 85; falhas: 5; `DeliveryRate ≈ 94%`.
- Breakdown por canal.

**Critério de Aceite:** HTTP 200.

---

### TC-NOT-021 — Criar relatório agendado

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | CreateScheduledReport |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Tenant autenticado.

**Passos:**
1. Enviar `CreateScheduledReport.Command(name: "Relatório Semanal", cronExpression: "0 8 * * 1", recipients: ["cto@empresa.com"], reportType: "ReleaseHealth")`.
2. Handler valida cron expression.
3. Persiste.

**Resultado Esperado:**
- Relatório agendado criado com `IsActive = true`.

**Critério de Aceite:** HTTP 201.

---

### TC-NOT-022 — Cron expression inválida em agendamento

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | CreateScheduledReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. Enviar `cronExpression = "nao-eh-cron"`.

**Resultado Esperado:**
- `ErrorType = Validation`.

**Critério de Aceite:** HTTP 422.

---

### TC-NOT-023 — Ativar/desativar relatório agendado

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ToggleScheduledReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Relatório `R1` ativo.

**Passos:**
1. Enviar `ToggleScheduledReport.Command(R1.Id, active: false)`.

**Resultado Esperado:**
- `IsActive = false`; job de agendamento ignorará relatório.

**Critério de Aceite:** HTTP 200.

---

### TC-NOT-024 — Excluir relatório agendado

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | DeleteScheduledReport |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. Enviar `DeleteScheduledReport.Command(R1.Id)`.

**Resultado Esperado:**
- Soft-delete; não aparece em `ListScheduledReports`.

**Critério de Aceite:** HTTP 204.

---

### TC-NOT-025 — Listar relatórios agendados com paginação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListScheduledReports |
| **Tipo** | Unitário |
| **Prioridade** | Baixa |

**Pré-condições:** 25 relatórios ativos.

**Passos:**
1. Executar `ListScheduledReports.Query(page: 1, pageSize: 10)`.

**Resultado Esperado:**
- 10 itens; `hasNextPage = true`.

**Critério de Aceite:** HTTP 200 com paginação correta.

---

### TC-NOT-026 — Criar template de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | CreateWebhookTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Tenant autenticado.

**Passos:**
1. Enviar `Name = "Deploy Notifier"`, `Url = "https://hooks.empresa.com/deploy"`, `Method = POST`, `Headers = { "Authorization": "Bearer {{token}}" }`, `EventType = "release.deployed"`.

**Resultado Esperado:**
- Template criado com `IsActive = true`.

**Critério de Aceite:** HTTP 201.

---

### TC-NOT-027 — Ativar/desativar template de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ToggleWebhookTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Pré-condições:** Template ativo.

**Passos:**
1. Toggle para `IsActive = false`.

**Resultado Esperado:**
- Webhook não é disparado em futuros eventos.

**Critério de Aceite:** HTTP 200.

---

### TC-NOT-028 — Excluir template de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | DeleteWebhookTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. Enviar `DeleteWebhookTemplate.Command(templateId)`.

**Resultado Esperado:**
- Soft-delete; não aparece em listagem.

**Critério de Aceite:** HTTP 204.

---

### TC-NOT-029 — Reconhecer notificação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | AcknowledgeNotification |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** Notificação que requer ação explícita (`RequiresAck = true`).

**Passos:**
1. Enviar `AcknowledgeNotification.Command(notificationId)`.
2. Handler seta `AcknowledgedAt = now`.

**Resultado Esperado:**
- `result.IsSuccess == true`.

**Critério de Aceite:** HTTP 204; campo `acknowledgedAt` preenchido.

---

### TC-NOT-030 — Listagem de notificações com filtros

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListNotifications |
| **Tipo** | Unitário |
| **Prioridade** | Alta |

**Pré-condições:** 20 notificações (10 lidas, 5 arquivadas, 5 ativas não lidas).

**Passos:**
1. Consultar com `isRead = false`, `isArchived = false`.

**Resultado Esperado:**
- Retorna apenas as 5 ativas não lidas.

**Critério de Aceite:** `result.Value.Count == 5`.

---

### TC-NOT-031 — Isolamento de notificações entre tenants

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListNotifications |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Pré-condições:** Tenant A com 5 notificações, Tenant B com 10 notificações.

**Passos:**
1. Tenant A lista notificações.

**Resultado Esperado:**
- Retorna exatamente 5.
- `TenantRlsInterceptor` garante isolamento via `set_config('app.current_tenant_id', ...)`.

**Critério de Aceite:** `result.Value.Count == 5`.

---

### TC-NOT-032 — Acesso sem autenticação

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListNotifications |
| **Tipo** | Segurança |
| **Prioridade** | Crítica |

**Passos:**
1. Chamar endpoint sem Bearer token.

**Resultado Esperado:**
- HTTP 401.

**Critério de Aceite:** Nenhum dado retornado.

---

### TC-NOT-033 — Template com variável obrigatória faltando

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | UpsertNotificationTemplate |
| **Tipo** | Unitário |
| **Prioridade** | Média |

**Passos:**
1. Criar template com `Body = ""`.

**Resultado Esperado:**
- `ErrorType = Validation` — corpo não pode ser vazio.

**Critério de Aceite:** HTTP 422.

---

### TC-NOT-034 — Disparo via Outbox após evento de domínio

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | Entrega via Outbox |
| **Tipo** | Integração |
| **Prioridade** | Crítica |

**Pré-condições:**
- Template de email configurado para evento `release.approved`.
- `ModuleOutboxProcessorJob` ativo.

**Passos:**
1. Release aprovado publica `ReleaseApprovedIntegrationEvent` no Outbox.
2. Job processa evento e chama handler de notificação.
3. Notificação criada e entregue via canal configurado.

**Resultado Esperado:**
- Notificação persistida com `DeliveredAt` preenchido em < 30s.

**Critério de Aceite:** `GetNotificationTrail` mostra entrega bem-sucedida.

---

### TC-NOT-035 — Listar templates de webhook

| Campo | Valor |
|-------|-------|
| **Módulo** | Notifications |
| **Feature** | ListWebhookTemplates |
| **Tipo** | Unitário |
| **Prioridade** | Baixa |

**Pré-condições:** 3 templates criados para o tenant.

**Passos:**
1. Executar `ListWebhookTemplates.Query`.

**Resultado Esperado:**
- Lista com 3 templates; tenant B não visível.

**Critério de Aceite:** HTTP 200 `{ items: [...] }`.

---
