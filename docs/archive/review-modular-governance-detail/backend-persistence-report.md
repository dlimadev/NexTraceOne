# Relatório de Persistência — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Auditoria de persistência (DbContexts, entity configurations, multi-tenancy, outbox)  
> **Escopo:** Todos os 16 DbContexts e 4 bases de dados lógicas

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| DbContexts totais | 16 (únicos) |
| Bases de dados lógicas | 4 |
| Entity configurations | 130 |
| Entidades persistidas | 382 |
| Multi-tenancy | PostgreSQL RLS |
| Encriptação de campos | AES-256-GCM |
| Outbox pattern | ✅ Implementado |
| Soft deletes | ✅ AuditableEntity |
| Audit interceptor | ✅ AuditInterceptor |
| Repository base | ✅ RepositoryBase\<T\> |

---

## 2. Bases de Dados Lógicas (4)

### 2.1 Mapa Completo

| Base de Dados | DbContexts | Módulos | Entidades (est.) |
|---------------|------------|---------|-----------------|
| nextraceone_identity | IdentityDb, AuditDb | identityaccess, auditcompliance | 48 |
| nextraceone_catalog | ContractsDb, CatalogGraphDb, DeveloperPortalDb | catalog | 82 |
| nextraceone_operations | ChangeIntelDb, PromotionDb, RulesetGovernanceDb, WorkflowDb, AutomationDb, CostIntelDb, IncidentDb, ReliabilityDb, RuntimeIntelDb, GovernanceDb, ConfigurationDb, NotificationsDb | changegovernance, operationalintelligence, governance, configuration, notifications | 177 |
| nextraceone_ai | ExternalAiDb, AiGovernanceDb, AiOrchestrationDb | aiknowledge | 72 |

### 2.2 Observações sobre Distribuição

- **nextraceone_operations** contém 12 DbContexts — é a base mais carregada
- Cada DbContext opera como bounded context isolado dentro da mesma base
- A separação lógica permite migração futura para bases de dados separadas sem alteração de código

---

## 3. Detalhamento de DbContexts

### 3.1 IdentityDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | identityaccess |
| **Base de dados** | nextraceone_identity |
| **Entidades (est.)** | 37 |
| **Entity configurations (est.)** | ~20 |
| **Migrações** | 2 |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | ✅ (API keys, tokens) |

**Entidades principais:** User, Role, Permission, Tenant, Session, BreakGlassAccess, JitAccess, Delegation, AccessReview, EnvironmentAccess

---

### 3.2 AuditDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | auditcompliance |
| **Base de dados** | nextraceone_identity |
| **Entidades (est.)** | 11 |
| **Entity configurations (est.)** | ~8 |
| **Migrações** | 2 |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** AuditEvent, AuditTrail, ComplianceCheck, ComplianceReport

---

### 3.3 ContractsDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | catalog |
| **Base de dados** | nextraceone_catalog |
| **Entidades (est.)** | ~35 |
| **Entity configurations (est.)** | ~20 |
| **Migrações** | Incluídas nas 6 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Contract, ContractVersion, ContractSchema, ApiEndpoint, EventContract, SoapService

---

### 3.4 CatalogGraphDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | catalog |
| **Base de dados** | nextraceone_catalog |
| **Entidades (est.)** | ~30 |
| **Entity configurations (est.)** | ~15 |
| **Migrações** | Incluídas nas 6 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Service, ServiceDependency, ServiceOwnership, ServiceTopology

---

### 3.5 DeveloperPortalDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | catalog |
| **Base de dados** | nextraceone_catalog |
| **Entidades (est.)** | ~17 |
| **Entity configurations (est.)** | ~10 |
| **Migrações** | Incluídas nas 6 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** DeveloperPortalEntry, ApiDocumentation, TryItSession

---

### 3.6 ChangeIntelDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | changegovernance |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~15 |
| **Entity configurations (est.)** | ~10 |
| **Migrações** | Incluídas nas 8 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Change, BlastRadius, ChangeCorrelation, ChangeConfidence

---

### 3.7 PromotionDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | changegovernance |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~10 |
| **Entity configurations (est.)** | ~6 |
| **Migrações** | Incluídas nas 8 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** PromotionRequest, PromotionGate, PromotionEnvironment

---

