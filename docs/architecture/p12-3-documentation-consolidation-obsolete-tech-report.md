# P12.3 — Documentation Consolidation & Obsolete Tech Cleanup Report

> Data: 2026-03-27 | Fase: P12.3 — Consolidação de documentação contraditória e tecnologias obsoletas

---

## 1. Objectivo

Consolidar e limpar o fluxo activo de documentação do NexTraceOne, arquivando documentação de fases antigas, resolvendo contradições críticas (especialmente na área de IA), reduzindo a fragmentação de docs/11-review-modular/00-governance/, e estabelecendo um conjunto canónico mínimo de documentos por tema.

Esta fase sucede P12.1 (Licensing) e P12.2 (Self-Hosted Enterprise) e trata do problema transversal de documentação contraditória e fragmentada.

---

## 2. Inventário de Acções Executadas

### 2.1 Directórios Criados

| Caminho | Finalidade |
|---------|-----------|
| `docs/archive/` | Raiz do arquivo histórico |
| `docs/archive/architecture-phases/` | Fases de evolução arquivadas |
| `docs/archive/legacy-seeds/` | Seeds SQL legados |
| `docs/archive/ai-audits/` | Auditorias de IA históricas |
| `docs/archive/review-modular-governance-detail/` | Relatórios de detalhe do 00-governance |

---

### 2.2 Documentação de Fases Antigas Arquivada

**Acção:** `git mv` para `docs/archive/architecture-phases/`

| Directório Arquivado | Nº Ficheiros | Conteúdo |
|---------------------|-------------|---------|
| `phase-0/` | 7 | Refactoring tenant-environment-context (ADR-001, ADR-002 + impact maps) |
| `phase-1/` | 3 | Domain foundation |
| `phase-2/` | 4 | AI context + authorization foundation |
| `phase-4/` | 4 | Backend modular refactor |
| `phase-4-agents/` | 1 | AI agents contextual integration |
| `phase-5/` | 5 | Distributed context + telemetry foundation |
| `phase-6/` | 4 | Frontend context architecture |
| `phase-7/` | 5 | AI sources, tools & governance |
| `phase-8/` | 4 | Test, hardening & rollout planning (inclui referência a Redis como checklist, agora arquivada) |
| `phase-9/` | 5 | Final consolidation audit |
| **TOTAL** | **42 ficheiros** | |

**Justificativa:** Todas as fases representam decisões e planos de evolução já concluídos. Não têm valor operacional mas têm valor histórico. Mantidos em arquivo com README explicativo.

---

### 2.3 Legacy Seeds Arquivados

**Acção:** `git mv docs/architecture/legacy-seeds → docs/archive/legacy-seeds/architecture-legacy-seeds/`

| Ficheiro | Problema |
|---------|---------|
| `seed-identity.sql` | Prefixo `identity_*` incorrecto (deve ser `iam_*`) |
| `seed-catalog.sql` | Prefixos `eg_*`, `ct_*` incorrectos |
| `seed-incidents.sql` | Prefixo `oi_*` incorrecto (deve ser `ops_inc_*`) |
| `seed-audit.sql` | Prefixos antigos |
| `seed-governance.sql` | Prefixos antigos |
| `seed-aiknowledge.sql` | Prefixos antigos |
| `seed-changegovernance.sql` | Prefixos antigos |

**Justificativa:** Se executados, falhariam ou inseririam dados em tabelas inexistentes. Risco operacional alto de manter no fluxo activo.

---

### 2.4 Auditoria de IA Contraditória Arquivada

**Acção:** `git mv docs/AI-LOCAL-IMPLEMENTATION-AUDIT.md → docs/archive/ai-audits/AI-LOCAL-IMPLEMENTATION-AUDIT-2026-03-17.md`

**Contradições resolvidas:**

| Afirmação do Documento Arquivado | Realidade Actual |
|----------------------------------|-----------------|
| "Migrations de BD para IA: 0% (DbContexts não registados)" | ✅ Todos os DbContexts AI registados em `WebApplicationExtensions.cs` (corrigido em P8+) |
| "ZERO dependências SDK de IA" | ⚠️ Parcialmente impreciso — existem `OllamaHttpClient` e `OpenAiHttpClient` (não são SDKs oficiais mas são clientes customizados) |
| "Maturidade global: 20-25%" | ⚠️ Desactualizado — maturidade do Governance (vertical principal) é >75%; streaming real implementado em P9.3; grounding em P9.5 |
| "Frontend: dados 100% mock" | ⚠️ Parcialmente desactualizado — AI Assistant está conectado ao backend real |

