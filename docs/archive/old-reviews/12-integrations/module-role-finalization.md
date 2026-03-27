# Integrations — Module Role Finalization

> **Module:** Integrations (12)  
> **Table prefix (target):** `int_`  
> **Date:** 2026-03-25  
> **Status:** Role finalized — pending extraction from Governance

---

## 1. Papel oficial do módulo

O módulo **Integrations** é o **hub de integrações externas** do NexTraceOne. É responsável pela gestão operacional de conectores com sistemas externos, ingestão de dados, monitorização de health e freshness, e rastreabilidade de execuções.

### O que Integrations É

| Responsabilidade | Descrição |
|-----------------|-----------|
| **Gestão de conectores** | CRUD e configuração de conectores com sistemas externos (GitHub, Datadog, PagerDuty, Jira, etc.) |
| **Ingestão de dados** | Orquestração e monitorização de pipelines de ingestão de dados de fontes externas |
| **Execução e rastreabilidade** | Registo, tracking e histórico de todas as execuções de ingestão |
| **Freshness monitoring** | Monitorização da frescura dos dados ingeridos por domínio |
| **Health monitoring** | Monitorização da saúde operacional dos conectores |
| **Retries e reprocessamento** | Lógica de retry para conectores falhados e reprocessamento de execuções |
| **Status operacional** | Gestão do estado operacional de conectores e fontes (Active, Paused, Failed, etc.) |
| **Trust levels** | Classificação de confiança das fontes de dados (Unverified → Official) |
| **Webhooks** | Recepção e processamento de webhooks de sistemas externos |
| **Configuração operacional** | Endpoint, modo de autenticação, polling mode, teams permitidas |

### O que Integrations NÃO É

| Anti-padrão | Dono correcto |
|------------|--------------|
| Políticas de aprovação de integrações | **Governance** (08) |
| Compliance reporting sobre uso de integrações | **Governance** (08) |
| Risk assessment de dependências de integração | **Governance** (08) |
| Executive views sobre saúde de integrações | **Governance** (08) |
| Notificação de falhas de integração | **Notifications** (11) — consome eventos de Integrations |
| Auditoria de acções sobre integrações | **Audit & Compliance** (10) — consome eventos de Integrations |
| Definição de serviços/APIs integrados | **Service Catalog** (03) |
| Gestão de ambientes onde integrações correm | **Environment Management** (02) |

---

## 2. Entidades que o módulo é dono

| Entidade | Tipo | Descrição |
|----------|------|-----------|
| `IntegrationConnector` | Aggregate Root | Conector com sistema externo — define tipo, provider, endpoint, status, health |
| `IngestionSource` | Entity (child of Connector) | Fonte de dados dentro de um conector — webhook, API polling, etc. |
| `IngestionExecution` | Entity (child of Connector/Source) | Registo de uma execução de ingestão — resultado, duração, items processados |

---

## 3. Estado actual

| Dimensão | Estado | Nota |
|----------|--------|------|
| Backend | ⚠️ **Em Governance** | Entidades, endpoints, features todos em `src/modules/governance/` |
| Frontend | ✅ Independente | `src/frontend/src/features/integrations/` com 4 páginas |
| DbContext | ❌ **Partilhado** | Usa `GovernanceDbContext` — sem `IntegrationsDbContext` próprio |
| Table prefix | ❌ **`gov_`** | Tabelas usam `gov_integration_connectors`, target é `int_` |
| Permissões | ✅ Registadas | `integrations:read` e `integrations:write` em `RolePermissionCatalog` |
| Documentação | ❌ **0%** | Zero documentação dedicada |
| Migrations | ⚠️ Em Governance | 3 migrations dentro do módulo Governance |

---

## 4. Dependências principais

| Módulo | Tipo | Detalhe |
|--------|------|---------|
| **Governance** (08) | 🔴 Coupling forte | Backend fisicamente dentro de Governance — bloqueador |
| **Identity & Access** (01) | Integração | JWT, TenantId, permissões |
| **Configuration** (09) | Referência | Parâmetros de configuração de conectores |
| **Notifications** (11) | Emissão | Integrations publica eventos → Notifications consome |
| **Audit & Compliance** (10) | Emissão | Integrations publica eventos → Audit consome |
| **Operational Intelligence** (06) | Emissão | Dados de integração alimentam métricas operacionais |

---

## 5. Posição no produto

O módulo Integrations é **crítico para o funcionamento real do NexTraceOne** porque:

1. Sem conectores reais, o produto não recebe dados de sistemas externos
2. Sem ingestão funcional, módulos como Change Governance e Operational Intelligence operam com dados seed
3. Sem health/freshness monitoring, não há confiança nos dados ingeridos
4. Sem retries, falhas de integração não são recuperáveis

**Prioridade:** ALTA para consolidação estrutural; MÉDIA para funcionalidades avançadas.

---

## 6. Decisão arquitectural confirmada

Conforme `docs/architecture/module-frontier-decisions.md`:

> **Integrations é módulo independente.** O backend deve ser extraído de Governance para `src/modules/integrations/` com `IntegrationsDbContext` próprio e prefixo `int_`.

Esta extracção é o bloqueador principal do módulo (OI-02 em `phase-a-open-items.md`).
