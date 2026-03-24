# Inventário de Ficheiros Markdown — NexTraceOne

> **Data:** 2026-03-24  
> **Tipo:** Auditoria Estrutural — Parte 1  
> **Fonte de verdade:** Código do repositório

---

## Resumo

| Métrica | Valor |
|---------|-------|
| Total de ficheiros `.md` | ~220+ |
| Diretórios com `.md` | 20+ |
| Idioma predominante | Inglês (~55%), Português (~40%), Misto (~5%) |
| Documentos classificados como KEEP | ~45 |
| Documentos classificados como KEEP_WITH_REWRITE | ~30 |
| Documentos classificados como MERGE | ~25 |
| Documentos classificados como ARCHIVE | ~80 |
| Documentos classificados como DELETE_CANDIDATE | ~15 |
| Documentos classificados como UNKNOWN | ~5 |

---

## Classificação por Categoria

### Legenda

| Categoria | Significado |
|-----------|-------------|
| **KEEP** | Documento atual, útil e alinhado com o produto |
| **KEEP_WITH_REWRITE** | Útil mas necessita atualização significativa |
| **MERGE** | Conteúdo duplicado ou fragmentado que deve ser consolidado |
| **ARCHIVE** | Valor histórico mas não representa o estado atual |
| **DELETE_CANDIDATE** | Sem valor evidente; candidato a exclusão após validação |
| **UNKNOWN** | Não foi possível classificar sem análise mais profunda |

---

## 1. Raiz do Repositório

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `build/README.md` | KEEP | Documentação do sistema de build |
| `src/frontend/README.md` | KEEP_WITH_REWRITE | Precisa refletir estado atual do frontend (React 19, 105 páginas, 13 features) |
| `src/frontend/ARCHITECTURE.md` | KEEP_WITH_REWRITE | Princípios válidos mas superficial (16 linhas) |
| `src/frontend/src/shared/design-system/README.md` | KEEP | Referência do design system |
| `tests/load/README.md` | KEEP | Documentação de testes de carga |

---

## 2. docs/ — Documentação Raiz (45 ficheiros)

### 2.1 Visão e Escopo do Produto

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `PRODUCT-VISION.md` | **KEEP** | PT | Visão clara e atual do produto como Source of Truth |
| `PRODUCT-SCOPE.md` | **KEEP** | PT | Escopo detalhado com status por feature e contagens de testes |
| `PLATFORM-CAPABILITIES.md` | KEEP_WITH_REWRITE | EN | Capacidades válidas mas superficial |
| `SERVICE-CONTRACT-GOVERNANCE.md` | **KEEP** | EN | Pilar central do produto — regras de governança de contratos |
| `SOURCE-OF-TRUTH-STRATEGY.md` | **KEEP** | EN | Estratégia fundamental do produto |
| `CHANGE-CONFIDENCE.md` | **KEEP** | EN | Pilar central — rastreio de mudanças e confiança |
| `CONTRACT-STUDIO-VISION.md` | **KEEP** | PT | Visão do Contract Studio com modos IA e manual |

### 2.2 Arquitetura e Técnica

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `ARCHITECTURE-OVERVIEW.md` | KEEP_WITH_REWRITE | EN | Apenas 19 linhas; precisa expandir para refletir os 9 módulos, 71 projetos e 16+ DbContexts reais |
| `FRONTEND-ARCHITECTURE.md` | KEEP_WITH_REWRITE | EN | Apenas 16 linhas; precisa refletir as 105 páginas, 13 features e padrões de hooks |
| `BACKEND-MODULE-GUIDELINES.md` | **KEEP** | EN | Guidelines DDD + CQRS válidas e aplicadas |
| `DATA-ARCHITECTURE.md` | KEEP_WITH_REWRITE | EN | Superficial; não reflete os 16+ DbContexts e a arquitetura multi-database real |
| `DOMAIN-BOUNDARIES.md` | **KEEP** | EN | Fronteiras de domínio alinhadas com o código |
| `DEPLOYMENT-ARCHITECTURE.md` | **KEEP** | PT | Arquitetura de deployment detalhada e atual |
| `ENVIRONMENT-VARIABLES.md` | **KEEP** | PT | Referência de variáveis de ambiente atual |
| `INTEGRATIONS-ARCHITECTURE.md` | KEEP_WITH_REWRITE | EN | Superficial; precisa refletir implementação real |

