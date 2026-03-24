# NexTraceOne — Inventário de Ficheiros Markdown

**Data:** 2026-03-24
**Total auditado:** ~357 ficheiros `.md`
**Fonte de verdade:** código e estrutura de ficheiros do repositório

---

## Legenda de Classificação

| Código | Significado |
|--------|-------------|
| **KEEP** | Manter como está — relevante e atualizado |
| **KEEP_WITH_REWRITE** | Manter com reescrita — estrutura válida mas conteúdo desatualizado |
| **MERGE** | Consolidar com outro documento |
| **ARCHIVE** | Mover para pasta de histórico — valor histórico, não operacional |
| **DELETE_CANDIDATE** | Pode ser excluído — duplicado, obsoleto ou sem valor |
| **UNKNOWN** | Requer leitura individual para classificar |

---

## Secção 1 — `docs/` (raiz)

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `AI-ARCHITECTURE.md` | MERGE | Consolidar com outros docs de AI numa pasta `docs/ai-hub/` |
| `AI-ASSISTED-OPERATIONS.md` | MERGE | Idem — parte do conjunto AI Hub |
| `AI-DEVELOPER-EXPERIENCE.md` | MERGE | Idem |
| `AI-GOVERNANCE.md` | MERGE | Idem |
| `AI-LOCAL-IMPLEMENTATION-AUDIT.md` | ARCHIVE | Auditoria pontual de implementação local |
| `ANALISE-CRITICA-ARQUITETURAL.md` | MERGE | Consolidar com SOLUTION-GAP-ANALYSIS e CORE-FLOW-GAPS |
| `ARCHITECTURE-OVERVIEW.md` | KEEP_WITH_REWRITE | Estrutura válida; verificar se reflete arquitetura atual (9 módulos) |
| `BACKEND-MODULE-GUIDELINES.md` | KEEP | Convenções de desenvolvimento — mantém relevância |
| `CHANGE-CONFIDENCE.md` | KEEP_WITH_REWRITE | Conceito central do produto; verificar alinhamento com implementação atual |
| `CONTRACT-STUDIO-VISION.md` | KEEP_WITH_REWRITE | Visão do Contract Studio; verificar vs implementação real |
| `CORE-FLOW-GAPS.md` | MERGE | Consolidar com gap analysis |
| `DATA-ARCHITECTURE.md` | KEEP_WITH_REWRITE | Verificar se DbContexts listados correspondem aos 13 atuais |
| `DEPLOYMENT-ARCHITECTURE.md` | KEEP | Arquitetura de deployment — relevante para operações |
| `DESIGN-SYSTEM.md` | KEEP | Design system — relevante para frontend |
| `DESIGN.md` | KEEP_WITH_REWRITE | Verificar se reflete componentes atuais (Tailwind, Lucide React) |
| `DOMAIN-BOUNDARIES.md` | KEEP | Limites de domínio — fundamental para arquitetura |
| `ENVIRONMENT-VARIABLES.md` | KEEP | Referência operacional essencial |
| `EXECUTION-BASELINE-PR1-PR16.md` | ARCHIVE | Histórico de PRs 1-16 — valor histórico apenas |
| `FRONTEND-ARCHITECTURE.md` | KEEP_WITH_REWRITE | Verificar vs React 19, React Router 7, TanStack Query 5 atuais |
| `GO-NO-GO-GATES.md` | DELETE_CANDIDATE | Gates de lançamento de versão passada |
| `GUIDELINE.md` | KEEP_WITH_REWRITE | Verificar se guidelines são atuais |
| `I18N-STRATEGY.md` | KEEP | Estratégia de i18n — relevante (4 locales ativos) |
| `IMPLEMENTATION-STATUS.md` | KEEP | **Documento mais crítico** — taxonomia de estado de implementação |
| `INTEGRATIONS-ARCHITECTURE.md` | KEEP_WITH_REWRITE | Verificar vs IntegrationHub atual |
| `LOCAL-SETUP.md` | KEEP_WITH_REWRITE | Setup local — verificar se reflete stack atual |
| `MODULES-AND-PAGES.md` | KEEP_WITH_REWRITE | **Extremamente superficial** — 30 linhas para 100+ páginas. Reescrever completamente. |
| `NexTraceOne_Avaliacao_Atual_e_Plano_de_Testes.md` | DELETE_CANDIDATE | Plano de testes de versão anterior |
| `NexTraceOne_Plano_Operacional_Finalizacao.md` | DELETE_CANDIDATE | Plano de finalização executado |
| `OBSERVABILITY-STRATEGY.md` | KEEP_WITH_REWRITE | Verificar vs implementação atual de observabilidade |
| `PERSONA-MATRIX.md` | KEEP_WITH_REWRITE | Personas relevantes para UX; verificar se PersonaContext reflete isto |
| `PERSONA-UX-MAPPING.md` | KEEP_WITH_REWRITE | Idem |
| `PLATFORM-CAPABILITIES.md` | KEEP_WITH_REWRITE | Verificar se lista capacidades reais atuais |
| `POST-PR16-EVOLUTION-ROADMAP.md` | ARCHIVE | Roadmap pós-PR16 — histórico de evolução passada |
| `PRODUCT-REFOUNDATION-PLAN.md` | DELETE_CANDIDATE | Plano de refundação executado |
| `PRODUCT-SCOPE.md` | KEEP_WITH_REWRITE | Escopo do produto — verificar vs módulos atuais |
| `PRODUCT-VISION.md` | KEEP_WITH_REWRITE | Visão — verificar se ainda é atual |
| `REBASELINE.md` | DELETE_CANDIDATE | Evento de rebaseline passado |
| `ROADMAP.md` | KEEP_WITH_REWRITE | Roadmap — limpar fases concluídas |
| `SECURITY-ARCHITECTURE.md` | KEEP | Segurança — fundamental |
| `SECURITY.md` | KEEP | Idem |
| `SERVICE-CONTRACT-GOVERNANCE.md` | KEEP_WITH_REWRITE | Conceito central; verificar vs implementação |
| `SOLUTION-GAP-ANALYSIS.md` | MERGE | Consolidar com outros gap docs |
| `SOURCE-OF-TRUTH-STRATEGY.md` | KEEP_WITH_REWRITE | Feature ativa; verificar vs SourceOfTruthEndpointModule |
| `UX-PRINCIPLES.md` | KEEP | Princípios UX — referência válida |
| `WAVE-1-CONSOLIDATED-VALIDATION.md` | ARCHIVE | Validação de Wave-1 — histórico |
| `WAVE-1-VALIDATION-TRACKER.md` | ARCHIVE | Tracker de Wave-1 — histórico |

