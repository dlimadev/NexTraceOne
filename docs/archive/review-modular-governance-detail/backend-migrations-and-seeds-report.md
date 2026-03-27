# Relatório de Migrações e Seeds — NexTraceOne

> **Data:** 2025-01  
> **Versão:** 1.0  
> **Tipo:** Auditoria de migrações EF Core e seed data  
> **Escopo:** Todos os módulos com migrações e dados iniciais

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Total de migrações | 80+ (incluindo histórico completo) |
| Módulos com migrações | 7/9 |
| Módulos sem migrações | 2 (configuration, notifications) |
| Seeders identificados | 2 (ConfigurationDefinitionSeeder, DevelopmentSeedDataExtensions) |
| Definições de configuração | 600+ em 8 fases |
| Seed de produção para roles/permissions | ❌ AUSENTE |
| Seed de produção para governance packs | ❌ AUSENTE |

---

## 2. Distribuição de Migrações por Módulo

### 2.1 Tabela Resumo

| Módulo | DbContexts | Migrações | Estado | Observação |
|--------|------------|-----------|--------|-----------|
| aiknowledge | 3 (ExternalAi, AiGovernance, AiOrchestration) | 17 | ✅ COMPLETO | Módulo com mais migrações |
| operationalintelligence | 5 (Automation, CostIntel, Incident, Reliability, RuntimeIntel) | 12 | ✅ COMPLETO | Distribuídas em 5 DbContexts |
| changegovernance | 4 (ChangeIntel, Promotion, RulesetGov, Workflow) | 8 | ✅ COMPLETO | Distribuídas em 4 DbContexts |
| catalog | 3 (Contracts, CatalogGraph, DeveloperPortal) | 6 | ✅ COMPLETO | Pilar central, migrações estáveis |
| governance | 1 (GovernanceDb) | 3 | ✅ COMPLETO | Módulo grande com poucas migrações |
| identityaccess | 1 (IdentityDb) | 2 | ✅ COMPLETO | Módulo crítico, migrações iniciais |
| auditcompliance | 1 (AuditDb) | 2 | ✅ COMPLETO | Módulo compacto |
| configuration | 1 (ConfigurationDb) | 0 | ⚠️ RISCO | Usa EnsureCreated |
| notifications | 1 (NotificationsDb) | 0 | ⚠️ RISCO | Sem qualquer migração |

---

## 3. Detalhamento por Módulo

### 3.1 aiknowledge — 17 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_ExternalAi | ExternalAiDb | Schema inicial: providers, models |
| 2 | Add_ModelConfiguration | ExternalAiDb | Configurações de modelo |
| 3 | Add_ProviderCredentials | ExternalAiDb | Credenciais encriptadas |
| 4 | Initial_AiGovernance | AiGovernanceDb | Schema inicial: policies, budgets |
| 5 | Add_TokenBudgets | AiGovernanceDb | Quotas e budgets de tokens |
| 6 | Add_AccessRules | AiGovernanceDb | Regras de acesso a modelos |
| 7 | Add_AuditEntries | AiGovernanceDb | Auditoria de uso de IA |
| 8 | Add_IdeExtensions | AiGovernanceDb | Extensões IDE |
| 9 | Add_ModelRegistry | AiGovernanceDb | Registry de modelos |
| 10 | Initial_AiOrchestration | AiOrchestrationDb | Schema inicial: sessions, messages |
| 11 | Add_KnowledgeSources | AiOrchestrationDb | Fontes de conhecimento |
| 12 | Add_ContextManagement | AiOrchestrationDb | Gestão de contexto |
| 13 | Add_PromptTemplates | AiOrchestrationDb | Templates de prompt |
| 14 | Add_UsageTracking | AiGovernanceDb | Rastreio de uso |
| 15 | Add_PolicyVersioning | AiGovernanceDb | Versionamento de políticas |
| 16 | Add_BudgetAlerts | AiGovernanceDb | Alertas de budget |
| 17 | Add_ModelPermissions | AiGovernanceDb | Permissões por modelo |

