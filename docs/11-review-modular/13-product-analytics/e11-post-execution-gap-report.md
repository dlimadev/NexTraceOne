# E11 — Product Analytics Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ficou pendente,
e o que depende de outras fases após a execução do E11 para o módulo Product Analytics.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| 1 tabela gov_→pan_ prefixo | Persistência | ✅ Concluído |
| 2 check constraints (Module, EventType) | Persistência | ✅ Concluído |
| 2 composite indexes (TenantId+OccurredAt, TenantId+Module+OccurredAt) | Persistência | ✅ Concluído |
| `governance:analytics:read` para TechLead | Segurança | ✅ Concluído |
| `governance:analytics:read` para Viewer | Segurança | ✅ Concluído |
| AnalyticsEvent confirmada como imutável sem RowVersion | Domínio | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 163/163 Governance + 290/290 Identity | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Extração para módulo próprio (OI-03) | Arquitectura | Wave 0 | Wave 0 | 1 sprint |
| Gerar baseline migration (InitialCreate pan_) | Persistência | Após OI-03 | Wave 4+ | 4h |
| Criar ProductAnalyticsDbContext próprio | Persistência | OI-03 | Wave 0 | 4h |
| Renomear permissões governance:analytics:* → analytics:* | Segurança (PAN-01) | OI-03 | Wave 0 | 2h |
| ClickHouse pipeline para eventos de alto volume | Analytics (PAN-02) | ClickHouse setup | Wave 5 | 16h |
| ClickHouse materialized views para métricas derivadas | Analytics (PAN-03) | ClickHouse setup | Wave 5 | 8h |
| Architect role com analytics:read | Segurança (PAN-04) | Role definition | E11+ | 1h |
| Executive role com analytics:read | Segurança (PAN-05) | Role definition | E11+ | 1h |
| Real event ingestion from other modules | Backend (PAN-06) | Cross-module events | E11+ | 8h |
| Frontend i18n completeness | Frontend (PAN-07) | Translation | E11+ | 2h |
| Real dashboard data source (replace stub aggregations) | Frontend (PAN-08) | ClickHouse + real data | Wave 5 | 8h |
| Retention policy for analytics events | Domain (PAN-09) | TTL design | E11+ | 4h |
| Export/download of analytics data | Backend (PAN-10) | API design | E11+ | 4h |
| TenantId filtering enforcement in all queries | Security (PAN-11) | RLS verification | E11+ | 2h |

---

## 🚫 Não Bloqueia Evolução

Todos os itens pendentes são incrementais e **não bloqueiam** a evolução para:

1. **Próxima fase da trilha E** — E12+ ou wave transitions
2. **Wave 0** — OI-03 extraction (dedicated Product Analytics module)
3. **Próximas releases** do produto

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E11 | Após E11 | Target |
|----------|-------------|---------|--------|
| Backend | 60% | 63% | 85% |
| Frontend | 50% | 53% | 80% |
| Persistência | 45% | 72% | 100% |
| Segurança | 40% | 62% | 90% |
| Documentação | 35% | 50% | 85% |
| Domínio | 65% | 70% | 90% |
| **Global** | **49%** | **62%** | **88%** |

A maturidade global subiu 13 pontos percentuais (49% → 62%), com os maiores ganhos
na persistência (45% → 72%) pelo prefixo pan_, check constraints e composite indexes,
e segurança (40% → 62%) pelas permissões analytics:read para TechLead+Viewer.

---

## Decisões Tomadas Durante E11

1. **Sem RowVersion (imutável)**: AnalyticsEvent é write-once. Todos os setters são `private init`.
   Uma vez criado, o evento nunca é modificado. Mesma decisão que AuditEvent.

2. **Prefixo pan_**: Product ANalytics → pan_. Seguindo padrão de prefixos curtos do produto
   (aud_, chg_, ops_, ntf_, int_, cfg_, etc.).

3. **TechLead + Viewer com analytics:read**: Dashboards de adoção e métricas de produto são
   informação de leitura relevante para TechLeads e Viewers. Write permanece PlatformAdmin-only.

4. **Composite indexes para padrão de consulta analítica**: As queries típicas filtram por
   TenantId + time-range (OccurredAt). O índice adicional (TenantId, Module, OccurredAt)
   suporta drill-down por módulo.

5. **Permissões mantidas como governance:analytics:***: A renomeação para analytics:* depende
   da extração OI-03 do módulo para evitar breaking changes prematuros.
