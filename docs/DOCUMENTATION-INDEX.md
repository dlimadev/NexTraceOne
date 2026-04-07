# Índice de Documentação — NexTraceOne

> **Última actualização:** 2026-04-07

Este índice distingue documentação **activa** de documentação **arquivada/histórica** e serve como ponto de entrada único para navegar o repositório.

---

## 1. Documentação Principal (Activa)

| Ficheiro | Descrição |
|---------|-----------|
| [`README.md`](../README.md) | Introdução ao repositório |
| [`CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md`](CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md) | **Documento canónico** — estado de todos os gaps e plano de ação |
| [`IMPLEMENTATION-STATUS.md`](IMPLEMENTATION-STATUS.md) | Estado de implementação por módulo |
| [`PRODUCT-VISION.md`](PRODUCT-VISION.md) | Visão do produto |
| [`PRODUCT-SCOPE.md`](PRODUCT-SCOPE.md) | Escopo do produto |
| [`ROADMAP.md`](ROADMAP.md) | Roadmap por ondas e sprints |
| [`EVOLUTION-ROADMAP-2026-2027.md`](EVOLUTION-ROADMAP-2026-2027.md) | Roadmap de evolução 2026-2027 |
| [`MODULES-AND-PAGES.md`](MODULES-AND-PAGES.md) | Módulos e páginas do produto |
| [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) | Visão geral da arquitectura |
| [`DOMAIN-BOUNDARIES.md`](DOMAIN-BOUNDARIES.md) | Fronteiras de domínio |
| [`DATA-ARCHITECTURE.md`](DATA-ARCHITECTURE.md) | Arquitectura de dados |
| [`SECURITY-ARCHITECTURE.md`](SECURITY-ARCHITECTURE.md) | Arquitectura de segurança |
| [`FRONTEND-ARCHITECTURE.md`](FRONTEND-ARCHITECTURE.md) | Arquitectura frontend |
| [`BACKEND-MODULE-GUIDELINES.md`](BACKEND-MODULE-GUIDELINES.md) | Guidelines de backend |
| [`DESIGN-SYSTEM.md`](DESIGN-SYSTEM.md) | Design system |
| [`I18N-STRATEGY.md`](I18N-STRATEGY.md) | Estratégia de internacionalização |
| [`OBSERVABILITY-STRATEGY.md`](OBSERVABILITY-STRATEGY.md) | Estratégia de observabilidade |
| [`SOURCE-OF-TRUTH-STRATEGY.md`](SOURCE-OF-TRUTH-STRATEGY.md) | Estratégia Source of Truth |
| [`CONTRACT-STUDIO-VISION.md`](CONTRACT-STUDIO-VISION.md) | Visão do Contract Studio |
| [`CHANGE-CONFIDENCE.md`](CHANGE-CONFIDENCE.md) | Change Confidence |
| [`AI-ARCHITECTURE.md`](AI-ARCHITECTURE.md) | Arquitectura AI |
| [`AI-GOVERNANCE.md`](AI-GOVERNANCE.md) | Governança AI |
| [`AI-ASSISTED-OPERATIONS.md`](AI-ASSISTED-OPERATIONS.md) | Operações assistidas por AI |
| [`AI-DEVELOPER-EXPERIENCE.md`](AI-DEVELOPER-EXPERIENCE.md) | Developer Experience AI |
| [`INTEGRATIONS-ARCHITECTURE.md`](INTEGRATIONS-ARCHITECTURE.md) | Arquitectura de integrações |
| [`DEPLOYMENT-ARCHITECTURE.md`](DEPLOYMENT-ARCHITECTURE.md) | Arquitectura de deployment |
| [`PLATFORM-CAPABILITIES.md`](PLATFORM-CAPABILITIES.md) | Capacidades da plataforma |
| [`PERSONA-MATRIX.md`](PERSONA-MATRIX.md) | Matriz de personas |
| [`PERSONA-UX-MAPPING.md`](PERSONA-UX-MAPPING.md) | Mapeamento UX por persona |
| [`UX-PRINCIPLES.md`](UX-PRINCIPLES.md) | Princípios UX |
| [`BRAND-IDENTITY.md`](BRAND-IDENTITY.md) | Identidade visual |
| [`ENVIRONMENT-VARIABLES.md`](ENVIRONMENT-VARIABLES.md) | Variáveis de ambiente |
| [`LOCAL-SETUP.md`](LOCAL-SETUP.md) | Setup local de desenvolvimento |
| [`GUIDELINE.md`](GUIDELINE.md) | Guidelines gerais |