### 2.3 Design e UX

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `DESIGN-SYSTEM.md` | **KEEP** | PT | Tokens de cor detalhados e referência visual |
| `DESIGN.md` | MERGE | PT | Sobrepõe-se com DESIGN-SYSTEM.md e GUIDELINE.md |
| `GUIDELINE.md` | MERGE | PT | Sobrepõe-se com DESIGN-SYSTEM.md e DESIGN.md |
| `UX-PRINCIPLES.md` | **KEEP** | PT | Princípios UX distintos e válidos |
| `PERSONA-MATRIX.md` | MERGE | EN | Superficial; sobrepõe-se com PERSONA-UX-MAPPING.md |
| `PERSONA-UX-MAPPING.md` | MERGE | PT | Mais detalhado que PERSONA-MATRIX.md; consolidar em um |
| `I18N-STRATEGY.md` | KEEP_WITH_REWRITE | EN | Apenas 6 pontos; precisa refletir os 4 locales (~639 KB) reais |

### 2.4 Segurança

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `SECURITY.md` | MERGE | PT | Sobrepõe-se parcialmente com SECURITY-ARCHITECTURE.md |
| `SECURITY-ARCHITECTURE.md` | MERGE | PT | Mais detalhado mas sobrepõe SECURITY.md; consolidar |

### 2.5 Observabilidade e IA

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `OBSERVABILITY-STRATEGY.md` | **KEEP** | EN | Estratégia de observabilidade atual |
| `AI-ARCHITECTURE.md` | **KEEP** | EN | Arquitetura de IA multicamada atual |
| `AI-ASSISTED-OPERATIONS.md` | **KEEP** | EN | Definições de IA operacional válidas |
| `AI-DEVELOPER-EXPERIENCE.md` | **KEEP** | EN | Features de IA para developers |
| `AI-GOVERNANCE.md` | **KEEP** | EN | Framework de controle de IA |
| `AI-LOCAL-IMPLEMENTATION-AUDIT.md` | ARCHIVE | PT/EN | Explicitamente marcado como referência histórica; stack migrada |

### 2.6 Roadmap e Planeamento

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `ROADMAP.md` | **KEEP** | PT | Direção estratégica atual com waves e prioridades |
| `REBASELINE.md` | **KEEP** | PT | Inventário real detalhado do estado pós-PR16 |
| `PRODUCT-REFOUNDATION-PLAN.md` | MERGE | EN/PT | Redirecionamento para ROADMAP.md e REBASELINE.md — pode ser consolidado |
| `POST-PR16-EVOLUTION-ROADMAP.md` | MERGE | EN | Sobrepõe-se significativamente com ROADMAP.md |
| `EXECUTION-BASELINE-PR1-PR16.md` | ARCHIVE | EN | Baseline histórico de PRs 1-16 |
| `IMPLEMENTATION-STATUS.md` | **KEEP** | EN | Taxonomia oficial de maturidade com dados concretos |
| `SOLUTION-GAP-ANALYSIS.md` | **KEEP** | PT | Análise detalhada de gaps com 50+ itens |
| `MODULES-AND-PAGES.md` | KEEP_WITH_REWRITE | EN | Lista superficial; precisa refletir as 105 páginas reais |
| `GO-NO-GO-GATES.md` | **KEEP** | PT | Framework de gates ativo |
| `CORE-FLOW-GAPS.md` | **KEEP** | PT | Gaps prioritários em fluxos core |

