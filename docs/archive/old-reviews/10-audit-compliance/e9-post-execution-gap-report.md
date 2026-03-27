# E9 — Audit & Compliance Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ficou pendente,
e o que depende de outras fases após a execução do E9 para o módulo Audit & Compliance.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em 3 entidades mutáveis | Domain + Persistence | ✅ Concluído |
| 3 check constraints (Status, Severity, Outcome) | Persistence | ✅ Concluído |
| `IsRowVersion()` configurado em 3 entidades mutáveis | Persistence | ✅ Concluído |
| `audit:compliance:write` registado no RolePermissionCatalog | Security | ✅ Concluído |
| 5 permissões audit:* cobertas | Security | ✅ Concluído |
| README do módulo | Documentação | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 113/113 Audit + 290/290 Identity | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Gerar baseline migration (InitialCreate aud_) | Persistência | Requer Wave 4 | Wave 4 | 1 sprint |
| DbUpdateConcurrencyException handling → 409 | Backend (AUD-01) | Handler updates | E9+ | 2h |
| Frontend compliance pages (policies, campaigns, results) | Frontend (AUD-02) | New pages | E9+ | 8h |
| Export report integration in AuditPage | Frontend (AUD-03) | Button wiring | E9+ | 2h |
| EnvironmentId column on AuditEvent | Domain (AUD-04) | Schema addition | E9+ | 2h |
| Cross-module event production standardization | Backend (AUD-05) | All modules | Future | 8h |
| ClickHouse for high-volume audit analytics | Analytics (AUD-06) | ClickHouse setup | Wave 5 | 8h |
| Retention policy enforcement (background worker) | Backend (AUD-07) | Worker setup | E9+ | 4h |
| Hash chain rebuild/repair utility | Backend (AUD-08) | Admin endpoint | E9+ | 4h |
| ComplianceResult RowVersion (if mutable in future) | Domain (AUD-09) | Design decision | Future | 1h |
| i18n completeness for frontend pages | Frontend (AUD-10) | Translation | E9+ | 2h |
| Additional roles with audit:compliance:write | Security (AUD-11) | Role review | E9+ | 1h |

---

## 🚫 Não Bloqueia Evolução

Todos os itens pendentes são incrementais e **não bloqueiam** a evolução para:

1. **E10+** — Próximos módulos da trilha E
2. **Wave 4** — Baseline generation (Audit+Governance)
3. **Próximas releases** do produto

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E9 | Após E9 | Target |
|----------|-------------|---------|--------|
| Backend | 78% | 80% | 90% |
| Frontend | 45% | 48% | 85% |
| Persistência | 65% | 85% | 100% |
| Segurança | 68% | 80% | 90% |
| Documentação | 25% | 60% | 85% |
| Domínio | 80% | 87% | 95% |
| **Global** | **60%** | **73%** | **91%** |

A maturidade global subiu 13 pontos percentuais (60% → 73%), com os maiores ganhos
na persistência (65% → 85%) pelo RowVersion e check constraints,
segurança (68% → 80%) pela permissão audit:compliance:write,
e documentação (25% → 60%) pelo README abrangente.

---

## Decisões Tomadas Durante E9

1. **RowVersion apenas nas entidades mutáveis**: AuditEvent, AuditChainLink e ComplianceResult
   são imutáveis por design (eventos de auditoria nunca devem ser alterados), logo não necessitam
   de concorrência otimista.

2. **Prefixo aud_ já correto**: Diferentemente de outros módulos que precisaram rename (oi_→ops_,
   ci_→chg_, etc.), o módulo Audit já usava o prefixo correto `aud_` desde o início.

3. **audit:compliance:write**: Esta permissão era usada nos endpoints mas não estava registada
   no RolePermissionCatalog. Foi adicionada para PlatformAdmin (acesso total) e Auditor
   (responsável por compliance).

4. **Check constraints string-based**: Todos os 3 enums (CampaignStatus, ComplianceSeverity,
   ComplianceOutcome) são armazenados como string via `HasConversion<string>()`, então os
   constraints usam IN list com valores string.
