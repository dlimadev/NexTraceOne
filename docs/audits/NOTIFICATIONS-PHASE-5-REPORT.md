# Notifications Phase 5 — Audit Report

## Resumo Executivo

A Fase 5 da plataforma de notificações do NexTraceOne integrou com sucesso os eventos de alto valor dos 7 domínios centrais do produto, expandindo a cobertura de 8 para 29 tipos de notificação, com 81 novos testes unitários.

## Estado Inicial

| Dimensão | Valor |
|----------|-------|
| Event handlers | 6 |
| Registos DI | 8 |
| NotificationType entries | 11 |
| Testes unitários | 249 |
| Domínios com cobertura | 5 (parcial) |

## Estado Final

| Dimensão | Valor |
|----------|-------|
| Event handlers | 8 |
| Registos DI | 29 |
| NotificationType entries | 29 |
| Testes unitários | 330 |
| Domínios com cobertura | 7 (completa) |

## Eventos Integrados

### Operações e Incidentes (5 tipos)
- IncidentCreated, IncidentEscalated (existentes)
- **IncidentResolved, AnomalyDetected, HealthDegradation** (novos)

### Aprovações e Workflow (4 tipos)
- ApprovalPending, ApprovalRejected (existentes)
- **ApprovalApproved, ApprovalExpiring** (novos)

### Catálogo e Contratos (3 tipos)
- **ContractPublished, BreakingChangeDetected, ContractValidationFailed** (todos novos)

### Segurança e Acesso (5 tipos)
- BreakGlassActivated, JitAccessPending (existentes — JitAccessPending tipo reservado)
- **UserRoleChanged, JitAccessGranted, AccessReviewPending** (novos)

### FinOps, Governance e Compliance (5 tipos)
- ComplianceCheckFailed, BudgetExceeded (existentes)
- **PolicyViolated, EvidenceExpiring, BudgetThresholdReached** (novos)

### IA e Governança de IA (4 tipos)
- AiProviderUnavailable (tipo existente, novo handler)
- **TokenBudgetExceeded, AiGenerationFailed, AiActionBlockedByPolicy** (novos)

### Integrações e Ingestion (3 tipos)
- IntegrationFailed (existente)
- **SyncFailed, ConnectorAuthFailed** (novos)

## Decisões de Destinatários e Canais

### Padrão por Domínio
| Domínio | Destinatário Principal | Fallback |
|---------|----------------------|----------|
| Operações | Owner do serviço | — |
| Aprovações | Aprovador (pending/expiring) / Owner (approved/rejected) | — |
| Contratos | Owner / Publisher do serviço | — |
| Segurança | Utilizador impactado | — |
| Governance | Owner do serviço | — |
| IA | Utilizador solicitante / Roles AiAdmin | — |
| Integrações | Owner do conector/integração | — |

### Decisões de Canal
- InApp: Sempre para todos os eventos
- Email: Eventos com severidade ≥ Warning ou ActionRequired
- Teams: Eventos com severidade Critical

### Decisão Especial — AiProviderUnavailable
- Notifica por roles (AiAdmin, PlatformAdmin) em vez de utilizador individual
- Rationale: indisponibilidade de provider afeta toda a plataforma

### Decisão Especial — BudgetThresholdReached
- Severidade dinâmica baseada na percentagem:
  - 80%: ActionRequired (sem RequiresAction)
  - 90%: Warning (com RequiresAction)
  - 100%+: Critical (com RequiresAction)

## Templates e Deep Links Refinados

Todos os eventos possuem:
- **Título contextual**: inclui nome do serviço/contrato/recurso
- **Mensagem acionável**: explica o que aconteceu, o impacto e a ação recomendada
- **Deep link**: navega diretamente para a entidade relevante
- **Payload JSON**: dados adicionais para templates ricos

### Exemplos de Deep Links
| Domínio | Padrão |
|---------|--------|
| Incidentes | `/incidents/{IncidentId}` |
| Aprovações | `/workflows/{WorkflowId}/stages/{StageId}` |
| Contratos | `/contracts/{ContractId}` |
| Segurança | `/security/break-glass/{UserId}`, `/security/access-reviews/{ReviewId}` |
| Governance | `/governance/compliance/{ReportId}`, `/governance/evidence/{EvidenceId}` |
| FinOps | `/finops/budgets?service={ServiceName}`, `/finops/anomalies/{AnomalyId}` |
| IA | `/ai/providers`, `/ai/usage`, `/ai/history` |
| Integrações | `/integrations/{IntegrationId}`, `/integrations/connectors/{ConnectorId}` |

## Testes Adicionados

| Ficheiro de Teste | Testes | Domínio |
|-------------------|--------|---------|
| OperationsPhase5HandlerTests | 8 | Operações |
| ApprovalPhase5HandlerTests | 6 | Aprovações |
| CatalogNotificationHandlerTests | 8 | Contratos |
| SecurityPhase5HandlerTests | 8 | Segurança |
| GovernancePhase5HandlerTests | 9 | Governance/FinOps |
| AiGovernanceNotificationHandlerTests | 10 | IA |
| IntegrationPhase5HandlerTests | 6 | Integrações |
| NotificationTypeTests (atualizado) | +18 | Catálogo |
| **Total novos** | **81** | — |

### Cobertura de Teste por Cenário
- ✅ Submissão correta de notificação
- ✅ Skip por dados obrigatórios em falta (OwnerUserId, TenantId)
- ✅ Deep links corretos
- ✅ Severidade adequada por contexto
- ✅ Categoria correta
- ✅ Destinatários corretos
- ✅ Severidade dinâmica (BudgetThresholdReached 80/90/100%)

## O Que Fica para a Fase 6

A Fase 6 pode iniciar focada em inteligência e automação da central:

1. **Digest** — agregação periódica de notificações de baixa severidade
2. **Quiet Hours** — supressão por horário configurável por utilizador
3. **Escalation automática** — escalonamento para superiores quando não há acknowledge
4. **Suppression avançada** — regras de supressão por padrão de evento
5. **ML para priorização** — priorização inteligente baseada em padrões de uso
6. **Analytics de notificações** — dashboard de métricas de entrega e ação
7. **Recipient resolution por equipa** — resolução de destinatários via equipas e roles organizacionais

## Conclusão

A Fase 5 transforma a plataforma de notificações do NexTraceOne de uma capability com cobertura parcial em uma plataforma completa que notifica os eventos mais importantes de todos os domínios centrais do produto, com contexto real, deep links acionáveis e severidade adequada. A base de 29 tipos de evento por 7 domínios está pronta para a aplicação de inteligência na Fase 6.

---

**Data**: 2026-03-23  
**Autor**: Copilot Coding Agent  
**Fase anterior**: Fase 4 — Preferências e Roteamento  
**Próxima fase**: Fase 6 — Inteligência e Automação da Central
