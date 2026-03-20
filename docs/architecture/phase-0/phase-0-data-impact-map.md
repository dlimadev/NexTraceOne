# Phase 0 — Data Impact Map

**Data:** 2026-03-20  
**Scope:** Inventário de impacto nos dados — aggregates, tabelas, entidades, índices, migrações

Classificação de scope:
- **Global** — não precisa de tenant nem ambiente
- **Tenant-scoped** — pertence a um tenant, não a um ambiente específico
- **Tenant+Environment-scoped** — pertence a um tenant e a um ambiente específico

Legenda de risco:
- 🔴 Crítico — dado hoje sem isolamento, risco imediato de vazamento cross-tenant
- 🟠 Alto — dado sem tenant/ambiente, operações cruzadas possíveis
- 🟡 Médio — dado incompleto mas sem risco imediato
- 🟢 Baixo — correto ou sem impacto

---

## 1. IdentityAccess Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `User` | Tenant-scoped | ✅ via Membership | — | Tenant-scoped | 🟢 | OK |
| `Tenant` | Global | — | — | Global | 🟢 | Entidade raiz |
| `TenantMembership` | Tenant-scoped | ✅ | — | Tenant-scoped | 🟢 | OK |
| `Role` | Global / Tenant | ⚠️ Parcial | — | Rever se Role é global ou tenant-scoped | 🟡 | Validar |
| `Environment` (IdentityAccess) | Tenant-scoped | ✅ | ✅ (é o Id) | Tenant-scoped | 🟢 | Modelo correto |
| `EnvironmentAccess` | Tenant+Environment-scoped | ✅ `TenantId` | ✅ `EnvironmentId` | Tenant+Environment | 🟢 | OK |
| `JitAccessRequest` | Tenant-scoped | ✅ | — | Tenant-scoped | 🟢 | OK |
| `BreakGlassRequest` | Tenant-scoped | ✅ | ⚠️ Provável — JIT é para ambiente específico | Tenant+Environment | 🟡 | Adicionar EnvironmentId ao BreakGlass |
| `Delegation` | Tenant-scoped | ✅ | — | Tenant-scoped | 🟢 | OK |
| `AccessReviewCampaign` | Tenant-scoped | ✅ | — | Tenant-scoped | 🟢 | OK |
| `SecurityEvent` | Tenant-scoped | ✅ | ❌ | Tenant+Environment | 🟡 | Eventos de segurança deveriam ter ambiente |

**Banco:** `identity_db`  
**Índices existentes confirmados:** `TenantId` indexado em `EnvironmentAccess`  
**Índices adicionais necessários:** Nenhum urgente nesta fase

---

## 2. Catalog Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `ServiceAsset` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🔴 Crítico | Core do catálogo sem tenant |
| `ApiAsset` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🔴 Crítico | APIs sem tenant |
| `ConsumerRelationship` | **Global** | ❌ | ⚠️ `string consumerEnvironment` | **Tenant+Environment** | 🔴 Crítico | Relacionamentos cross-tenant possíveis |
| `ConsumerAsset` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | |
| `DiscoverySource` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `NodeHealthRecord` | **Global** | ❌ | ❌ | **Tenant+Environment** | 🟠 Alto | Health é por tenant e ambiente |
| `GraphSnapshot` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `SavedGraphView` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `ContractVersion` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🔴 Crítico | Contratos sem tenant — risco de exposição |
| `ContractDiff` | **Global** | ❌ | — | Via ContractVersion | 🟠 Alto | Herdar tenant de ContractVersion |
| `ContractArtifact` | **Global** | ❌ | — | Via ContractVersion | 🟠 Alto | |
| `ContractDraft` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `ContractEvidencePack` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `SpectralRuleset` | Global ou Tenant | ❌ | ❌ | Avaliar: global shared ou tenant-owned | 🟡 Médio | Rulesets partilhados vs tenant-specific |
| `PlaygroundSession` | **Global** | ❌ | ⚠️ `string environment` | **Tenant+Environment** | 🟡 Médio | |
| `Subscription` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `SavedSearch` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `CodeGenerationRecord` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `PortalAnalyticsEvent` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `LinkedReference` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `CanonicalEntity` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |

**Bancos:** `catalog_graph_db`, `contracts_db`, `developer_portal_db`  
**Índices necessários após refactor:** `(TenantId)` em todas as tabelas; `(TenantId, EnvironmentId)` em NodeHealthRecord, ConsumerRelationship  
**Risco de backfill:** Alto — muitos registros podem existir sem TenantId; precisam de valor default ou migração com `SET TenantId = <inferred>`

