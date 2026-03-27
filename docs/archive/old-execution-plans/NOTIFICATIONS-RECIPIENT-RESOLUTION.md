# Resolução de Destinatários de Notificação

## Visão Geral

O `NotificationRecipientResolver` é o componente central responsável por determinar quem recebe cada notificação. Centraliza a lógica de targeting para evitar dispersão nos event handlers.

## Tipos de Destinatário Suportados

### Implementados (Fase 4)

| Tipo | Mecanismo | Estado |
|------|-----------|--------|
| Utilizador explícito | `RecipientUserIds` no NotificationRequest | ✅ Implementado |
| Deduplicação | HashSet remove duplicados automaticamente | ✅ Implementado |
| Filtragem | Guid.Empty excluído automaticamente | ✅ Implementado |

### Scaffolded (Fase 5+)

| Tipo | Mecanismo | Estado |
|------|-----------|--------|
| Role funcional | `RecipientRoles` → resolução via Identity | 🔲 Scaffolded |
| Equipa/grupo | `RecipientTeamIds` → resolução via Governance | 🔲 Scaffolded |

## Fluxo de Resolução

```
NotificationRequest
    │
    ├── RecipientUserIds? ──→ Adiciona IDs válidos (≠ Guid.Empty)
    │
    ├── RecipientRoles? ──→ [Futuro] Resolve via Identity module
    │
    └── RecipientTeamIds? ──→ [Futuro] Resolve via Governance module
    │
    ▼
HashSet<Guid> (destinatários únicos)
    │
    ├── Count > 0 → Prosseguir com notificação
    └── Count == 0 → Log warning, notificação não criada
```

## Casos de Uso por Evento

| Evento | Destinatário Primário | Fallback |
|--------|----------------------|----------|
| IncidentCreated | OwnerUserId do serviço | — (skip se null) |
| IncidentEscalated | OwnerUserId do serviço | — (skip se null) |
| ApprovalPending | Aprovador designado | — (skip se null) |
| WorkflowRejected | Submitter do workflow | — (skip se null) |
| BreakGlassActivated | Admin que ativou | — (skip se null) |
| ComplianceCheckFailed | OwnerUserId do serviço | — (skip se null) |
| BudgetExceeded | OwnerUserId do serviço | — (skip se null) |
| IntegrationFailed | OwnerUserId da integração | — (skip se null) |

## Fallback

### Comportamento atual:
- Se nenhum destinatário pode ser resolvido: notificação não é criada
- Warning é logado com contexto completo (EventType, roles, teams)
- Sem perda silenciosa — sempre há log

### Evolução futura:
- Fallback para admin do tenant quando owner não existe
- Fallback para equipa operacional quando owner individual está ausente
- Notificação para admin com contexto "no recipient found"

## Rastreabilidade

- Cada decisão de resolução é logada com DEBUG/WARNING level
- Roles e teams não resolvidos geram WARNING explícito
- O orquestrador loga o resultado final por destinatário