---

## 2. ADRs (Architecture Decision Records) — `docs/adr/`

| Ficheiro | Decisão |
|---------|---------|
| [`adr/001-modular-monolith.md`](adr/001-modular-monolith.md) | Modular Monolith |
| [`adr/002-single-database-per-tenant.md`](adr/002-single-database-per-tenant.md) | Single database per tenant |
| [`adr/003-elasticsearch-observability.md`](adr/003-elasticsearch-observability.md) | Elasticsearch para observabilidade |
| [`adr/004-local-ai-first.md`](adr/004-local-ai-first.md) | Local AI first |
| [`adr/005-react-frontend-stack.md`](adr/005-react-frontend-stack.md) | React frontend stack |
| [`adr/006-graphql-protobuf-roadmap.md`](adr/006-graphql-protobuf-roadmap.md) | GraphQL/Protobuf roadmap |

---

## 3. Planos de Acção e Análises Completas

| Ficheiro | Descrição | Estado |
|---------|-----------|--------|
| [`SERVICES-CONTRACTS-ACTION-PLAN.md`](SERVICES-CONTRACTS-ACTION-PLAN.md) | Plano de acção para serviços e contratos (197/197 tarefas) | ✅ Completo |
| [`PARAMETERIZATION-MODULE-PROPOSAL.md`](PARAMETERIZATION-MODULE-PROPOSAL.md) | Proposta e execução do módulo de parametrização (6 fases) | ✅ Completo |
| [`SERVICE-CREATION-STUDIO-PLAN.md`](SERVICE-CREATION-STUDIO-PLAN.md) | Plano do Service Creation Studio | ✅ Completo |
| [`SERVICE-CONTRACT-GOVERNANCE.md`](SERVICE-CONTRACT-GOVERNANCE.md) | Governança de serviços e contratos | Activo |

---

## 4. Deployment e Operações — `docs/deployment/`

| Ficheiro | Descrição |
|---------|-----------|
| [`deployment/PRODUCTION-BOOTSTRAP.md`](deployment/PRODUCTION-BOOTSTRAP.md) | **Guia de bootstrap para produção** |
| [`deployment/CI-CD-PIPELINES.md`](deployment/CI-CD-PIPELINES.md) | Pipelines CI/CD |
| [`deployment/DOCKER-AND-COMPOSE.md`](deployment/DOCKER-AND-COMPOSE.md) | Docker e Docker Compose |
| [`deployment/ENVIRONMENT-CONFIGURATION.md`](deployment/ENVIRONMENT-CONFIGURATION.md) | Configuração de ambientes |
| [`deployment/MIGRATION-STRATEGY.md`](deployment/MIGRATION-STRATEGY.md) | Estratégia de migrações |

---

## 5. Segurança — `docs/security/`

| Ficheiro | Descrição |
|---------|-----------|
| [`SECURITY.md`](SECURITY.md) | Modelo de segurança |
| [`security/BACKEND-ENDPOINT-AUTH-AUDIT.md`](security/BACKEND-ENDPOINT-AUTH-AUDIT.md) | Auditoria de autenticação de endpoints |
| [`security/KEY-ROTATION.md`](security/KEY-ROTATION.md) | Rotação de chaves |
| [`security/PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md`](security/PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md) | Checklist baseline produção |
| [`security/PHASE-1-SECRETS-BASELINE.md`](security/PHASE-1-SECRETS-BASELINE.md) | Baseline de segredos |
| [`security/application-hardening-checklist.md`](security/application-hardening-checklist.md) | Checklist de hardening |

---

## 6. Runbooks — `docs/runbooks/`