### 2.7 Documentos em Português (Avaliação/Plano)

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `NexTraceOne_Avaliacao_Atual_e_Plano_de_Testes.md` | KEEP_WITH_REWRITE | PT | Framework de avaliação e testes útil |
| `NexTraceOne_Plano_Operacional_Finalizacao.md` | **KEEP** | PT | Plano operacional de 10 fases ativo |
| `ANALISE-CRITICA-ARQUITETURAL.md` | **KEEP** | PT | Revisão arquitetural profunda com scores (9/10 arquitetura, 7/10 frontend, 5/10 produção) |

### 2.8 Validação Wave 1

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `WAVE-1-CONSOLIDATED-VALIDATION.md` | ARCHIVE | EN | Validação consolidada histórica (valor referencial) |
| `WAVE-1-VALIDATION-TRACKER.md` | ARCHIVE | EN | Tracker histórico |

---

## 3. docs/acceptance/ (5 ficheiros)

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `NexTraceOne_Baseline_Estavel.md` | KEEP_WITH_REWRITE | PT | Checklist de baseline — precisa atualização |
| `NexTraceOne_Checklist_Entrada_Aceite.md` | **KEEP** | PT | Checklist de entrada para aceitação |
| `NexTraceOne_Escopo_Homologavel.md` | **KEEP** | PT | Escopo homologável definido |
| `NexTraceOne_Plano_Teste_Funcional.md` | **KEEP** | PT | Plano de testes funcional |
| `NexTraceOne_Relatorio_Teste_Aceitacao.md` | KEEP_WITH_REWRITE | PT | Relatório que precisa atualização contínua |

---

## 4. docs/aiknowledge/ (3 ficheiros)

| Ficheiro | Classificação | Idioma | Justificativa |
|----------|--------------|--------|---------------|
| `AIK_EXTERNAL_AI_FLOW.md` | **KEEP** | EN | Fluxo de IA externa — alinhado com módulo aiknowledge |
| `AIK_ORCHESTRATION_DESIGN.md` | **KEEP** | EN | Design de orquestração IA |
| `PHASE-2-AIKNOWLEDGE-COMPLETION.md` | ARCHIVE | EN | Relatório de fase 2 — histórico |

---

## 5. docs/architecture/ (~70 ficheiros)

### 5.1 ADRs

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `ADR-001-database-strategy.md` | **KEEP** | Decisão arquitetural fundamental |
| `ADR-002-migration-policy.md` | **KEEP** | Política de migrações ativa |
| `ADR-003-event-bus-limitations.md` | **KEEP** | Limitação documentada (InProcessEventBus) |
| `ADR-004-simulated-data-policy.md` | **KEEP** | Política de dados simulados |
| `ADR-005-ai-runtime-foundation.md` | **KEEP** | Fundação AI runtime |
| `ADR-006-agent-runtime-foundation.md` | **KEEP** | Fundação agent runtime |
| `adr/ADR-001-database-consolidation-plan.md` | MERGE | Sobrepõe ADR-001-database-strategy.md |
| `adr/ADR-002-event-bus-in-process-limitation.md` | MERGE | Sobrepõe ADR-003-event-bus-limitations.md |
| `adr/ADR-002-migration-policy.md` | DELETE_CANDIDATE | Duplicata exata de ADR-002-migration-policy.md |

### 5.2 Ambientes

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `environments/environment-management-design.md` | **KEEP** | Design de gestão de ambientes |
| `environments/environment-control-audit.md` | KEEP_WITH_REWRITE | Auditoria de controle |
| `environments/environment-control-transition-notes.md` | ARCHIVE | Notas de transição históricas |
| `environments/environment-production-designation.md` | **KEEP** | Designação de produção |
| `environments/non-prod-to-prod-risk-analysis.md` | **KEEP** | Análise de risco relevante |

### 5.3 Fases (phase-0 a phase-9)