---

## Secção 2 — `docs/acceptance/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `NexTraceOne_Baseline_Estavel.md` | ARCHIVE | Baseline de aceite de versão anterior |
| `NexTraceOne_Checklist_Entrada_Aceite.md` | ARCHIVE | Checklist de aceite anterior |
| `NexTraceOne_Escopo_Homologavel.md` | ARCHIVE | Escopo de homologação anterior |
| `NexTraceOne_Plano_Teste_Funcional.md` | ARCHIVE | Plano de testes funcional anterior |
| `NexTraceOne_Relatorio_Teste_Aceitacao.md` | ARCHIVE | Relatório de aceite anterior |

---

## Secção 3 — `docs/aiknowledge/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `AIK_EXTERNAL_AI_FLOW.md` | MERGE | Parte da consolidação AI Hub |
| `AIK_ORCHESTRATION_DESIGN.md` | MERGE | Idem |
| `PHASE-2-AIKNOWLEDGE-COMPLETION.md` | ARCHIVE | Relatório de conclusão de fase |

---

## Secção 4 — `docs/architecture/`

### ADRs na raiz de `docs/architecture/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `ADR-001-database-strategy.md` | MERGE | Duplicado com `adr/ADR-001-*` — consolidar |
| `ADR-002-migration-policy.md` | MERGE | Duplicado direto com `adr/ADR-002-migration-policy.md` |
| `ADR-003-event-bus-limitations.md` | KEEP | ADR relevante — limitações do event bus in-process |
| `ADR-004-simulated-data-policy.md` | KEEP | ADR relevante — política de dados simulados |
| `ADR-005-ai-runtime-foundation.md` | KEEP | ADR de AI runtime — ainda relevante |
| `ADR-006-agent-runtime-foundation.md` | KEEP | ADR de agent runtime — ainda relevante |

