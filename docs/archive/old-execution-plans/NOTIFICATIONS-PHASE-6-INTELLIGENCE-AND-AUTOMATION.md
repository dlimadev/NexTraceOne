# Notifications Phase 6 — Intelligence & Automation

## Objetivo

Evoluir a plataforma de notificações do NexTraceOne de uma inbox/dispatcher para uma capability enterprise madura com inteligência operacional, redução de ruído, reconhecimento semântico, adiamento controlado, resumos consolidados, respeito a horários e escalação segura.

## Princípio Central

> "Uma plataforma de notificações enterprise não pode apenas gerar e entregar notificações; ela precisa ajudar o utilizador a priorizar, reconhecer, adiar, agrupar, resumir e escalar corretamente os eventos relevantes."

## Estado Inicial (Pré-Fase 6)

- 330 testes unitários
- Central interna funcional com unread count
- 29 tipos de evento por 7 domínios (Fase 5)
- Email e Teams como canais externos
- Deduplicação básica (5 min, tenant+recipient+type+entity)
- Preferências por utilizador
- Mandatory notification policy
- Routing engine

## Capacidades Entregues

### 1. Deduplicação Avançada (Block B)
- Chave de correlação rica (`tenant|module|eventType|entityType|entityId`)
- Contagem de ocorrências (`OccurrenceCount`) com incremento
- Timestamp da última ocorrência (`LastOccurrenceAt`)
- Merge de notificações existentes em vez de criar novas

### 2. Agrupamento e Correlação (Block C)
- `CorrelationKey` para agrupar notificações relacionadas
- `GroupId` para identificar grupos de notificações correlatas
- Resolução de grupo por janela temporal (60 min padrão)
- Notificações do mesmo serviço/entidade/tipo agrupadas automaticamente

### 3. Acknowledge Enriquecido (Block D)
- `AcknowledgedBy` — quem reconheceu
- `AcknowledgeComment` — nota opcional do reconhecimento
- Acknowledge continua distinto de leitura
- Retrocompatível com chamadas sem parâmetros

### 4. Snooze (Block E)
- `SnoozedUntil` — data/hora até à qual está adiada
- `SnoozedBy` — quem adiou
- `IsSnoozed()` — verificação semântica
- `Unsnooze()` — reactivação
- Guards: archived e dismissed não podem ser snoozed

### 5. Digest (Block F)
- Serviço `NotificationDigestService` com janela de 24h
- Agrupamento por categoria com contagem
- Apenas notificações Info/ActionRequired elegíveis
- Critical e Warning excluídos do digest
- Resumo textual estruturado

### 6. Quiet Hours (Block G)
- Período padrão: 22:00–08:00 UTC
- Notificações obrigatórias (Critical, BreakGlass, Compliance) ignoram quiet hours
- Entrega diferida para eventos elegíveis
- Lógica centralizada e auditável

### 7. Escalação (Block H)
- Critical não acknowledged em 30 min → escalável
- ActionRequired com RequiresAction não acknowledged em 2h → escalável
- `IsEscalated` e `EscalatedAt` para rastreabilidade
- Guards: acknowledged, archived, dismissed, snoozed não são escalados
- Escalação é idempotente

### 8. Correlação com Incidentes (Block I)
- `CorrelatedIncidentId` para vincular notificação a incidente
- `CorrelateWithIncident()` para vinculação explícita
- Base para automação controlada de criação/enriquecimento de incidentes

### 9. Supressão Básica (Block J)
- Regra 1: Já acknowledged para mesma entidade nos últimos 30 min
- Regra 2: Snooze activo para o mesmo tipo/entidade
- Notificações obrigatórias nunca são suprimidas
- Notificações Critical nunca são suprimidas
- `IsSuppressed` e `SuppressionReason` para rastreabilidade

## Impacto na Maturidade da Plataforma

| Dimensão | Antes (Fase 5) | Depois (Fase 6) |
|----------|----------------|------------------|
| Modelo de dados | 15 campos | 28 campos (+13) |
| Métodos de domínio | 7 | 15 (+8) |
| Serviços de inteligência | 1 (dedup) | 6 (+5) |
| Testes unitários | 330 | 373 (+43) |
| Estados operacionais | Read/Unread/Acknowledged | + Snoozed/Escalated/Suppressed |
| Redução de ruído | Dedup básica | Dedup + Grouping + Suppression + Digest |
| Correlação | Nenhuma | CorrelationKey + GroupId + IncidentId |

## Novos Campos no Modelo Notification

| Campo | Tipo | Descrição |
|-------|------|-----------|
| AcknowledgedBy | Guid? | Id do utilizador que fez acknowledge |
| AcknowledgeComment | string? | Comentário do acknowledge |
| CorrelationKey | string? | Chave de correlação |
| GroupId | Guid? | Id do grupo |
| OccurrenceCount | int | Contagem de ocorrências (default: 1) |
| LastOccurrenceAt | DateTimeOffset? | Última ocorrência |
| SnoozedUntil | DateTimeOffset? | Snoozed até |
| SnoozedBy | Guid? | Quem fez snooze |
| IsEscalated | bool | Se foi escalado |
| EscalatedAt | DateTimeOffset? | Data da escalação |
| CorrelatedIncidentId | Guid? | Id do incidente correlacionado |
| IsSuppressed | bool | Se foi suprimido |
| SuppressionReason | string? | Razão da supressão |

## Novos Serviços

| Serviço | Responsabilidade |
|---------|------------------|
| NotificationGroupingService | Correlação e agrupamento |
| QuietHoursService | Entrega diferida |
| NotificationEscalationService | Escalação de críticos |
| NotificationSuppressionService | Supressão controlada |
| NotificationDigestService | Resumos consolidados |

## Preparação para Fase 7

A Fase 7 pode focar-se em:
- Métricas e analytics de notificações
- Auditoria da plataforma de notificações
- Governança da central (SLA de acknowledge, tempo de resposta)
- Dashboard operacional de notificações
- Configuração avançada de quiet hours por utilizador
- Escalação com ampliação de destinatários