| Diretório | Ficheiros | Classificação | Justificativa |
|-----------|-----------|--------------|---------------|
| `phase-0/` | 7 | ARCHIVE | Fase 0 concluída — valor histórico |
| `phase-1/` | 3 | ARCHIVE | Fase 1 concluída |
| `phase-2/` | 4 | ARCHIVE | Fase 2 concluída |
| `phase-4/` | 4 | ARCHIVE | Fase 4 concluída |
| `phase-4-agents/` | 1 | ARCHIVE | Subprojeto de agentes |
| `phase-5/` | 5 | ARCHIVE | Fase 5 concluída |
| `phase-6/` | 4 | ARCHIVE | Fase 6 concluída |
| `phase-7/` | 5 | ARCHIVE | Fase 7 concluída |
| `phase-8/` | 4 | ARCHIVE | Fase 8 concluída |
| `phase-9/` | 5 | KEEP_WITH_REWRITE | Fase mais recente — auditoria de conformidade |

---

## 6. docs/assessment/ (12 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `00-EXECUTIVE-SUMMARY.md` | **KEEP** | Resumo executivo detalhado com métricas reais (62% completude, 1709 testes backend, 96 páginas) |
| `01-SOLUTION-INVENTORY.md` | **KEEP** | Inventário de solução abrangente |
| `02-FUNCTIONAL-MODULE-MAP.md` | **KEEP** | Mapa funcional dos módulos |
| `03-COMPLETENESS-MATRIX.md` | **KEEP** | Matriz de completude com evidências |
| `04-HIDDEN-REMOVED-INCOMPLETE-FEATURES.md` | **KEEP** | Features ocultas/removidas/incompletas |
| `05-BACKEND-AUDIT.md` | **KEEP** | Auditoria backend |
| `06-FRONTEND-AUDIT.md` | **KEEP** | Auditoria frontend |
| `07-DATA-MIGRATIONS-TENANCY-AUDIT.md` | **KEEP** | Migrações e multi-tenancy |
| `08-SECURITY-AUDIT.md` | **KEEP** | Auditoria de segurança |
| `09-OBSERVABILITY-AND-AI-READINESS.md` | **KEEP** | Prontidão observabilidade e IA |
| `10-PRODUCTION-READINESS.md` | **KEEP** | Prontidão para produção |
| `11-GAP-BACKLOG-PRIORITIZED.md` | **KEEP** | Backlog priorizado de gaps |
| `12-RECOMMENDED-EXECUTION-PLAN.md` | **KEEP** | Plano de execução recomendado |

> **Nota:** O diretório assessment/ é um dos mais valiosos — análise estruturada e com evidências reais.

---

## 7. docs/audits/ (~34 ficheiros)

| Categoria | Ficheiros | Classificação | Justificativa |
|-----------|-----------|--------------|---------------|
| Relatórios Phase 0 | ~10 | ARCHIVE | Fases iniciais concluídas |
| Relatórios Phase 1 | ~6 | ARCHIVE | Segurança e integridade — concluído |
| Relatórios Phase 2-4 | ~5 | ARCHIVE | IA, reliability e governança — concluído |
| Relatórios Phase 5-7 | ~5 | KEEP_WITH_REWRITE | Fases mais recentes — referência ativa |
| Phase 8-9 / Go-Live | ~4 | **KEEP** | Decisões de go-live e conformidade final |
| Wave Reports | ~5 | ARCHIVE | Reports de wave concluídos |
| `CONFIGURATION-PHASE-*-REPORT.md` (1-8) | 8 | ARCHIVE | Relatórios de configuração por fase |

---

## 8. docs/execution/ (~60 ficheiros)

| Categoria | Ficheiros | Classificação | Justificativa |
|-----------|-----------|--------------|---------------|
| CONFIGURATION-* (settings) | ~35 | **KEEP** | Documentação de configuração ativa para ~345 definições |
| NOTIFICATIONS-* | ~12 | **KEEP** | Framework de notificações ativo |
| PHASE-* execution plans | ~8 | ARCHIVE | Planos de execução históricos |
| WAVE-* plans | ~5 | ARCHIVE | Planos de wave históricos |