**Nota:** O documento tinha uma nota de "relatório histórico" no topo mas permanecia como ficheiro de raiz, criando confusão. Foi movido para arquivo.

---

### 2.5 Consolidação docs/11-review-modular/00-governance/

**Estado antes:** 85 ficheiros individuais de auditoria

**Estado depois:** 12 ficheiros canónicos (activos) + 73 arquivados

#### 12 Documentos Canónicos Mantidos

| Documento | Tema Canónico |
|-----------|--------------|
| `README.md` | Índice do directório |
| `modular-review-summary.md` | **Sumário geral** — ponto de entrada |
| `final-consolidation-and-master-plan.md` | Plano mestre e consolidação final |
| `review-status-overview.md` | Estado geral da revisão modular |
| `product-maturity-summary.md` | Maturidade do produto |
| `backend-structural-audit.md` | Estado canonical do backend |
| `frontend-structural-audit.md` | Estado canonical do frontend |
| `database-structural-audit.md` | Estado canonical da base de dados |
| `security-cross-layer-gap-report.md` | Estado canonical de segurança |
| `ai-and-agents-structural-audit.md` | Estado canonical de IA e agentes |
| `documentation-and-onboarding-audit.md` | Estado canonical da documentação |
| `module-consolidation-report.md` | Consolidação modular |

#### 73 Relatórios de Detalhe Arquivados

Movidos para `docs/archive/review-modular-governance-detail/`:

Exemplos de categorias arquivadas:
- Backend: `backend-application-layer-report.md`, `backend-domain-report.md`, `backend-endpoints-report.md`, `backend-persistence-report.md`, `backend-authorization-report.md`, `backend-security-enforcement-report.md`, `backend-xml-docs-and-comments-report.md`, `backend-observability-audit-and-docs-report.md`, `backend-migrations-and-seeds-report.md`, `backend-module-inventory.md`, `backend-frontend-integration-report.md`
- Frontend: `frontend-api-integration-report.md`, `frontend-i18n-report.md`, `frontend-legibility-and-comments-report.md`, `frontend-menu-and-navigation-report.md`, `frontend-module-inventory.md`, `frontend-pages-and-routes-report.md`, `frontend-permissions-and-guards-report.md`, `frontend-security-alignment-report.md`, `frontend-ux-and-layout-gap-report.md`
- Database: `database-ai-agents-workflow-support-report.md`, `database-frontend-backend-alignment-report.md`, `database-integrity-and-indexing-report.md`, `database-maintainability-report.md`, `database-migrations-report.md`, `database-seeds-report.md`, `database-tenant-environment-audit-report.md`, `dbcontexts-and-persistence-inventory.md`, `domain-vs-schema-alignment-report.md`
- Security: `authentication-audit-report.md`, `authorization-and-permissions-report.md`, `security-audit-traceability-report.md`, `security-identity-and-access-audit.md`, `security-product-alignment-report.md`, `sensitive-actions-and-admin-security-report.md`, `tenant-isolation-report.md`, `environment-control-report.md`, `break-glass-jit-delegated-access-report.md`
- AI: `ai-audit-traceability-and-observability-report.md`, `ai-chat-audit-report.md`, `ai-cross-layer-gap-report.md`, `ai-models-and-providers-report.md`, `ai-permissions-and-capabilities-report.md`, `ai-product-alignment-report.md`, `ai-prompts-context-memory-tools-report.md`, `agents-catalog-and-management-report.md`, `agents-execution-report.md`
- Planeamento/backlog: `prioritized-remediation-backlog.md`, `quick-wins-vs-structural-refactors.md`, `module-closure-plan.md`, `module-inventory-report.md`, `module-priority-matrix.md`, `product-closure-criteria.md`, `execution-waves-plan.md`, `master-plan-risks-and-mitigations.md`
- Outros: `review-methodology.md`, `review-checklist-global.md`, `review-priority-recommendation.md`, `naming-and-code-legibility-report.md`, `root-cause-consolidation-report.md`, `layer-consolidation-report.md`, `markdown-documentation-quality-report.md`, `markdown-inventory-report.md`, `prompt-n1-to-n10-execution-validation.md`, `prompt-n1-to-n10-missing-or-partial-items.md`, `prompt-n1-to-n10-summary-matrix.md`, `developer-onboarding-gap-report.md`, `documentation-improvement-recommendations.md`, `documentation-standard-recommendation.md`, `documentation-vs-code-gap-report.md`, `readme-coverage-report.md`, `menu-structure-report.md`, `repository-structural-audit.md`

---

### 2.6 Referências a Tecnologias Removidas

#### Revisão de Redis, OpenSearch, Temporal

**Resultado da varredura nos documentos activos:**

