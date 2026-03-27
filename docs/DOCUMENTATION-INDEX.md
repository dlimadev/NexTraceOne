# Índice de Documentação — NexTraceOne

> **Última actualização:** 2026-03-27 (P12.3 — Documentation Consolidation)

Este índice distingue documentação **activa** de documentação **arquivada** e serve como ponto de entrada único para navegar o repositório.

---

## 1. Documentação Raiz (Activa)

| Ficheiro | Descrição |
|---------|-----------|
| [`README.md`](../README.md) | Introdução ao repositório |
| [`PRODUCT-SCOPE.md`](PRODUCT-SCOPE.md) | Escopo do produto por onda |
| [`ROADMAP.md`](ROADMAP.md) | Roadmap por ondas e sprints |
| [`MODULES-AND-PAGES.md`](MODULES-AND-PAGES.md) | Módulos e páginas do produto |
| [`CONTRACT-STUDIO-VISION.md`](CONTRACT-STUDIO-VISION.md) | Visão do Contract Studio |
| [`BACKEND-MODULE-GUIDELINES.md`](BACKEND-MODULE-GUIDELINES.md) | Guidelines de backend |
| [`SECURITY.md`](SECURITY.md) | Modelo de segurança |
| [`SOLUTION-GAP-ANALYSIS.md`](SOLUTION-GAP-ANALYSIS.md) | Análise de gaps da solução |
| [`EXECUTION-BASELINE-PR1-PR16.md`](EXECUTION-BASELINE-PR1-PR16.md) | Baseline de execução |
| [`REBASELINE.md`](REBASELINE.md) | Inventário do estado actual |
| [`DEPLOYMENT-ARCHITECTURE.md`](DEPLOYMENT-ARCHITECTURE.md) | Arquitectura de deployment |

---

## 2. Architecture (docs/architecture/)

### ADRs (Architecture Decision Records)
| Ficheiro | Decisão |
|---------|---------|
| [`adr/ADR-001-database-strategy.md`](architecture/adr/ADR-001-database-strategy.md) | Estratégia de base de dados |
| [`adr/ADR-002-migration-policy.md`](architecture/adr/ADR-002-migration-policy.md) | Política de migrações |
| [`adr/ADR-003-event-bus-limitations.md`](architecture/adr/ADR-003-event-bus-limitations.md) | Limitações do event bus |
| [`adr/ADR-004-simulated-data-policy.md`](architecture/adr/ADR-004-simulated-data-policy.md) | Política de dados simulados |
| [`adr/ADR-005-ai-runtime-foundation.md`](architecture/adr/ADR-005-ai-runtime-foundation.md) | Fundação do runtime AI |
| [`adr/ADR-006-agent-runtime-foundation.md`](architecture/adr/ADR-006-agent-runtime-foundation.md) | Fundação do runtime de agentes |

### Estado Actual da Arquitectura
| Ficheiro | Descrição |
|---------|-----------|
| [`database-table-prefixes.md`](architecture/database-table-prefixes.md) | Prefixos de tabela por módulo |
| [`module-boundary-matrix.md`](architecture/module-boundary-matrix.md) | Matriz de fronteiras de módulo |
| [`final-data-placement-matrix.md`](architecture/final-data-placement-matrix.md) | Placement final de dados |
| [`clickhouse-baseline-strategy.md`](architecture/clickhouse-baseline-strategy.md) | Estratégia ClickHouse |
| [`architecture-decisions-final.md`](architecture/architecture-decisions-final.md) | Decisões arquitecturais finais |

### Relatórios de Fase Activos (P-series)
Relatórios de execução das fases P1–P12. Ver `docs/architecture/p*.md` e `docs/architecture/p*-*.md`.

---

## 3. Revisão Modular (docs/11-review-modular/)

### 00-governance — Documentos Canónicos (12 docs)
| Documento | Tema |
|-----------|------|
| [`modular-review-summary.md`](11-review-modular/00-governance/modular-review-summary.md) | **Sumário geral** — ponto de entrada |
| [`final-consolidation-and-master-plan.md`](11-review-modular/00-governance/final-consolidation-and-master-plan.md) | Plano mestre e consolidação |
| [`review-status-overview.md`](11-review-modular/00-governance/review-status-overview.md) | Estado geral da revisão |
| [`product-maturity-summary.md`](11-review-modular/00-governance/product-maturity-summary.md) | Maturidade do produto |
| [`backend-structural-audit.md`](11-review-modular/00-governance/backend-structural-audit.md) | Canonical: estado backend |
| [`frontend-structural-audit.md`](11-review-modular/00-governance/frontend-structural-audit.md) | Canonical: estado frontend |
| [`database-structural-audit.md`](11-review-modular/00-governance/database-structural-audit.md) | Canonical: estado database |
| [`security-cross-layer-gap-report.md`](11-review-modular/00-governance/security-cross-layer-gap-report.md) | Canonical: estado segurança |
| [`ai-and-agents-structural-audit.md`](11-review-modular/00-governance/ai-and-agents-structural-audit.md) | Canonical: estado IA |
| [`documentation-and-onboarding-audit.md`](11-review-modular/00-governance/documentation-and-onboarding-audit.md) | Canonical: estado documentação |
| [`module-consolidation-report.md`](11-review-modular/00-governance/module-consolidation-report.md) | Consolidação modular |