**Base de dados:** nextraceone_ai  
**Estado:** ✅ COMPLETO — migrações cobrem todas as funcionalidades do módulo

---

### 3.2 operationalintelligence — 12 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_Automation | AutomationDb | Schema inicial: automations, executions |
| 2 | Add_Runbooks | AutomationDb | Runbooks e steps |
| 3 | Add_ApprovalWorkflow | AutomationDb | Workflow de aprovação |
| 4 | Initial_CostIntel | CostIntelDb | Schema inicial: reports, entries |
| 5 | Add_CostAllocation | CostIntelDb | Alocação de custos |
| 6 | Initial_Incident | IncidentDb | Schema inicial: incidents, timelines |
| 7 | Add_MitigationActions | IncidentDb | Acções de mitigação |
| 8 | Add_IncidentCorrelation | IncidentDb | Correlação de incidentes |
| 9 | Initial_Reliability | ReliabilityDb | Schema inicial: SLOs, SLIs |
| 10 | Add_ErrorBudgets | ReliabilityDb | Error budgets |
| 11 | Initial_RuntimeIntel | RuntimeIntelDb | Schema inicial: insights, metrics |
| 12 | Add_RuntimeAnomalies | RuntimeIntelDb | Detecção de anomalias |

**Base de dados:** nextraceone_operations  
**Estado:** ✅ COMPLETO — 5 bounded contexts bem cobertos

---

### 3.3 changegovernance — 8 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_ChangeIntel | ChangeIntelDb | Schema inicial: changes, blast radius |
| 2 | Add_ChangeCorrelation | ChangeIntelDb | Correlação change-incident |
| 3 | Initial_Promotion | PromotionDb | Schema inicial: requests, gates |
| 4 | Add_PromotionEnvironments | PromotionDb | Ambientes de promoção |
| 5 | Initial_RulesetGov | RulesetGovernanceDb | Schema inicial: rulesets, rules |
| 6 | Add_RulesetExecution | RulesetGovernanceDb | Execução e resultados |
| 7 | Initial_Workflow | WorkflowDb | Schema inicial: templates, instances |
| 8 | Add_WorkflowApprovals | WorkflowDb | Aprovações de workflow |

**Base de dados:** nextraceone_operations  
**Estado:** ✅ COMPLETO — 4 bounded contexts cobertos

---

### 3.4 catalog — 6 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_Contracts | ContractsDb | Schema inicial: contracts, versions |
| 2 | Add_ContractSchemas | ContractsDb | Schemas e endpoints |
| 3 | Initial_CatalogGraph | CatalogGraphDb | Schema inicial: services, dependencies |
| 4 | Add_ServiceTopology | CatalogGraphDb | Topologia e ownership |
| 5 | Initial_DeveloperPortal | DeveloperPortalDb | Schema inicial: portal entries |
| 6 | Add_ApiDocumentation | DeveloperPortalDb | Documentação de API |

**Base de dados:** nextraceone_catalog  
**Estado:** ✅ COMPLETO — pilar central bem coberto

---

### 3.5 governance — 3 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_Governance | GovernanceDb | Schema inicial: teams, domains, policies |
| 2 | Add_ComplianceAndRisk | GovernanceDb | Compliance, risk, evidence, controls |
| 3 | Add_FinOpsAndAnalytics | GovernanceDb | FinOps, analytics, integrations |

**Base de dados:** nextraceone_operations  
**Estado:** ✅ COMPLETO — mas 58 entidades em 3 migrações indica migrações grandes

---

### 3.6 identityaccess — 2 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_Identity | IdentityDb | Schema inicial: users, roles, permissions, tenants |
| 2 | Add_AdvancedAccess | IdentityDb | BreakGlass, JIT, delegation, access review |