---

## 3. ChangeGovernance Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `Release` | **Global** | ❌ | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | Release sem tenant — vazamento cross-tenant |
| `ReleaseBaseline` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | |
| `DeploymentEnvironment` | **Global** | ❌ | ✅ (é o Id) | **Tenant-scoped** | 🔴 Crítico | Ambientes de pipeline sem tenant |
| `PromotionRequest` | **Global** | ❌ | ✅ source/target IDs | **Tenant-scoped** | 🔴 Crítico | Promoções sem tenant |
| `PromotionGate` | **Global** | ❌ | ✅ via DeploymentEnvironmentId | **Tenant-scoped** | 🟠 Alto | Gates sem tenant |
| Workflow entities | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | Workflows de release sem tenant |
| RulesetGovernance entities | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | Rulesets podem ser global ou tenant |

**Bancos:** `change_intelligence_db`, `promotion_db`, `workflow_db`, `ruleset_governance_db`  
**Índices necessários:** `(TenantId)` em `Release`, `DeploymentEnvironment`, `PromotionRequest`; `(TenantId, Environment)` em `Release`  
**Risco de backfill:** Alto — dados existentes de staging/demo precisam de TenantId

---

## 4. OperationalIntelligence Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `IncidentRecord` | **Global** | ❌ | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | Incidentes de diferentes clientes misturados |
| `MitigationWorkflowRecord` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | Via IncidentRecord |
| `MitigationWorkflowActionLog` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | |
| `MitigationValidationLog` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | |
| `RunbookRecord` | **Global** | ❌ | ❌ | **Tenant+Environment** | 🟠 Alto | Runbooks são tenant e possivelmente ambiente-específicos |
| `ServiceCostProfile` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | Custos por tenant |
| `CostAttribution` | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟠 Alto | |
| `CostSnapshot` | **Global** | ❌ | ❌ | **Tenant+Environment** | 🟡 Médio | Custo por ambiente faz sentido |
| `CostTrend` | **Global** | ❌ | ❌ | **Tenant+Environment** | 🟡 Médio | |
| Automation entities (runtime) | **Global** | ❌ | ❌ | **Tenant+Environment** | 🟠 Alto | Automações são específicas por ambiente |

**Bancos:** `oi_db` (EF persist) + InMemory (parcial)  
**Índices necessários:** `(TenantId)` em todas; `(TenantId, EnvironmentId)` em IncidentRecord, CostSnapshot  
**Risco de backfill:** Médio — dados de seed/demo existem sem TenantId

---

## 5. AIKnowledge Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `AiTokenUsageLedger` | Tenant-scoped | ⚠️ `string TenantId` | ❌ | **Tenant-scoped** | 🟡 Médio | Usar strongly typed `TenantId` |
| `AiExternalInferenceRecord` | Tenant-scoped | ⚠️ `string TenantId` | ❌ | **Tenant-scoped** | 🟡 Médio | Usar strongly typed `TenantId` |
| `AiAssistantConversation` | Tenant-scoped | ⚠️ Verificar | ❌ | **Tenant+Environment** | 🟡 Médio | Conversas deveriam ter contexto de ambiente |
| `AiQuotaPolicy` | Tenant-scoped | ⚠️ Verificar | ❌ | **Tenant-scoped** | 🟢 Baixo | Quota é por tenant |
| `AiModelRegistry` | Global ou Tenant | ❌ | ❌ | Global (modelos globais) ou Tenant (modelos privados) | 🟡 Médio | Rever se modelos são globais ou por tenant |
| ExternalAI entities | — | ❌ (stubs) | ❌ | **Tenant-scoped** | 🟢 Baixo | Implementar já com TenantId |
| Orchestration entities | — | ❌ (stubs) | ❌ | **Tenant+Environment** | 🟢 Baixo | Implementar já com TenantId + EnvironmentId |

**Bancos:** `ai_governance_db`, `external_ai_db`, `ai_orchestration_db`  
**Índices existentes:** `TenantId` indexado em `AiTokenUsageLedger`, `AiExternalInferenceRecord`  
**Risco de backfill:** Baixo — dados de AI são recentes; mudança de tipo `string → Guid` requer migration

---

