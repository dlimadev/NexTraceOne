# Notifications — Audit & Compliance

## Eventos Auditados

### Tipos de Acção
| Acção | Código | Descrição |
|-------|--------|-----------|
| Geração crítica | `notification.critical.generated` | Notificação Critical criada |
| Entrega crítica | `notification.critical.delivered` | Notificação Critical entregue com sucesso |
| Falha crítica | `notification.critical.failed` | Entrega de notificação Critical falhou |
| Acknowledge | `notification.acknowledged` | Utilizador reconheceu notificação |
| Snooze | `notification.snoozed` | Utilizador adiou notificação |
| Escalation | `notification.escalated` | Notificação escalada automaticamente |
| Incidente criado | `notification.incident.created` | Incidente criado a partir de notificação |
| Preferências | `notification.preferences.changed` | Utilizador alterou preferências |
| Supressão | `notification.suppressed` | Notificação suprimida por regra |

### Campos da Entrada de Auditoria
| Campo | Tipo | Descrição |
|-------|------|-----------|
| TenantId | Guid | Id do tenant |
| ActionType | string | Código da acção (ver tabela acima) |
| ResourceId | string | Id do recurso (notificação, preferência) |
| ResourceType | string | Tipo do recurso |
| PerformedBy | Guid? | Utilizador (null para acções automáticas) |
| Description | string? | Descrição textual da acção |
| PayloadJson | string? | Contexto adicional em JSON |
| OccurredAt | DateTimeOffset | Data/hora da ocorrência |

## Integração com Audit Trail

A integração com o módulo de auditoria é feita via logging estruturado.
Cada entrada é registada como log de nível Information com campos nomeados
que permitem correlação com o audit trail existente do NexTraceOne.

### Padrão de Integração
```
NotificationAudit: Action={ActionType} Resource={ResourceType}/{ResourceId}
                   Tenant={TenantId} User={PerformedBy} Description={Description}
```

## Rationale

### Porque Auditar
1. **Conformidade** — Notificações críticas precisam de trilha para compliance
2. **Responsabilização** — Saber quem viu, reconheceu, adiou, ignorou
3. **Diagnóstico** — Identificar falhas de entrega de notificações críticas
4. **Governança** — Rastrear mudanças de preferências e regras

### O Que Não Auditar
- Notificações informacionais de baixa severidade
- Leituras simples de notificações Info
- Navegação na central de notificações
- Consultas de métricas

### Princípio
> Auditar o que é crítico para governança, operação e conformidade.
> Não auditar tudo indiscriminadamente.
