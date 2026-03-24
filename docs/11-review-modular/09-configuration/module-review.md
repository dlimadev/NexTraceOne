# Revisão Modular — Configuration

> **Data:** 2026-03-24  
> **Prioridade:** P3 (Módulo Transversal)  
> **Módulo Backend:** `src/modules/configuration/`  
> **Módulo Frontend:** `src/frontend/src/features/configuration/`  
> **Fonte de verdade:** Código do repositório

---

## 1. Propósito do Módulo

O módulo **Configuration** é o módulo transversal que centraliza todas as definições de configuração do sistema. Toda feature do NexTraceOne usa configuração proveniente deste módulo.

---

## 2. Aderência ao Produto

| Aspecto | Avaliação | Observação |
|---------|-----------|------------|
| Alinhamento | ✅ Forte | Configuração centralizada é essencial para plataforma enterprise |
| Completude | ✅ Alta | ~345 definições, 251 testes backend, 82 testes frontend |
| Maturidade | ✅ Muito Alta | 8 fases de seed, advanced console com 6 tabs |
| **Problema principal** | ⚠️ **Documentação fragmentada** | 35 ficheiros execution/CONFIGURATION-* sem doc unificada |

---

## 3. Definições de Configuração (~345 definições)

As definições foram implementadas em 8 fases:

| Fase | Domínio | Definições | Key Prefix | SortOrder Range |
|------|---------|-----------|------------|----------------|
| Phase 0 | Foundation (instance) | ~5 | instance.* | 1-10 |
| Phase 1 | Foundation (feature flags, policies) | ~10 | instance.*, policies.* | 10-50 |
| Phase 2 | Notifications | 38 | notifications.* | 150-201 |
| Phase 3 | Workflow & Promotion | 45 | workflow.*, promotion.* | 2000-2650 |
| Phase 4 | Governance & Compliance | 44 | governance.* | 3000-3540 |
| Phase 5 | Catalog, Contracts & Change | 49 | catalog.*, change.* | 4000-4690 |
| Phase 6 | Operations, Incidents, FinOps | 53 | incidents.*, operations.*, finops.*, benchmarking.* | 5000-5620 |
| Phase 7 | AI & Integrations | 55 | ai.*, integrations.* | 6000-6670 |

### Propriedades de Cada Definição

| Campo | Tipo | Propósito |
|-------|------|-----------|
| Key | string | Chave única (ex: notifications.mandatory.types) |
| DisplayName | string | Nome para UI |
| Description | string | Descrição |
| Category | enum | System / Functional |
| DataType | enum | String / Integer / Boolean / Json / Decimal |
| EditorType | enum | Text / Toggle / Select / Json / Number / Tags / Textarea |
| DefaultValue | string | Valor default |
| SortOrder | int | Ordem de exibição |
| Scope | enum | Instance / Tenant / Environment |
| IsInheritable | bool | Se herda valor do scope pai |
| IsMandatory | bool | Se é obrigatório |
| ValidationRules | JSON | Regras de validação |

---

## 4. Páginas Frontend

### 4.1 Páginas do Módulo Configuration

| Página | Rota | Estado | Funcionalidade |
|--------|------|--------|----------------|
| ConfigurationAdminPage | `/platform/configuration` | ✅ Funcional | Hub de configuração com links para 6 domínios |
| AdvancedConfigurationConsolePage | `/platform/configuration/advanced` | ✅ Funcional | Console avançado com 6 tabs e 9 filtros |

### 4.2 Páginas de Configuração Distribuídas (Outras Features)

| Página | Rota | Feature | Secções |
|--------|------|---------|---------|
| NotificationConfigurationPage | `/platform/configuration/notifications` | notifications | 6: types, channels, templates, routing, consumption, escalation |
| WorkflowConfigurationPage | `/platform/configuration/workflows` | change-governance | 7: templates, stages, approvers, sla, gates, promotion, freeze |
| GovernanceConfigurationPage | `/platform/configuration/governance` | governance | 6: policies, evidence, waivers, packs, scorecards, requirements |
| CatalogContractsConfigurationPage | `/platform/configuration/catalog-contracts` | catalog | 7: contracts, validation, requirements, publication, importExport, changeTypes, releaseScoring |
| OperationsFinOpsConfigurationPage | `/platform/configuration/operations-finops` | operational-intelligence | 6: incidentTaxonomy, ownersCorrelation, playbooksAutomation, budgets, anomalyWaste, benchmarking |
| AiIntegrationsConfigurationPage | `/platform/configuration/ai-integrations` | ai-hub | 6: providersModels, budgetsQuotas, promptsRetrieval, connectorsSchedules, filtersMappings, failureGovernance |

### 4.3 Advanced Console — 6 Tabs

| Tab | Funcionalidade |
|-----|---------------|
| Explorer | Navegar definições por domínio (9 filtros) |
| Diff | Comparar valores entre scopes |
| Import/Export | JSON export com masking de valores sensíveis |
| Rollback | Rollback por versão |
| History | Histórico de auditoria |
| Health | 5 health checks |

---

## 5. Backend

### 5.1 Features CQRS

| Feature | Propósito |
|---------|-----------|
| GetDefinitions | Listar definições de configuração |
| GetEntries | Listar valores de configuração por scope |
| GetEffectiveSettings | Obter configuração efetiva (herança) |
| SetConfigurationValue | Definir valor |
| ToggleConfiguration | Alternar boolean |
| RemoveOverride | Remover override de scope |
| GetAuditHistory | Histórico de alterações |

### 5.2 Hooks Frontend

| Hook | Propósito |
|------|-----------|
| useConfiguration | React Query factory para definitions, entries, effective settings, audit logs |

---

## 6. Banco de Dados

| DbContext | Entidades |
|-----------|-----------|
| ConfigurationDbContext | ConfigurationDefinition, ConfigurationEntry, ConfigurationAuditEntry |

---

## 7. Testes

| Tipo | Quantidade | Cobertura |
|------|-----------|----------|
| Backend (seed validation) | 251 | Valida unique keys, unique sortOrders, categorias, prefixos, defaults, editors |
| Frontend | 82 | Componentes e hooks |

---

## 8. Resumo de Ações

### Ações Importantes (P1)

| # | Ação | Esforço |
|---|------|---------|
| 1 | **Criar documentação unificada** — consolidar 35 ficheiros execution/CONFIGURATION-* num documento de referência | 4h |
| 2 | Validar herança de configuração (Instance → Tenant → Environment) | 2h |
| 3 | Validar advanced console (6 tabs, export masking, rollback) | 2h |

### Ações de Melhoria (P2)

| # | Ação | Esforço |
|---|------|---------|
| 4 | Documentar todas as ~345 definições com categorias e valores default | 3h |
| 5 | Verificar cobertura de testes para todas as 8 fases | 2h |
| 6 | Avaliar se as 6 páginas distribuídas devem ser unificadas ou manter separação | 1h |