## 6. Governance Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `IntegrationConnector` | **Global** | ❌ | 🔴 hardcoded | **Tenant+Environment** | 🟠 Alto | Infrastructure vazia — implementar já com TenantId |
| `GovernancePack` | Global ou Tenant | ❌ | 🔴 hardcoded | Rever escopo | 🟡 Médio | Infrastructure vazia |
| `AnalyticsEvent` | Tenant-scoped | ✅ `Guid TenantId` | ❌ | **Tenant+Environment** | 🟡 Médio | Evento de analytics sem ambiente |
| Outros entities de Governance | **Global** | ❌ | ❌ | **Tenant-scoped** | 🟡 Médio | 9 entidades, Infrastructure vazia |

**Banco:** `governance_db`  
**Status:** Infrastructure completamente vazia — não há tabelas reais para GovernancePacks, Connectors, etc.  
**Risco de backfill:** Baixo (sem dados reais) — mas implementação deve ser feita já com TenantId

---

## 7. AuditCompliance Module

| Entidade / Tabela | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|-------------------|-------------|-----------|----------------|---------------|----------------|-------|
| `AuditEvent` | Tenant-scoped | ✅ `Guid TenantId` | ❌ | **Tenant+Environment** | 🟡 Médio | Audit sem ambiente — queries por ambiente impossíveis |

**Banco:** `audit_db`  
**Índices existentes:** `TenantId` indexado  
**Índices adicionais necessários:** `(TenantId, EnvironmentId)` após adição do campo

---

## 8. Telemetria (BuildingBlocks.Observability)

| Modelo | Scope Atual | TenantId? | EnvironmentId? | Scope Correto | Risco Migração | Notas |
|--------|-------------|-----------|----------------|---------------|----------------|-------|
| `ObservedTopologyEntry` | Tenant+Environment | ⚠️ `Guid? TenantId` nullable | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | TenantId deve ser obrigatório; string → EnvironmentId |
| `AnomalySnapshot` | Tenant+Environment | ⚠️ `Guid? TenantId` nullable | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | Idem |
| `ServiceMetricsSnapshot` | Tenant+Environment | ⚠️ `Guid? TenantId` nullable | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | Idem |
| `ReleaseRuntimeCorrelation` | Tenant+Environment | ⚠️ `Guid? TenantId` nullable | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | Idem |
| `InvestigationContext` | Tenant+Environment | ⚠️ `Guid? TenantId` nullable | ⚠️ `string Environment` | **Tenant+Environment** | 🔴 Crítico | Idem |
| `TelemetryReference` | Tenant+Environment | ⚠️ `Guid? TenantId` nullable | ⚠️ `string? Environment` nullable | **Tenant+Environment** | 🟠 Alto | Ambos nullable — risco de dados sem contexto |

---

## 9. Resumo Consolidado por Ação

### Ação: Adicionar TenantId a entidades críticas (Fase 1)
- `Release`, `DeploymentEnvironment`, `PromotionRequest`, `PromotionGate`
- `IncidentRecord`, `RunbookRecord`, `ServiceCostProfile`
- `ApiAsset`, `ServiceAsset`, `ContractVersion`, `ConsumerRelationship`
- `GraphSnapshot`, `NodeHealthRecord`, `PlaygroundSession`, `Subscription`

### Ação: Trocar `string Environment` por `EnvironmentId` (Fase 1)
- `Release.Environment` → `EnvironmentId: EnvironmentId`
- `IncidentRecord.Environment` → `EnvironmentId: EnvironmentId`
- `ConsumerRelationship.consumerEnvironment` → `ConsumerEnvironmentId: EnvironmentId?`

### Ação: Tornar TenantId obrigatório onde é nullable (Fase 5)
- Todos os modelos de telemetria `Guid? TenantId` → `Guid TenantId`

### Ação: Criar índices compostos (Fase 3)
- `(TenantId, EnvironmentId)` em: `Release`, `IncidentRecord`, `ConsumerRelationship`, todos os modelos de telemetria

### Ação: Scripts de backfill necessários
- `Release`: TenantId = TenantId do ApiAsset correspondente
- `IncidentRecord`: TenantId = TenantId do contexto de criação (pode ser indetectável retroativamente)
- `DeploymentEnvironment`: Descoberta manual por admin — ambientes precisam ser reatribuídos
- Catalog entities: TenantId inferível via owner service ou necessita input manual

### Risco de migração de dados
- **Alto**: Entidades sem TenantId com dados de produção — necessitam estratégia de backfill
- **Médio**: Mudanças de tipo `string → Guid` para EnvironmentId — migration com lookup table
- **Baixo**: Entidades ainda sem dados reais (Governance infrastructure vazia)
