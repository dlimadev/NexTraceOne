# Notifications Phase 5 — Domain High-Value Events

## Objetivo

Ligar a plataforma de notificações do NexTraceOne aos eventos de negócio e operação mais valiosos de cada domínio, transformando a capability de notificação de técnica genérica em profundamente integrada ao valor do produto.

## Princípio Central

> "O valor da plataforma de notificações do NexTraceOne não está em notificar tudo, mas em notificar os eventos certos, para as pessoas certas, com contexto suficiente para ação real."

## Estado Inicial (Pré-Fase 5)

- 6 event handlers ativos
- 8 registos de integração no DI
- 11 tipos no catálogo `NotificationType`
- 249 testes unitários
- Cobertura: Incidentes (2), Aprovações (2), Segurança (1), Compliance (1), Budget (1), Integração (1)

## Estado Final (Pós-Fase 5)

- 8 event handlers ativos (6 expandidos + 2 novos)
- 29 registos de integração no DI
- 29 tipos no catálogo `NotificationType`
- 330 testes unitários (+81 novos)

## Famílias de Domínio Cobertas

### 1. Operações e Incidentes
- IncidentCreated (existente)
- IncidentEscalated (existente)
- **IncidentResolved** (novo)
- **AnomalyDetected** (novo)
- **HealthDegradation** (novo)

### 2. Aprovações e Workflow
- ApprovalPending (existente)
- ApprovalRejected (existente)
- **ApprovalApproved** (novo)
- **ApprovalExpiring** (novo)

### 3. Catálogo e Contratos
- **ContractPublished** (novo)
- **BreakingChangeDetected** (novo)
- **ContractValidationFailed** (novo)

### 4. Segurança e Acesso
- BreakGlassActivated (existente)
- **UserRoleChanged** (novo)
- **JitAccessGranted** (novo)
- **AccessReviewPending** (novo)

### 5. FinOps, Governance e Compliance
- ComplianceCheckFailed (existente)
- BudgetExceeded (existente)
- **PolicyViolated** (novo)
- **EvidenceExpiring** (novo)
- **BudgetThresholdReached** (novo) — com severidade dinâmica (80%→ActionRequired, 90%→Warning, 100%→Critical)

### 6. IA e Governança de IA
- **AiProviderUnavailable** (novo) — notifica roles AiAdmin/PlatformAdmin
- **TokenBudgetExceeded** (novo)
- **AiGenerationFailed** (novo)
- **AiActionBlockedByPolicy** (novo)

### 7. Integrações e Ingestion
- IntegrationFailed (existente)
- **SyncFailed** (novo)
- **ConnectorAuthFailed** (novo)

## Impacto no Valor da Plataforma

| Métrica | Antes | Depois |
|---------|-------|--------|
| Domínios cobertos | 5 parciais | 7 completos |
| Tipos de notificação | 11 | 29 |
| Eventos com handler ativo | 8 | 29 |
| Testes unitários | 249 | 330 |
| Deep links por domínio | Parcial | Completo |
| Severidade contextual | Estática | Dinâmica (ex: budget threshold) |

## Artefactos Alterados

### Novos Integration Events
- `OperationalIntegrationEvents.cs` — +5 eventos (IncidentResolved, AnomalyDetected, HealthDegradation, ConnectorAuthFailed, SyncFailed)
- `WorkflowIntegrationEvents.cs` — +2 eventos (ApprovalApproved, ApprovalExpiring)
- `SecurityIntegrationEvents.cs` — novo ficheiro (JitAccessGranted, AccessReviewPending)
- `IntegrationEvents.cs` (Governance) — +3 eventos (PolicyViolated, EvidenceExpiring, BudgetThresholdReached)
- `CatalogIntegrationEvents.cs` — novo ficheiro (ContractPublished, BreakingChangeDetected, ContractValidationFailed)
- `AiGovernanceIntegrationEvents.cs` — novo ficheiro (AiProviderUnavailable, TokenBudgetExceeded, AiGenerationFailed, AiActionBlockedByPolicy)

### Handlers Expandidos
- `IncidentNotificationHandler` — +3 event interfaces
- `ApprovalNotificationHandler` — +2 event interfaces
- `SecurityNotificationHandler` — +3 event interfaces
- `ComplianceNotificationHandler` — +3 event interfaces
- `IntegrationFailureNotificationHandler` — +2 event interfaces

### Novos Handlers
- `CatalogNotificationHandler` — 3 eventos de contratos
- `AiGovernanceNotificationHandler` — 4 eventos de IA

### Testes
- 7 novos ficheiros de teste por domínio
- 81 novos testes unitários
- Cobertura: submissão, skip por dados em falta, deep links, severidade, categoria, destinatários

## Preparação para Fase 6

A Fase 6 pode focar-se em:
- Inteligência da central (digest, suppression avançada)
- Quiet hours
- Escalation automática
- ML para priorização
- Analytics de notificações

A base de 29 tipos de evento por 7 domínios fornece dados suficientes para começar a aplicar inteligência sobre a central de notificações.