---

## 9. docs/frontend/ (3 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `AUDIT-REPORT.md` | KEEP_WITH_REWRITE | Auditoria de junho 2025 — 9 meses desatualizada; métricas mudaram (233→105+ páginas) |
| `REFACTORING-PLAN.md` | KEEP_WITH_REWRITE | Plano de refactoring — precisa revisão |
| `TECHNICAL-INVENTORY.md` | KEEP_WITH_REWRITE | Inventário técnico de componentes — precisa revisão |

---

## 10. docs/governance/ (1 ficheiro)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `PHASE-5-GOVERNANCE-ENRICHMENT.md` | ARCHIVE | Enriquecimento de fase 5 — histórico |

---

## 11. docs/observability/ (~12 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `architecture-overview.md` | **KEEP** | Visão geral de observabilidade |
| `README.md` | KEEP_WITH_REWRITE | Precisa atualização |
| `DRIFT-DETECTION-PIPELINE.md` | **KEEP** | Pipeline de deteção de drift |
| `ENVIRONMENT-COMPARISON-ARCHITECTURE.md` | **KEEP** | Comparação de ambientes |
| `INGESTION-API-ROLE-AND-FLOW.md` | **KEEP** | Papel da API de ingestão |
| `PHASE-6-OBSERVABILITY-COMPLETION.md` | ARCHIVE | Relatório de fase |
| `collection/` (3 ficheiros) | KEEP_WITH_REWRITE | Guias de coleção de dados |
| `configuration/` (1 ficheiro) | **KEEP** | Configuração de observabilidade |
| `providers/` (2 ficheiros) | **KEEP** | ClickHouse e Elastic |
| `troubleshooting.md` | **KEEP** | Guia de troubleshooting |

---

## 12. docs/quality/ (5 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `CONTRACT-TEST-BOUNDARIES.md` | **KEEP** | Fronteiras de teste de contratos |
| `E2E-GO-LIVE-SUITE.md` | **KEEP** | Suite E2E para go-live |
| `PERFORMANCE-AND-RESILIENCE-BASELINE.md` | **KEEP** | Baseline de performance |
| `PHASE-8-VALIDATION-MATRIX.md` | ARCHIVE | Matriz de validação histórica |
| `TEST-STRATEGY-AND-LAYERS.md` | **KEEP** | Estratégia de testes por camadas |

---

## 13. docs/reliability/ (4 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `RELIABILITY-DATA-MODEL.md` | **KEEP** | Modelo de dados de fiabilidade |
| `RELIABILITY-SCORING-MODEL.md` | **KEEP** | Modelo de scoring |
| `RELIABILITY-FRONTEND-INTEGRATION.md` | **KEEP** | Integração frontend |
| `PHASE-3-RELIABILITY-COMPLETION.md` | ARCHIVE | Relatório de fase |

---

## 14. docs/release/ (9 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `NexTraceOne_Final_Production_Scope.md` | **KEEP** | Escopo de produção final |
| `NexTraceOne_Release_Gate_Final.md` | **KEEP** | Gate de release final |
| `NexTraceOne_ZR1` a `ZR6` | **KEEP** | Gates "Zero Ressalvas" ativos |
| `NexTraceOne_Zero_Ressalvas_Backlog.md` | **KEEP** | Backlog de zero ressalvas |

---

## 15. docs/reviews/ (3 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `ANALISE-CRITICA-ARQUITETURAL-2026-03.md` | MERGE | Sobrepõe ANALISE-CRITICA-ARQUITETURAL.md na raiz |
| `NexTraceOne_Full_Production_Convergence_Report.md` | **KEEP** | Relatório de convergência |
| `NexTraceOne_Production_Readiness_Review.md` | **KEEP** | Revisão de prontidão |

---

## 16. docs/runbooks/ (12 ficheiros)