| Ficheiro | Descrição |
|---------|-----------|
| [`runbooks/PRODUCTION-DEPLOY-RUNBOOK.md`](runbooks/PRODUCTION-DEPLOY-RUNBOOK.md) | Deploy em produção |
| [`runbooks/STAGING-DEPLOY-RUNBOOK.md`](runbooks/STAGING-DEPLOY-RUNBOOK.md) | Deploy em staging |
| [`runbooks/INCIDENT-RESPONSE-PLAYBOOK.md`](runbooks/INCIDENT-RESPONSE-PLAYBOOK.md) | Playbook de resposta a incidentes |
| [`runbooks/ROLLBACK-RUNBOOK.md`](runbooks/ROLLBACK-RUNBOOK.md) | Rollback |
| [`runbooks/BACKUP-OPERATIONS-RUNBOOK.md`](runbooks/BACKUP-OPERATIONS-RUNBOOK.md) | Operações de backup |
| [`runbooks/RESTORE-OPERATIONS-RUNBOOK.md`](runbooks/RESTORE-OPERATIONS-RUNBOOK.md) | Operações de restore |
| [`runbooks/MIGRATION-FAILURE-RUNBOOK.md`](runbooks/MIGRATION-FAILURE-RUNBOOK.md) | Falha de migração |
| [`runbooks/POST-DEPLOY-VALIDATION.md`](runbooks/POST-DEPLOY-VALIDATION.md) | Validação pós-deploy |
| [`runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md`](runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md) | Degradação de provider AI |
| [`runbooks/PRODUCTION-SECRETS-PROVISIONING.md`](runbooks/PRODUCTION-SECRETS-PROVISIONING.md) | Provisioning de segredos |
| [`runbooks/contracts-operations.md`](runbooks/contracts-operations.md) | Operações de contratos |

---

## 7. Observabilidade — `docs/observability/`

| Ficheiro | Descrição |
|---------|-----------|
| [`observability/architecture-overview.md`](observability/architecture-overview.md) | Visão geral da arquitectura |
| [`observability/DRIFT-DETECTION-PIPELINE.md`](observability/DRIFT-DETECTION-PIPELINE.md) | Pipeline de detecção de drift |
| [`observability/ENVIRONMENT-COMPARISON-ARCHITECTURE.md`](observability/ENVIRONMENT-COMPARISON-ARCHITECTURE.md) | Comparação de ambientes |
| [`observability/INGESTION-API-ROLE-AND-FLOW.md`](observability/INGESTION-API-ROLE-AND-FLOW.md) | API de ingestão |
| [`observability/pipeline-validation-report.md`](observability/pipeline-validation-report.md) | Relatório de validação do pipeline |
| [`observability/troubleshooting.md`](observability/troubleshooting.md) | Troubleshooting |

---

## 8. User Guide — `docs/user-guide/`

| Ficheiro | Descrição |
|---------|-----------|
| [`user-guide/getting-started.md`](user-guide/getting-started.md) | Getting started |
| [`user-guide/service-catalog.md`](user-guide/service-catalog.md) | Service Catalog |
| [`user-guide/change-governance.md`](user-guide/change-governance.md) | Change Governance |
| [`user-guide/governance-reports.md`](user-guide/governance-reports.md) | Governance Reports |
| [`user-guide/operations.md`](user-guide/operations.md) | Operations |
| [`user-guide/ai-hub.md`](user-guide/ai-hub.md) | AI Hub |
| [`user-guide/troubleshooting.md`](user-guide/troubleshooting.md) | Troubleshooting |

---

## 9. Legacy / Mainframe — `docs/legacy/`

Plano de desenvolvimento para suporte a core systems legacy/mainframe.

| Ficheiro | Descrição |
|---------|-----------|
| [`LEGACY-MAINFRAME-WAVES.md`](LEGACY-MAINFRAME-WAVES.md) | **Documento mestre** — visão geral das 13 ondas |
| [`legacy/WAVE-00-STRATEGY.md`](legacy/WAVE-00-STRATEGY.md) – [`legacy/WAVE-12-SECURITY-READINESS.md`](legacy/WAVE-12-SECURITY-READINESS.md) | Ondas 0–12 |

---

## 10. Documentação Arquivada / Histórica

> ⚠️ **Documentação arquivada não deve ser usada como referência operacional.**

| Ficheiro | Motivo |
|---------|--------|
| [`DEEP-ANALYSIS-APRIL-2026.md`](DEEP-ANALYSIS-APRIL-2026.md) | Supersedido por CONSOLIDATED-GAP-ANALYSIS |
| [`SERVICES-CONTRACTS-DEEP-ANALYSIS-2026-04.md`](SERVICES-CONTRACTS-DEEP-ANALYSIS-2026-04.md) | Supersedido por ACTION-PLAN |
| [`CORE-FLOW-GAPS.md`](CORE-FLOW-GAPS.md) | Supersedido por CONSOLIDATED-GAP-ANALYSIS |
| [`FEATURE-ANALYSIS-AND-INNOVATION.md`](FEATURE-ANALYSIS-AND-INNOVATION.md) | Completo (15/15 ondas) |
| `analysis-output/` | Removido — conteúdo consolidado |

---

## 11. Tecnologias Removidas / Não Usadas

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

*Última consolidação: 2026-04-07*