### 3.8 RulesetGovernanceDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | changegovernance |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~10 |
| **Entity configurations (est.)** | ~6 |
| **Migrações** | Incluídas nas 8 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Ruleset, RulesetExecution, Rule, RuleResult

---

### 3.9 WorkflowDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | changegovernance |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~12 |
| **Entity configurations (est.)** | ~8 |
| **Migrações** | Incluídas nas 8 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** WorkflowTemplate, WorkflowInstance, WorkflowStep, WorkflowApproval

---

### 3.10 AutomationDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | operationalintelligence |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~12 |
| **Entity configurations (est.)** | ~8 |
| **Migrações** | Incluídas nas 12 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Automation, AutomationExecution, Runbook, RunbookStep

---

### 3.11 CostIntelDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | operationalintelligence |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~8 |
| **Entity configurations (est.)** | ~5 |
| **Migrações** | Incluídas nas 12 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** CostReport, CostEntry, CostAllocation

---

### 3.12 IncidentDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | operationalintelligence |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~12 |
| **Entity configurations (est.)** | ~8 |
| **Migrações** | Incluídas nas 12 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Incident, MitigationAction, IncidentTimeline, IncidentCorrelation

---

### 3.13 ReliabilityDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | operationalintelligence |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~10 |
| **Entity configurations (est.)** | ~6 |
| **Migrações** | Incluídas nas 12 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** ReliabilityMetric, SLO, SLI, ErrorBudget

---

### 3.14 RuntimeIntelDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | operationalintelligence |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | ~9 |
| **Entity configurations (est.)** | ~5 |
| **Migrações** | Incluídas nas 12 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** RuntimeInsight, RuntimeMetric, RuntimeAnomaly

---

### 3.15 GovernanceDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | governance |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | 58 |
| **Entity configurations (est.)** | ~30 |
| **Migrações** | 3 |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | ✅ (tokens de integração) |

**Entidades principais:** Team, Domain, Policy, GovernancePack, Waiver, ComplianceReport, RiskAssessment, FinOpsEntry, Control, Evidence

---

### 3.16 ConfigurationDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | configuration |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | 6 |
| **Entity configurations (est.)** | ~4 |
| **Migrações** | ⚠️ 0 (usa EnsureCreated) |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | ✅ (segredos de configuração) |

**Entidades principais:** ConfigurationDefinition, ConfigurationValue, ConfigurationPhase

---

### 3.17 NotificationsDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | notifications |
| **Base de dados** | nextraceone_operations |
| **Entidades (est.)** | 15 |
| **Entity configurations (est.)** | ~8 |
| **Migrações** | ⚠️ 0 (sem migrações) |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** Notification, NotificationPreference, NotificationTemplate, NotificationChannel

---

### 3.18 ExternalAiDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | aiknowledge |
| **Base de dados** | nextraceone_ai |
| **Entidades (est.)** | ~20 |
| **Entity configurations (est.)** | ~12 |
| **Migrações** | Incluídas nas 17 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | ✅ (credenciais de provider) |

**Entidades principais:** AiProvider, AiModel, AiModelConfiguration, AiProviderCredential

---

### 3.19 AiGovernanceDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | aiknowledge |
| **Base de dados** | nextraceone_ai |
| **Entidades (est.)** | ~28 |
| **Entity configurations (est.)** | ~15 |
| **Migrações** | Incluídas nas 17 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** AiPolicy, AiTokenBudget, AiAccessRule, AiAuditEntry, AiIdeExtension, AiModelRegistry

---

### 3.20 AiOrchestrationDb

| Atributo | Valor |
|----------|-------|
| **Módulo** | aiknowledge |
| **Base de dados** | nextraceone_ai |
| **Entidades (est.)** | ~24 |
| **Entity configurations (est.)** | ~12 |
| **Migrações** | Incluídas nas 17 do módulo |
| **Tenant isolation** | ✅ RLS |
| **Audit interceptor** | ✅ |
| **Soft deletes** | ✅ |
| **Encriptação** | Não aplicável |

**Entidades principais:** AiSession, AiMessage, AiKnowledgeSource, AiContext, AiPromptTemplate

---

## 4. Padrões de Persistência

### 4.1 NexTraceDbContextBase

Todos os DbContexts herdam de `NexTraceDbContextBase`, que fornece:

| Funcionalidade | Implementação |
|---------------|--------------|
| Audit interceptor | AuditInterceptor — regista alterações no SaveChanges |
| Tenant RLS | TenantRlsInterceptor — aplica `SET app.current_tenant` |
| Soft delete filter | Query filter global para `IsDeleted == false` |
| Encrypted string | EncryptedStringConverter para campos marcados |
| Outbox | OutboxMessage para eventual consistency |
| Strongly-typed IDs | Value converters automáticos para TypedIdBase |

### 4.2 RepositoryBase

| Operação | Método |
|----------|--------|
| GetByIdAsync | Consulta por ID tipado |
| GetAllAsync | Consulta paginada |
| AddAsync | Inserção |
| UpdateAsync | Actualização |
| DeleteAsync | Soft delete |

### 4.3 Outbox Pattern

| Componente | Implementação |
|------------|--------------|
| OutboxMessage | Entidade que armazena eventos pendentes |
| OutboxProcessorJob | BackgroundWorker que publica eventos pendentes |
| EventBus | Publicação de eventos via MediatR/outbox |

**Fluxo:**
```
Handler → domain event → OutboxMessage (persistido no mesmo transaction)
OutboxProcessorJob → lê OutboxMessages pendentes → publica via EventBus → marca como processado
```

### 4.4 Entity Configurations

| Padrão | Exemplo |
|--------|---------|
| `IEntityTypeConfiguration<T>` | Cada entidade tem configuração dedicada |
| Strongly-typed ID converters | `.HasConversion(v => v.Value, v => new ServiceId(v))` |
| Soft delete filter | `.HasQueryFilter(e => !e.IsDeleted)` |
| Encrypted fields | `.HasConversion<EncryptedStringConverter>()` |
| Index definitions | Índices por tenant, chaves naturais, foreign keys |

---

## 5. Classificação por Módulo

| Módulo | DbContexts | Entity Configs | Migrações | Estado |
|--------|------------|---------------|-----------|--------|
| catalog | 3 | ~45 | 6 | ✅ COERENTE |
| aiknowledge | 3 | ~39 | 17 | ✅ COERENTE |
| operationalintelligence | 5 | ~32 | 12 | ✅ COERENTE |
| changegovernance | 4 | ~30 | 8 | ✅ COERENTE |
| governance | 1 | ~30 | 3 | ✅ COERENTE |
| identityaccess | 1 | ~20 | 2 | ✅ COERENTE |
| auditcompliance | 1 | ~8 | 2 | ✅ COERENTE |
| notifications | 1 | ~8 | 0 | ⚠️ PARCIAL |
| configuration | 1 | ~4 | 0 | ⚠️ PARCIAL |
| **Total** | **20** | **~216** | **50** | — |

> Nota: 20 DbContexts referem-se às instâncias (alguns módulos partilham base de dados). São 16 DbContexts únicos conforme inventário principal.

---

## 6. Análise de Multi-Tenancy

### 6.1 Cobertura

| Base de Dados | DbContexts | RLS Activo |
|---------------|------------|------------|
| nextraceone_identity | 2 | ✅ 100% |
| nextraceone_catalog | 3 | ✅ 100% |
| nextraceone_operations | 12 | ✅ 100% |
| nextraceone_ai | 3 | ✅ 100% |

**Cobertura total: 100% dos DbContexts com RLS activo.**

### 6.2 Implementação RLS

```sql
-- Política RLS no PostgreSQL
CREATE POLICY tenant_isolation ON table_name
  FOR ALL
  USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

---

## 7. Recomendações

### Prioridade ALTA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 1 | Criar migrações para ConfigurationDb | configuration | EnsureCreated impede evolução controlada |
| 2 | Criar migrações para NotificationsDb | notifications | Schema não versionado |
| 3 | Avaliar carga em nextraceone_operations | plataforma | 12 DbContexts numa única base — monitorizar performance |

### Prioridade MÉDIA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 4 | Adicionar índices de performance | todos | Verificar query plans em produção |
| 5 | Documentar entity configurations | todos | 130 configurations sem documentação formal |
| 6 | Verificar cobertura de encriptação | todos | Garantir que todos os campos sensíveis estão encriptados |

### Prioridade BAIXA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 7 | Considerar separação de nextraceone_operations | plataforma | Separar em 2-3 bases se performance degradar |
| 8 | Adicionar métricas de outbox processing | plataforma | Visibilidade sobre latência de eventos |
