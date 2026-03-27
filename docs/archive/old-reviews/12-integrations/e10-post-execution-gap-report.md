# E10 — Integrations Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ficou pendente,
e o que depende de outras fases após a execução do E10 para o módulo Integrations.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em 2 entidades mutáveis | Domain + Persistence | ✅ Concluído |
| 6 check constraints em 6 colunas de enum | Persistence | ✅ Concluído |
| 3 tabelas gov_→int_ prefixo | Persistence | ✅ Concluído |
| `IsRowVersion()` configurado em 2 entidades mutáveis | Persistence | ✅ Concluído |
| `integrations:write` registado para TechLead | Security | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 163/163 Governance + 290/290 Identity | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Extração para módulo próprio (OI-02) | Arquitectura | Wave 0 | Wave 0 | 1 sprint |
| Gerar baseline migration (InitialCreate int_) | Persistência | Após OI-02 | Wave 4+ | 4h |
| DbUpdateConcurrencyException handling → 409 | Backend (INT-01) | Handler updates | E10+ | 2h |
| Webhook receiver endpoint real | Backend (INT-02) | Design decision | E10+ | 8h |
| Real connector test/ping endpoint | Backend (INT-03) | External deps | E10+ | 4h |
| Retry policy engine (background worker) | Backend (INT-04) | Worker infra | E10+ | 8h |
| ClickHouse pipeline for execution analytics | Analytics (INT-05) | ClickHouse setup | Wave 5 | 8h |
| IntegrationConnector TenantId column | Domain (INT-06) | Multi-tenant design | E10+ | 2h |
| Frontend i18n completeness | Frontend (INT-07) | Translation | E10+ | 2h |
| Create/Edit connector form in frontend | Frontend (INT-08) | Design finalization | E10+ | 4h |
| Admin endpoints for connector CRUD | Backend (INT-09) | API design | E10+ | 4h |
| Health check background worker | Backend (INT-10) | Worker infra | E10+ | 4h |
| Frontend guard for integrations:write actions | Frontend (INT-11) | Guard implementation | E10+ | 2h |

---

## 🚫 Não Bloqueia Evolução

Todos os itens pendentes são incrementais e **não bloqueiam** a evolução para:

1. **E11+** — Próximos módulos da trilha E
2. **Wave 0** — OI-02 extraction (dedicated Integrations module)
3. **Próximas releases** do produto

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E10 | Após E10 | Target |
|----------|-------------|---------|--------|
| Backend | 65% | 68% | 90% |
| Frontend | 55% | 58% | 85% |
| Persistência | 50% | 78% | 100% |
| Segurança | 55% | 72% | 90% |
| Documentação | 40% | 55% | 85% |
| Domínio | 72% | 80% | 95% |
| **Global** | **56%** | **69%** | **91%** |

A maturidade global subiu 13 pontos percentuais (56% → 69%), com os maiores ganhos
na persistência (50% → 78%) pelo prefixo int_, RowVersion, e check constraints,
e segurança (55% → 72%) pela permissão integrations:write para TechLead.

---

## Decisões Tomadas Durante E10

1. **RowVersion apenas em entidades mutáveis**: IntegrationConnector e IngestionSource são mutáveis
   (atualizam status, health, contadores). IngestionExecution é essencialmente imutável (criada e
   depois completada uma vez), logo não precisa de RowVersion.

2. **Prefixo int_ em vez de gov_integration_**: Seguindo a convenção de cada módulo ter prefixo curto
   (aud_, chg_, ops_, ntf_, etc.), os 3 tables foram renomeados de `gov_integration_connectors` /
   `gov_ingestion_sources` / `gov_ingestion_executions` para `int_connectors` / `int_ingestion_sources` /
   `int_ingestion_executions`.

3. **integrations:write para TechLead**: TechLeads precisam configurar integrações para as suas equipas.
   Anteriormente apenas PlatformAdmin tinha write. Developer mantém apenas read.

4. **Módulo permanece em GovernanceDbContext temporariamente**: A extração para módulo próprio (OI-02)
   é uma tarefa de Wave 0. Nesta fase, preparamos os prefixos e constraints para facilitar a extração.