Todos classificados como **KEEP** — runbooks operacionais ativos e essenciais.

---

## 17. docs/security/ (9 ficheiros)

| Ficheiro | Classificação | Justificativa |
|----------|--------------|---------------|
| `BACKEND-ENDPOINT-AUTH-AUDIT.md` | **KEEP** | Auditoria de autenticação |
| `PHASE-1-*` (3 ficheiros) | ARCHIVE | Baselines de fase 1 |
| `application-hardening-checklist.md` | **KEEP** | Checklist de hardening |
| `application-onprem-hardening-notes.md` | **KEEP** | Notas on-prem |
| `application-privacy-lgpd-gdpr-notes.md` | **KEEP** | Notas LGPD/GDPR |
| `application-security-review.md` | **KEEP** | Revisão de segurança |
| `security-backend-infra-integration-notes.md` | **KEEP** | Notas de integração |

---

## 18. docs/user-guide/ (8 ficheiros)

Todos classificados como **KEEP** — guias de utilizador essenciais para onboarding.

---

## 19. docs/planos/, docs/rebaseline/, docs/telemetry/, docs/testing/

| Diretório | Ficheiros | Classificação | Justificativa |
|-----------|-----------|--------------|---------------|
| `planos/` | 1 | **KEEP** | Plano de evolução fase 10 |
| `rebaseline/` | 2 | KEEP_WITH_REWRITE | Rebaseline arquitetural e persistência |
| `telemetry/` | 1 | **KEEP** | Arquitetura de telemetria |
| `testing/` | 1 | KEEP_WITH_REWRITE | Matriz de realidade |

---

## Problemas Críticos Identificados

### Documentos Duplicados ou Sobrepostos

1. **DESIGN.md + DESIGN-SYSTEM.md + GUIDELINE.md** → Consolidar em um documento unificado
2. **SECURITY.md + SECURITY-ARCHITECTURE.md** → Consolidar em um documento
3. **PERSONA-MATRIX.md + PERSONA-UX-MAPPING.md** → Consolidar em um documento
4. **POST-PR16-EVOLUTION-ROADMAP.md + ROADMAP.md + PRODUCT-REFOUNDATION-PLAN.md** → ROADMAP.md como fonte principal
5. **ANALISE-CRITICA-ARQUITETURAL.md (raiz) + reviews/ANALISE-CRITICA-ARQUITETURAL-2026-03.md** → Duplicata
6. **ADRs duplicados** entre `/architecture/` e `/architecture/adr/`

### Documentos Superficiais para Áreas Críticas

1. **ARCHITECTURE-OVERVIEW.md** — 19 linhas para 71 projetos C#
2. **FRONTEND-ARCHITECTURE.md** — 16 linhas para 105 páginas
3. **DATA-ARCHITECTURE.md** — Não reflete 16+ DbContexts reais
4. **I18N-STRATEGY.md** — 6 pontos para 4 locales com ~639 KB
5. **MODULES-AND-PAGES.md** — Lista superficial de ~30 itens quando existem 105+ páginas

### Documentação Ausente

1. Sem documentação dedicada para o módulo **Notifications**
2. Sem documentação dedicada para o módulo **Configuration** (apesar de ter 345+ definições e 251 testes)
3. Sem documentação dedicada para **Integrations Hub**
4. Sem documentação dedicada para **Product Analytics**
5. Sem documentação de **API Reference** por endpoint

---

## Recomendações Prioritárias

1. **Consolidar** documentos duplicados (5 pares identificados)
2. **Expandir** documentos superficiais de áreas críticas (5 identificados)
3. **Criar** documentação ausente para módulos sem docs (4 módulos)
4. **Arquivar** fase-relatórios concluídos (phases 0-8) numa pasta `archive/`
5. **Manter** o diretório `assessment/` como referência de alta qualidade
6. **Manter** `runbooks/`, `user-guide/`, `release/` como documentação operacional ativa