### `docs/architecture/adr/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `ADR-001-database-consolidation-plan.md` | MERGE | Consolidar com versão na raiz |
| `ADR-002-event-bus-in-process-limitation.md` | MERGE | Consolidar com ADR-003 da raiz |
| `ADR-002-migration-policy.md` | MERGE | Duplicado direto |

### `docs/architecture/environments/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `environment-control-audit.md` | KEEP_WITH_REWRITE | Auditoria de controlo de ambientes — verificar vs EnvironmentContext atual |
| `environment-control-transition-notes.md` | ARCHIVE | Notas de transição — histórico |
| `environment-management-design.md` | KEEP_WITH_REWRITE | Design de gestão de ambientes |
| `environment-production-designation.md` | KEEP | Designação de produção — operacionalmente relevante |
| `non-prod-to-prod-risk-analysis.md` | KEEP | Análise de risco — relevante para operações |

### `docs/architecture/phase-0/` a `docs/architecture/phase-9/`

**Todos os ficheiros de phase-0 a phase-9 são classificados como ARCHIVE.**

São registos históricos do processo de desenvolvimento por fases. Têm valor como memória arquitetural mas não refletem o estado atual do produto.

- Phase-0: 6 ficheiros → ARCHIVE
- Phase-1: 3 ficheiros → ARCHIVE
- Phase-2: 4 ficheiros → ARCHIVE
- Phase-4: 4 ficheiros → ARCHIVE
- Phase-4-agents: 1 ficheiro → ARCHIVE
- Phase-5: 5 ficheiros → ARCHIVE
- Phase-6: 4 ficheiros → ARCHIVE
- Phase-7: 5 ficheiros → ARCHIVE
- Phase-8: 4 ficheiros → ARCHIVE
- Phase-9: 5 ficheiros → ARCHIVE

**Total de phase docs para arquivo:** ~41 ficheiros

---

## Secção 5 — `docs/assessment/` (12 ficheiros)

Esta série representa a avaliação mais recente e completa do estado do sistema.

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `00-EXECUTIVE-SUMMARY.md` | KEEP | Sumário executivo — avaliar data |
| `01-SOLUTION-INVENTORY.md` | KEEP_WITH_REWRITE | Inventário de solução — atualizar com achados desta auditoria |
| `02-FUNCTIONAL-MODULE-MAP.md` | KEEP_WITH_REWRITE | Mapa funcional — provavelmente desatualizado |
| `03-COMPLETENESS-MATRIX.md` | KEEP_WITH_REWRITE | Matriz de completude — verificar vs IMPLEMENTATION-STATUS |
| `04-HIDDEN-REMOVED-INCOMPLETE-FEATURES.md` | KEEP | Funcionalidades escondidas/removidas — referência útil |
| `05-BACKEND-AUDIT.md` | KEEP_WITH_REWRITE | Auditoria backend — verificar vs 9 módulos atuais |
| `06-FRONTEND-AUDIT.md` | KEEP_WITH_REWRITE | Auditoria frontend — verificar vs 15 módulos atuais |
| `07-DATA-MIGRATIONS-TENANCY-AUDIT.md` | KEEP_WITH_REWRITE | Verificar vs migrations mais recentes (2026-03-23) |
| `08-SECURITY-AUDIT.md` | KEEP | Auditoria de segurança — relevante |
| `09-OBSERVABILITY-AND-AI-READINESS.md` | KEEP_WITH_REWRITE | Verificar vs estado atual de observabilidade |
| `10-PRODUCTION-READINESS.md` | KEEP_WITH_REWRITE | Prontidão para produção — verificar |
| `11-GAP-BACKLOG-PRIORITIZED.md` | KEEP_WITH_REWRITE | Backlog priorizado — comparar vs achados desta auditoria |
| `12-RECOMMENDED-EXECUTION-PLAN.md` | ARCHIVE | Plano de execução que pode já ter sido seguido |

---

## Secção 6 — `docs/audits/` (43 ficheiros)

Série de auditorias por fase e wave. **Todos os ficheiros de phase e wave são ARCHIVE.**