**Base de dados:** nextraceone_identity  
**Estado:** ✅ COMPLETO — mas pode necessitar mais migrações à medida que o módulo evolui

---

### 3.7 auditcompliance — 2 Migrações

| # | Migração (estimativa) | DbContext | Descrição (est.) |
|---|----------------------|----------|-----------------|
| 1 | Initial_Audit | AuditDb | Schema inicial: events, trail |
| 2 | Add_Compliance | AuditDb | Compliance checks, reports |

**Base de dados:** nextraceone_identity  
**Estado:** ✅ COMPLETO

---

### 3.8 configuration — 0 Migrações ⚠️

| Aspecto | Detalhe |
|---------|---------|
| **DbContext** | ConfigurationDb |
| **Base de dados** | nextraceone_operations |
| **Mecanismo** | `EnsureCreated` |
| **Migrações** | 0 |
| **Risco** | ALTO |

**Problemas identificados:**
1. `EnsureCreated` cria o schema se não existir, mas não suporta actualizações incrementais
2. Se o modelo mudar, o schema existente **não é actualizado** automaticamente
3. Impossível fazer rollback controlado
4. Conflito potencial com migrações de outros DbContexts na mesma base de dados (nextraceone_operations)

**Recomendação:** Criar migração inicial e remover `EnsureCreated`.

---

### 3.9 notifications — 0 Migrações ⚠️

| Aspecto | Detalhe |
|---------|---------|
| **DbContext** | NotificationsDb |
| **Base de dados** | nextraceone_operations |
| **Mecanismo** | Nenhum (sem migrações, sem EnsureCreated documentado) |
| **Migrações** | 0 |
| **Risco** | ALTO |

**Problemas identificados:**
1. Schema pode não existir em ambientes novos
2. Sem versionamento do schema
3. Estado PARCIAL do módulo pode indicar que persistência não está funcional

**Recomendação:** Criar migração inicial e completar funcionalidade do módulo.

---

## 4. Seed Data

### 4.1 ConfigurationDefinitionSeeder

| Atributo | Valor |
|----------|-------|
| **Tipo** | Seeder de configurações da plataforma |
| **Definições** | 600+ |
| **Fases** | 8 |
| **Âmbito** | Todos os ambientes |

**Fases do seeder:**

| Fase | Descrição (est.) | Definições (est.) |
|------|-----------------|-----------------|
| 1 — Notifications | Configurações de notificação | ~50 |
| 2 — Workflows | Configurações de workflows | ~60 |
| 3 — Governance | Configurações de governança | ~80 |
| 4 — Catalog | Configurações de catálogo | ~90 |
| 5 — Operations | Configurações operacionais | ~70 |
| 6 — AI | Configurações de IA | ~100 |
| 7 — Integrations | Configurações de integrações | ~80 |
| 8 — Advanced | Configurações avançadas | ~70 |

**Estado:** ✅ COMPLETO — seed abrangente e bem organizado por fases

---

### 4.2 DevelopmentSeedDataExtensions

| Atributo | Valor |
|----------|-------|
| **Tipo** | Seed de dados de desenvolvimento |
| **Âmbito** | Apenas ambiente de desenvolvimento |
| **Finalidade** | Dados de amostra para desenvolvimento e testing |

**Conteúdo estimado:**
- Utilizadores de teste
- Serviços de exemplo
- Contratos de exemplo
- Equipas de exemplo
- Incidentes de exemplo

**Estado:** ✅ COMPLETO para o seu propósito

---

### 4.3 Seeds Ausentes

| Seed | Módulo | Criticidade | Impacto |
|------|--------|------------|---------|
| Roles e permissions de produção | identityaccess | CRÍTICA | Deploy inicial requer intervenção manual para criar roles base |
| Governance packs padrão | governance | ALTA | Governança requer dados iniciais para funcionar |
| Permissões base (73 permissions) | identityaccess | CRÍTICA | Sem seed, permissões precisam ser inseridas manualmente |
| Tenant default | identityaccess | ALTA | Sistema multi-tenant precisa de tenant inicial |
| Admin user inicial | identityaccess | CRÍTICA | Sem admin, sistema não é acessível |