| Tecnologia | Ficheiros com Referências Activas Problemáticas | Acção |
|-----------|----------------------------------------------|-------|
| Redis | `docs/architecture/p1-4-post-change-gap-report.md` — menciona Redis como "opção para rate limiting futuro, fora do escopo" | ✅ Mantido — framing correcto ("fora do escopo", "fase futura") |
| OpenSearch | Zero referências em documentos activos | ✅ Nenhuma acção necessária |
| Temporal (workflow engine) | Zero referências em documentos activos como tecnologia de stack | ✅ Nenhuma acção necessária |

**Nota sobre "Temporal" como adjectivo:** As referências a "temporal" em docs activos (temporal validity, temporal window, temporal access) são uso do adjectivo português/inglês, não referências ao Temporal.io workflow engine. Não foram modificadas.

**Referências problemáticas arquivadas (via phase-8):**  
`docs/archive/architecture-phases/phase-8/phase-8-rollout-and-fallback-plan.md` continha `"Redis/correlation storage"` como checklist item — agora no arquivo.

#### Tecnologias Confirmadas Como Não Usadas (documentado em DOCUMENTATION-INDEX.md)

| Tecnologia | Estado | Alternativa |
|-----------|--------|-------------|
| Redis | ❌ Não usado | PostgreSQL sem cache distribuído no MVP |
| OpenSearch | ❌ Não usado | PostgreSQL FTS |
| Temporal (workflow engine) | ❌ Não usado | Quartz.NET + PostgreSQL |
| Grafana/Loki/Tempo | ❌ Removidos do scope | ClickHouse |

---

### 2.7 Documento de Navegação Criado

**Ficheiro criado:** `docs/DOCUMENTATION-INDEX.md`

Conteúdo:
- Documentação raiz activa (11 ficheiros)
- ADRs activos (6 decisões)
- Estado actual da arquitectura
- Tabela dos 12 docs canónicos de 00-governance
- Módulos 01–13 de revisão modular
- Auditorias activas (2026-03-25)
- Stack de observabilidade activa
- Tabela de documentação arquivada (4 directórios)
- Tabela de tecnologias removidas/não usadas

---

## 3. Impacto por Área

### Frontend
Sem alterações de código frontend. A consolidação é puramente documental.

### Backend
Sem alterações de código backend. A consolidação é puramente documental.

### Deployment Docs
`docs/architecture/phase-8/` (que continha planos de rollout/fallback) foi arquivado. Os Dockerfiles e `docker-compose.yml` activos não foram alterados.

### Roadmap
Sem novas alterações — ROADMAP.md e PRODUCT-SCOPE.md já foram limpos no P12.2.

---

## 4. Ficheiros Criados

| Ficheiro | Tipo |
|---------|------|
| `docs/archive/README.md` | Índice do arquivo |
| `docs/archive/architecture-phases/README.md` | README do arquivo de fases |
| `docs/archive/legacy-seeds/README.md` | README do arquivo de seeds |
| `docs/archive/ai-audits/README.md` | README do arquivo de auditorias AI |
| `docs/archive/review-modular-governance-detail/README.md` | README do arquivo de detalhes 00-governance |
| `docs/DOCUMENTATION-INDEX.md` | Índice de navegação canónico |
| `docs/architecture/p12-3-documentation-consolidation-obsolete-tech-report.md` | Este relatório |
| `docs/architecture/p12-3-post-change-gap-report.md` | Relatório de gaps |

---

## 5. Sumário de Movimentos

| Categoria | Antes | Depois | Arquivados |
|-----------|-------|--------|-----------|
| Phase dirs (architecture) | 10 dirs / 42 docs | 0 dirs / 0 docs | 42 docs |
| Legacy seeds | 7 SQL ficheiros | 0 | 7 SQL ficheiros |
| AI-LOCAL-IMPLEMENTATION-AUDIT | 1 ficheiro raiz | 0 | 1 ficheiro |
| 00-governance docs | 85 ficheiros | 12 canónicos | 73 detalhes |
| **TOTAL** | **~135 ficheiros** | **~14 canónicos** | **~123 ficheiros** |

---

## 6. Validação de Consistência

- ✅ Nenhum código de runtime foi alterado
- ✅ Todos os movimentos foram feitos via `git mv` (histórico preservado)
- ✅ READMEs de arquivo criados explicando o motivo do arquivo
- ✅ Índice de navegação `docs/DOCUMENTATION-INDEX.md` criado
- ✅ Documentação activa não contradiz estado real conhecido do projecto
- ✅ Fases antigas, auditorias antigas e tecnologias removidas deixam de poluir o fluxo principal