| Grupo | Ficheiros | Classificação |
|-------|-----------|---------------|
| CONFIGURATION-PHASE-1 a 8 | 8 | ARCHIVE |
| NEXTRACEONE-FINAL-GO-LIVE-AUDIT | 1 | ARCHIVE |
| NEXTRACEONE-*GAP* | 3 | ARCHIVE |
| NOTIFICATIONS-PHASE-0/3/4/5/6/7 | 6 | ARCHIVE |
| PHASE-0-* | 5 | ARCHIVE |
| PHASE-1 a 9 | 9 | ARCHIVE |
| WAVE-1 a WAVE-FINAL | 7 | ARCHIVE |
| FINAL-BLOCKERS, FINAL-GO-LIVE-GATE, FINAL-MODULE-CONFORMITY | 3 | KEEP_WITH_REWRITE → verificar se ainda aplicáveis |

---

## Secção 7 — `docs/execution/` (95 ficheiros)

Esta é a pasta com maior volume. Contém dois grandes conjuntos:

### Guias de Configuração (`CONFIGURATION-*`)

São guias detalhados de como configurar o sistema por área funcional. Valor operacional alto mas precisam de ser mapeados às páginas de configuração reais.

| Sub-grupo | Qtd | Classificação | Observação |
|-----------|-----|---------------|------------|
| `CONFIGURATION-PHASE-*` | 8 | ARCHIVE | Versões de fase, substituídas por versões sem fase |
| `CONFIGURATION-ADVANCED-CONSOLE-*` | 1 | KEEP_WITH_REWRITE | Verificar vs AdvancedConfigurationConsolePage |
| `CONFIGURATION-AI-*` | 1 | KEEP_WITH_REWRITE | Verificar vs AiIntegrationsConfigurationPage |
| `CONFIGURATION-APPROVERS-*` | 1 | KEEP_WITH_REWRITE | Verificar vs WorkflowConfigurationPage |
| `CONFIGURATION-BENCHMARKING-*` | 1 | KEEP_WITH_REWRITE | Verificar vs OperationsFinOpsConfigurationPage |
| `CONFIGURATION-BRANDING-*` | 1 | KEEP_WITH_REWRITE | Verificar vs ConfigurationAdminPage |
| `CONFIGURATION-CHANGE-*` | 1 | KEEP_WITH_REWRITE | Verificar vs CatalogContractsConfigurationPage |
| `CONFIGURATION-CONTRACT-*` | 1 | KEEP_WITH_REWRITE | Idem |
| `CONFIGURATION-ENVIRONMENT-MODEL.md` | 1 | KEEP_WITH_REWRITE | Verificar vs EnvironmentsPage |
| `CONFIGURATION-FINOPS-*` | 1 | KEEP_WITH_REWRITE | Verificar vs GovernanceConfigurationPage |
| `CONFIGURATION-HEALTH-*` | 1 | KEEP_WITH_REWRITE | Verificar vs PlatformOperationsPage |
| `CONFIGURATION-IMPORT-EXPORT-*` | 1 | KEEP_WITH_REWRITE | Verificar vs AdvancedConfigurationConsolePage |
| `CONFIGURATION-INCIDENT-*` | 1 | KEEP_WITH_REWRITE | Verificar vs OperationsFinOpsConfigurationPage |
| `CONFIGURATION-INSTANCE-*` | 1 | KEEP_WITH_REWRITE | Verificar vs ConfigurationAdminPage |
| `CONFIGURATION-INTEGRATION-*` | 2 | KEEP_WITH_REWRITE | Verificar vs IntegrationHubPage |
| `CONFIGURATION-MINIMUM-*` | 1 | KEEP_WITH_REWRITE | Verificar vs ConfigurationAdminPage |
| `CONFIGURATION-NOTIFICATION-*` | 3 | KEEP_WITH_REWRITE | Verificar vs NotificationConfigurationPage |
| `CONFIGURATION-POLICY-*` | 1 | KEEP_WITH_REWRITE | Verificar vs GovernanceConfigurationPage |
| `CONFIGURATION-PROMOTION-*` | 1 | KEEP_WITH_REWRITE | Verificar vs WorkflowConfigurationPage |
| `CONFIGURATION-SCORECARDS-*` | 1 | KEEP_WITH_REWRITE | Verificar vs GovernanceConfigurationPage |
| `CONFIGURATION-WAIVERS-*` | 1 | KEEP_WITH_REWRITE | Verificar vs WaiversPage/GovernanceConfigurationPage |
| `CONFIGURATION-WORKFLOW-*` | 1 | KEEP_WITH_REWRITE | Verificar vs WorkflowConfigurationPage |

