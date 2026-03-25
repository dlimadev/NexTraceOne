# E6 — Change Governance Post-Execution Gap Report

## Data
2026-03-25

## Resumo

Este relatório documenta o que foi resolvido, o que ficou pendente,
e o que depende de outras fases após a execução do E6 para o módulo Change Governance.

---

## ✅ Resolvido Nesta Fase

| Item | Categoria | Estado |
|------|-----------|--------|
| RowVersion (xmin) em Release, ChangeIntelligenceScore, BlastRadiusReport | Domain + Persistence | ✅ Concluído |
| RowVersion (xmin) em WorkflowInstance, PromotionRequest, Ruleset | Domain + Persistence | ✅ Concluído |
| 27 tabelas + 4 outbox → `chg_` prefix unificado | Persistence | ✅ Concluído |
| 5 check constraints (Release Status/ChangeLevel/ChangeScore, WorkflowInstance Status, PromotionRequest Status) | Persistence | ✅ Concluído |
| Named indexes updated (ix_ci_ → ix_chg_) | Persistence | ✅ Concluído |
| IsRowVersion() em 6 aggregates | Persistence | ✅ Concluído |
| README do módulo | Documentação | ✅ Concluído |
| Build: 0 erros | Validação | ✅ |
| Testes: 198/198 Change Governance | Validação | ✅ |

---

## ⏳ Pendente — Depende de Outras Fases

| Item | Categoria | Bloqueador | Fase Esperada | Esforço |
|------|-----------|------------|---------------|---------|
| Gerar baseline migration (InitialCreate) | Persistência | Requer prefix decision final | Wave 3 | 1 sprint |
| DbUpdateConcurrencyException handling in write handlers → 409 | Backend (FC-02) | Handler updates | E6+ | 2h |
| Domain events for Release lifecycle transitions | Backend (FC-05) | New event types | E6+ | 4h |
| Domain events for WorkflowInstance state changes | Backend (FC-06) | New event types | E6+ | 2h |
| Domain events for PromotionRequest decisions | Backend (FC-07) | New event types | E6+ | 2h |
| Incident correlation for PostReleaseReview | Backend (FC-10) | OpIntel integration | E6+ | 4h |
| Score algorithm enrichment (more factors) | Score (FC-03) | Product decision | E6+ | 8h |
| Blast radius with real Catalog graph queries | BlastRadius (FC-04) | Catalog API | E6+ | 8h |
| Filtered indexes WHERE is_deleted = false | Persistence (FC-08) | Config update | E6+ | 1h |
| RowVersion on remaining entities (ChangeEvent, FreezeWindow, etc.) | Persistence (FC-09) | Lower priority | E6+ | 2h |
| FK constraints Release→Score, Release→BlastRadius | Persistence (FC-11) | Config update | E6+ | 1h |
| i18n completeness (pt-BR, es) | Frontend (FC-12) | Translation | E6+ | 2h |
| Consolidate 4 DbContexts → 1 ChangeGovernanceDbContext | Architecture (SA-01) | LOW priority | Future | 8h |
| DeploymentEnvironment deduplication vs Environment Management | Architecture (SA-02) | OI-04 | Future | 4h |

---

## 🚫 Não Bloqueia Evolução

Todos os itens pendentes são incrementais e **não bloqueiam** a evolução para:

1. **E7+** — Próximos módulos da trilha E
2. **Wave 3** — Baseline generation (ChangeGov+Notifications+OpIntel)
3. **Próximas releases** do produto

---

## 📊 Métricas de Maturidade

| Dimensão | Antes do E6 | Após E6 | Target |
|----------|-------------|---------|--------|
| Backend | 65% | 68% | 90% |
| Frontend | 55% | 57% | 85% |
| Persistência | 45% | 78% | 100% |
| Segurança | 70% | 73% | 90% |
| Documentação | 25% | 60% | 85% |
| Domínio | 70% | 80% | 95% |
| Score/BlastRadius | 60% | 68% | 90% |
| **Global** | **56%** | **69%** | **91%** |

A maturidade global subiu 13 pontos percentuais (56% → 69%), com os maiores ganhos na
persistência (45% → 78%) pela unificação de prefixo e concorrência otimista,
e documentação (25% → 60%) pelo README.

---

## Decisões Tomadas Durante E6

1. **`chg_` como prefixo unificado**: Consolidou os 4 prefixos subdomain-specific
   (`ci_`, `wf_`, `prm_`, `rg_`) num único `chg_`. Isto simplifica o modelo mental,
   reduz ambiguidade entre módulos e prepara para a baseline (Wave 3). Outbox tables
   mantêm sufixo subdomain para unicidade (`chg_wf_outbox_messages`, etc.).

2. **xmin via IsRowVersion()**: Consistente com o padrão E1 (Configuration), E2 (Contracts),
   E3 (Environment), E4 (Governance), E5 (Catalog). Aplicado nos 6 aggregates mais
   importantes. Entidades menores (ChangeEvent, FreezeWindow) ficam para E6+.

3. **Check constraints em 3 tabelas**: Release (Status, ChangeLevel, ChangeScore),
   WorkflowInstance (Status), PromotionRequest (Status). Estes são os 3 aggregates
   com ciclos de vida mais complexos. Outros enums ficam para E6+.

4. **DbContext consolidation adiada**: A consolidação de 4 DbContexts num único
   ChangeGovernanceDbContext (SA-01) é LOW priority porque a separação actual funciona
   correctamente. Consolidação seria melhor feita junto com a baseline (Wave 3).

5. **DeploymentEnvironment deduplicação adiada**: A resolução da duplicação entre
   DeploymentEnvironment (Promotion) e Environment (Environment Management) depende
   da extração OI-04 que é pré-requisito para Wave 3.