> ⚠️ Os 73 relatórios de detalhe individuais foram arquivados em `docs/archive/review-modular-governance-detail/`.

### Módulos 01–13 (por bounded context)
| Módulo | Caminho |
|--------|---------|
| 01 Identity Access | [`01-identity-access/`](11-review-modular/01-identity-access/) |
| 02 Environment Management | [`02-environment-management/`](11-review-modular/02-environment-management/) |
| 03 Catalog | [`03-catalog/`](11-review-modular/03-catalog/) |
| 04 Contracts | [`04-contracts/`](11-review-modular/04-contracts/) |
| 05 Change Governance | [`05-change-governance/`](11-review-modular/05-change-governance/) |
| 06 Operational Intelligence | [`06-operational-intelligence/`](11-review-modular/06-operational-intelligence/) |
| 07 AI Knowledge | [`07-ai-knowledge/`](11-review-modular/07-ai-knowledge/) |
| 08 Governance | [`08-governance/`](11-review-modular/08-governance/) |
| 09 Configuration | [`09-configuration/`](11-review-modular/09-configuration/) |
| 10 Audit Compliance | [`10-audit-compliance/`](11-review-modular/10-audit-compliance/) |
| 11 Notifications | [`11-notifications/`](11-review-modular/11-notifications/) |
| 12 Integrations | [`12-integrations/`](11-review-modular/12-integrations/) |
| 13 Product Analytics | [`13-product-analytics/`](11-review-modular/13-product-analytics/) |

---

## 4. Auditorias Activas (docs/audits/)

| Directório | Descrição |
|-----------|-----------|
| [`audits/2026-03-25/`](audits/2026-03-25/) | Auditoria de estado de Março 2026 — fonte de verdade para estado actual |

**Documentos chave da auditoria 2026-03-25:**
- `documentation-state-report.md` — Estado da documentação
- `final-project-state-assessment.md` — Avaliação final do projecto
- `licensing-selfhosted-readiness-report.md` — Readiness relatório (histórico)
- `prioritized-remediation-roadmap.md` — Roadmap de remediação priorizado
- `remove-archive-consolidate-report.md` — Recomendações de arquivo/consolidação

---

## 5. Observabilidade (docs/observability/)

Documentação da stack de observabilidade activa do NexTraceOne.
Ver [`observability/`](observability/) para providers, collectors e configuração.

**Stack activa:**
- ClickHouse (provider analítico)
- OpenTelemetry Collector (Kubernetes/Docker)
- CLR Profiler (para apps .NET em IIS — observabilidade de apps de cliente, não do NexTraceOne)

---

## 6. Documentação Arquivada (docs/archive/)

| Directório | Conteúdo | Motivo |
|-----------|---------|--------|
| [`archive/architecture-phases/`](archive/architecture-phases/) | Phases 0–9 | Fases de evolução concluídas |
| [`archive/legacy-seeds/`](archive/legacy-seeds/) | Scripts SQL legados | Prefixos de tabela incorrectos |
| [`archive/ai-audits/`](archive/ai-audits/) | Auditoria AI de Março 2026 | Substituída por estado mais recente |
| [`archive/review-modular-governance-detail/`](archive/review-modular-governance-detail/) | 73 relatórios de detalhe | Consolidados em 12 docs canónicos |

> ⚠️ **Documentação arquivada não deve ser usada como referência operacional.**

---

## 7. Tecnologias Removidas / Não Usadas

As seguintes tecnologias foram consideradas e explicitamente **não adoptadas**:

| Tecnologia | Estado | Alternativa Adoptada |
|-----------|--------|---------------------|
| Redis | ❌ Não usado | PostgreSQL (sem cache distribuído no MVP) |
| OpenSearch | ❌ Não usado | PostgreSQL Full-Text Search |
| Temporal (workflow engine) | ❌ Não usado | Quartz.NET + PostgreSQL |
| Grafana/Loki/Tempo | ❌ Removido do scope | ClickHouse como store analítico |
| Licensing/Entitlements | ❌ Removido do scope (P12.1) | — |
| Self-Hosted Enterprise | ❌ Removido do scope (P12.2) | — |

> Referências a estas tecnologias em documentação arquivada são históricas e não devem ser confundidas com a stack actual.

---

*Última consolidação: P12.3 (2026-03-27)*