### Guias de Notificações (`NOTIFICATIONS-*`)

| Sub-grupo | Qtd | Classificação |
|-----------|-----|---------------|
| `NOTIFICATIONS-PHASE-*` | 7 | ARCHIVE |
| `NOTIFICATIONS-ARCHITECTURE.md` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-ACKNOWLEDGE-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-ADMIN-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-APPROVALS-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-AUDIT-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-CATALOG-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-CHANNEL-POLICY.md` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-CONTRACTS-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-DEDUPLICATION-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-DELIVERY-LOG-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-EMAIL-CHANNEL.md` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-ESCALATION-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-EVENT-CATALOG.md` | 1 | KEEP |
| `NOTIFICATIONS-METRICS-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-OPERATIONS-*` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-PREFERENCE-MODEL.md` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-RECIPIENT-RESOLUTION.md` | 1 | KEEP_WITH_REWRITE |
| `NOTIFICATIONS-ROADMAP.md` | ARCHIVE | Roadmap de notificações — histórico |
| `NOTIFICATIONS-TEAMS-CHANNEL.md` | 1 | KEEP_WITH_REWRITE |

### Guias de Execução por Phase/Wave

| Sub-grupo | Qtd | Classificação |
|-----------|-----|---------------|
| `PHASE-0-*` | 6 | ARCHIVE |
| `PHASE-1-*` | 6 | ARCHIVE |
| `PHASE-4-AI-*` | 5 | ARCHIVE |
| `PHASE-5-*` | 5 | ARCHIVE |
| `PHASE-7-*` | 5 | ARCHIVE |
| `WAVE-1-*` | 3 | ARCHIVE |
| `WAVE-2-*` | 3 | ARCHIVE |
| `WAVE-3-*` | 4 | ARCHIVE |
| `WAVE-5-*` | 5 | ARCHIVE |
| `WAVE-FINAL-*` | 6 | ARCHIVE |

---

## Secção 8 — `docs/frontend/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `AUDIT-REPORT.md` | KEEP_WITH_REWRITE | Auditoria frontend anterior — comparar com achados atuais |
| `REFACTORING-PLAN.md` | ARCHIVE | Plano de refactoring anterior |
| `TECHNICAL-INVENTORY.md` | KEEP_WITH_REWRITE | Inventário técnico — verificar vs 15 módulos atuais |

---

## Secção 9 — `docs/governance/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `PHASE-5-GOVERNANCE-ENRICHMENT.md` | ARCHIVE | Relatório de fase |

---

## Secção 10 — `docs/observability/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `README.md` | KEEP | Entrada da pasta |
| `architecture-overview.md` | KEEP_WITH_REWRITE | Verificar vs implementação atual |
| `DRIFT-DETECTION-PIPELINE.md` | KEEP | Pipeline de drift detection — ativo |
| `ENVIRONMENT-COMPARISON-ARCHITECTURE.md` | KEEP | EnvironmentComparisonPage ativa |
| `INGESTION-API-ROLE-AND-FLOW.md` | KEEP | Ingestion.Api ativo |
| `PHASE-6-OBSERVABILITY-COMPLETION.md` | ARCHIVE | Relatório de fase |
| `collection/iis-clr-profiler.md` | KEEP | Guia técnico operacional |
| `collection/kafka-log-collection.md` | KEEP | Guia técnico operacional |
| `collection/kubernetes-otel-collector.md` | KEEP | Guia técnico operacional |
| `configuration/nextraceone-observability-settings.md` | KEEP | Configuração de observabilidade |
| `providers/clickhouse.md` | KEEP | Provider ativo |
| `providers/elastic.md` | KEEP | Provider ativo |
| `troubleshooting.md` | KEEP | Troubleshooting operacional |

---

## Secção 11 — `docs/quality/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `CONTRACT-TEST-BOUNDARIES.md` | KEEP | Fronteiras de teste de contratos |
| `E2E-GO-LIVE-SUITE.md` | KEEP_WITH_REWRITE | Suite E2E — verificar vs e2e/ atual |
| `PERFORMANCE-AND-RESILIENCE-BASELINE.md` | KEEP | Baseline de performance |
| `PHASE-8-VALIDATION-MATRIX.md` | ARCHIVE | Matriz de validação de fase |
| `TEST-STRATEGY-AND-LAYERS.md` | KEEP | Estratégia de testes |