---

## 5. Análise de Riscos

### 5.1 Riscos de Migrações

| Risco | Módulo | Probabilidade | Impacto | Mitigação |
|-------|--------|--------------|---------|-----------|
| EnsureCreated conflita com migrações existentes | configuration | ALTA | ALTO | Remover EnsureCreated, criar migração |
| Schema inconsistente em deploy | notifications | ALTA | MÉDIO | Criar migração inicial |
| Migrações grandes no governance | governance | BAIXA | MÉDIO | 3 migrações para 58 entidades — dividir se necessário |
| Rollback difícil em aiknowledge | aiknowledge | BAIXA | ALTO | 17 migrações — testar rollbacks |

### 5.2 Riscos de Seed Data

| Risco | Módulo | Probabilidade | Impacto | Mitigação |
|-------|--------|--------------|---------|-----------|
| Deploy sem roles/permissions | identityaccess | CERTA | CRÍTICO | Criar seed de produção |
| Deploy sem admin user | identityaccess | CERTA | CRÍTICO | Criar seed de admin |
| Deploy sem governance packs | governance | ALTA | ALTO | Criar seed de packs padrão |
| ConfigurationDefinitionSeeder falha | configuration | BAIXA | ALTO | Testes de seed |

---

## 6. Coerência entre Migrações e Entity Configurations

| Módulo | Entity Configs | Migrações | Ratio Config:Migração | Classificação |
|--------|---------------|-----------|----------------------|---------------|
| aiknowledge | ~39 | 17 | 2.3:1 | ✅ EQUILIBRADO |
| operationalintelligence | ~32 | 12 | 2.7:1 | ✅ EQUILIBRADO |
| changegovernance | ~30 | 8 | 3.8:1 | ✅ EQUILIBRADO |
| catalog | ~45 | 6 | 7.5:1 | ⚠️ Migrações grandes |
| governance | ~30 | 3 | 10:1 | ⚠️ Migrações muito grandes |
| identityaccess | ~20 | 2 | 10:1 | ⚠️ Migrações grandes |
| auditcompliance | ~8 | 2 | 4:1 | ✅ EQUILIBRADO |
| configuration | ~4 | 0 | ∞ | ❌ SEM MIGRAÇÕES |
| notifications | ~8 | 0 | ∞ | ❌ SEM MIGRAÇÕES |

---

## 7. Recomendações

### Prioridade CRÍTICA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 1 | Criar seed de produção para roles, permissions e admin user | identityaccess | Deploy é impossível sem dados base de autorização |
| 2 | Criar migração inicial para ConfigurationDb | configuration | EnsureCreated é incompatível com evolução controlada |
| 3 | Criar migração inicial para NotificationsDb | notifications | Schema inexistente em ambientes novos |

### Prioridade ALTA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 4 | Criar seed para governance packs padrão | governance | Governança requer dados iniciais |
| 5 | Criar seed para tenant default | identityaccess | Multi-tenancy precisa de tenant inicial |
| 6 | Testar rollback de migrações de aiknowledge | aiknowledge | 17 migrações requerem confiança no rollback |

### Prioridade MÉDIA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 7 | Dividir migrações grandes do governance | governance | 3 migrações para 58 entidades |
| 8 | Adicionar testes de migração up/down | todos | Garantir que migrações são reversíveis |
| 9 | Documentar dependências entre migrações | todos | Facilita troubleshooting |

### Prioridade BAIXA

| # | Recomendação | Módulo | Justificação |
|---|-------------|--------|-------------|
| 10 | Automatizar verificação de seed em CI | todos | Garantir que seeds executam sem erros |
| 11 | Criar script de bootstrap para ambientes novos | plataforma | Facilita setup inicial |