---

## Secção 12 — `docs/release/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `NexTraceOne_Final_Production_Scope.md` | KEEP | Escopo de produção final — referência |
| `NexTraceOne_Release_Gate_Final.md` | ARCHIVE | Gate de release — histórico |
| `NexTraceOne_ZR1_*` a `NexTraceOne_ZR6_*` | ARCHIVE (5) | Zero Ressalvas series — histórico |
| `NexTraceOne_Zero_Ressalvas_Backlog.md` | ARCHIVE | Histórico |

---

## Secção 13 — `docs/reliability/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `PHASE-3-RELIABILITY-COMPLETION.md` | ARCHIVE | Relatório de fase |
| `RELIABILITY-DATA-MODEL.md` | KEEP | Modelo de dados de reliability |
| `RELIABILITY-FRONTEND-INTEGRATION.md` | KEEP_WITH_REWRITE | Verificar vs TeamReliabilityPage atual |
| `RELIABILITY-SCORING-MODEL.md` | KEEP | Modelo de scoring |

---

## Secção 14 — `docs/reviews/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `ANALISE-CRITICA-ARQUITETURAL-2026-03.md` | KEEP | Análise crítica recente (março 2026) |
| `NexTraceOne_Full_Production_Convergence_Report.md` | KEEP | Relatório de convergência de produção |
| `NexTraceOne_Production_Readiness_Review.md` | KEEP | Review de prontidão — recente |

---

## Secção 15 — `docs/runbooks/`

**Toda a série é KEEP** — são guias operacionais ativos.

| Ficheiro | Classificação |
|----------|---------------|
| `AI-PROVIDER-DEGRADATION-RUNBOOK.md` | KEEP |
| `BACKUP-OPERATIONS-RUNBOOK.md` | KEEP |
| `DRIFT-AND-ENVIRONMENT-ANALYSIS-RUNBOOK.md` | KEEP |
| `INCIDENT-RESPONSE-PLAYBOOK.md` | KEEP |
| `MIGRATION-FAILURE-RUNBOOK.md` | KEEP |
| `POST-DEPLOY-VALIDATION.md` | KEEP |
| `PRODUCTION-DEPLOY-RUNBOOK.md` | KEEP |
| `PRODUCTION-SECRETS-PROVISIONING.md` | KEEP |
| `RESTORE-OPERATIONS-RUNBOOK.md` | KEEP |
| `ROLLBACK-RUNBOOK.md` | KEEP |
| `STAGING-DEPLOY-RUNBOOK.md` | KEEP |

---

## Secção 16 — `docs/security/`

**Todos relevantes, maioria KEEP.**

| Ficheiro | Classificação |
|----------|---------------|
| `BACKEND-ENDPOINT-AUTH-AUDIT.md` | KEEP |
| `PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md` | KEEP_WITH_REWRITE |
| `PHASE-1-SECRETS-BASELINE.md` | ARCHIVE |
| `REQUIRED-ENVIRONMENT-CONFIGURATION.md` | KEEP |
| `application-hardening-checklist.md` | KEEP |
| `application-onprem-hardening-notes.md` | KEEP |
| `application-privacy-lgpd-gdpr-notes.md` | KEEP |
| `application-security-review.md` | KEEP |
| `security-backend-infra-integration-notes.md` | KEEP |

---

## Secção 17 — `docs/user-guide/`

**Todos precisam de reescrita — incompletos.**

| Ficheiro | Classificação | Observação |
|----------|---------------|------------|
| `README.md` | KEEP_WITH_REWRITE | Entrada do guia — incompleta |
| `ai-hub.md` | KEEP_WITH_REWRITE | Verificar vs 10 páginas de AI Hub |
| `change-governance.md` | KEEP_WITH_REWRITE | Verificar vs 6 páginas |
| `getting-started.md` | KEEP_WITH_REWRITE | Onboarding — verificar flow real |
| `governance-reports.md` | KEEP_WITH_REWRITE | Verificar vs 20 páginas de governance |
| `operations.md` | KEEP_WITH_REWRITE | Verificar vs 8 páginas |
| `service-catalog.md` | KEEP_WITH_REWRITE | Verificar vs ServiceCatalog atual |
| `troubleshooting.md` | KEEP_WITH_REWRITE | Verificar vs problemas conhecidos |

**Módulos sem user-guide:** contracts, identity-access, integrations, product-analytics, notifications, audit-compliance

---

## Secção 18 — Outros `docs/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `docs/checklists/GO-LIVE-CHECKLIST.md` | ARCHIVE | Checklist de go-live passado |
| `docs/deployment/CI-CD-PIPELINES.md` | KEEP | CI/CD ativo |
| `docs/deployment/DOCKER-AND-COMPOSE.md` | KEEP | Docker ativo (4 Dockerfiles na raiz) |
| `docs/deployment/ENVIRONMENT-CONFIGURATION.md` | KEEP | Configuração de ambiente |
| `docs/deployment/MIGRATION-STRATEGY.md` | KEEP | Estratégia de migração — 50+ migrations |
| `docs/deployment/PHASE-7-DELIVERY-AND-DEPLOYMENT.md` | ARCHIVE | Relatório de fase |
| `docs/engineering/ANTI-DEMO-REGRESSION-CHECKLIST.md` | KEEP | Checklist anti-demo — relevante |
| `docs/engineering/PHASE-0-PRODUCT-FREEZE-POLICY.md` | ARCHIVE | Política de freeze de fase |
| `docs/engineering/PRODUCT-DEFINITION-OF-DONE.md` | KEEP | Definition of Done — relevante |
| `docs/estado-atual-projeto-e-plano-testes.md` | UNKNOWN | Verificar data e conteúdo |
| `docs/governance/PHASE-5-GOVERNANCE-ENRICHMENT.md` | ARCHIVE | Relatório de fase |
| `docs/planos/NexTraceOne_Fase10_Plano_Evolucao.md` | KEEP_WITH_REWRITE | Plano de evolução Fase 10 |
| `docs/rebaseline/NexTraceOne_Fase0_Rebaseline_Arquitetural_2026-03-18.md` | ARCHIVE | Rebaseline de fase |
| `docs/rebaseline/NexTraceOne_Fase1_Persistencia_Migrations_2026-03-18.md` | ARCHIVE | Rebaseline de fase |
| `docs/roadmap/PHASE-0-FINALIZATION-BACKLOG.md` | ARCHIVE | Backlog de fase anterior |
| `docs/telemetry/TELEMETRY-ARCHITECTURE.md` | KEEP | Arquitetura de telemetria |
| `docs/testing/NexTraceOne_RH6_Reality_Matrix.md` | KEEP_WITH_REWRITE | Matriz de realidade de testes |

---

## Secção 19 — Fora de `docs/`

| Ficheiro | Classificação | Justificativa |
|----------|---------------|---------------|
| `.github/copilot-instructions.md` | KEEP | Instruções de Copilot — ativas |
| `build/README.md` | UNKNOWN | Verificar conteúdo |
| `src/frontend/README.md` | KEEP_WITH_REWRITE | README do frontend |
| `src/frontend/ARCHITECTURE.md` | KEEP_WITH_REWRITE | Arquitetura do frontend |
| `src/frontend/src/shared/design-system/README.md` | KEEP | Design system docs |
| `tests/load/README.md` | KEEP | README de load tests |

---

## Resumo Final de Classificação

| Classificação | Qtd Estimada | % |
|---------------|--------------|---|
| KEEP | ~65 | 18% |
| KEEP_WITH_REWRITE | ~85 | 24% |
| MERGE | ~12 | 3% |
| ARCHIVE | ~160 | 45% |
| DELETE_CANDIDATE | ~8 | 2% |
| UNKNOWN | ~27 | 8% |
| **Total** | **~357** | **100%** |

### Observação Principal

**45% da documentação é histórico de fases/waves operacionais** que já foram executadas. Este material tem valor de referência histórica mas não deveria ser a documentação "ativa" do produto. Recomenda-se criar uma pasta `docs/archive/` e mover toda a série `phase-*/wave-*/ZR-*` para lá.

A documentação funcional do produto (user guides, módulos, páginas, UX) é a área mais fragilizada — cobre menos de 10% dos módulos e páginas reais.
