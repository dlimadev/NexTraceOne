# Índice de Documentação — NexTraceOne

> **Última actualização:** 2026-04-25

Este índice serve como ponto de entrada único para navegar toda a documentação do repositório.

---

## 1. Documentação Principal

| Ficheiro | Descrição |
|---------|-----------|
| [`README.md`](README.md) | Introdução e navegação |
| [`FUTURE-ROADMAP.md`](FUTURE-ROADMAP.md) | Roadmap histórico de funcionalidades — waves A → BC (155 features, 100% concluídas) |
| [`PLANO-DE-ACAO-V2.md`](PLANO-DE-ACAO-V2.md) | **Plano de acção consolidado pós-v1.0.0** — substitui e consolida todos os planos anteriores |
| [`NEXT-ACTION-PLAN.md`](NEXT-ACTION-PLAN.md) | Referência histórica — evoluções pós-v1.0.0 (ver PLANO-DE-ACAO-V2.md para versão completa) |
| [`IMPLEMENTATION-STATUS.md`](IMPLEMENTATION-STATUS.md) | Estado de implementação por módulo |
| [`HONEST-GAPS.md`](HONEST-GAPS.md) | Referência de padrões OOS, stubs by design e providers DEG-01..15 |
| [`PRODUCT-VISION.md`](PRODUCT-VISION.md) | Visão do produto |
| [`NEXTRACEONE-PRESENTATION.md`](NEXTRACEONE-PRESENTATION.md) | **Documento formal de apresentação** — O que é, que problemas resolve, diferenciação, proposta de valor e ROI |
| [`PITCH.md`](PITCH.md) | Resumo de investimento e posicionamento comercial |
| [`CHANGELOG.md`](CHANGELOG.md) | Histórico de versões (Keep a Changelog) |

---

## 2. Arquitectura

| Ficheiro | Descrição |
|---------|-----------|
| [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) | Visão geral da arquitectura |
| [`SECURITY-ARCHITECTURE.md`](SECURITY-ARCHITECTURE.md) | Arquitectura de segurança |
| [`FRONTEND-ARCHITECTURE.md`](FRONTEND-ARCHITECTURE.md) | Arquitectura frontend |
| [`DEPLOYMENT-ARCHITECTURE.md`](DEPLOYMENT-ARCHITECTURE.md) | Arquitectura de deployment |
| [`OBSERVABILITY-STRATEGY.md`](OBSERVABILITY-STRATEGY.md) | Estratégia de observabilidade |

---

## 3. ADRs (Architecture Decision Records) — `docs/adr/`

| Ficheiro | Decisão |
|---------|---------|
| [`adr/001-modular-monolith.md`](adr/001-modular-monolith.md) | Modular Monolith |
| [`adr/002-single-database-per-tenant.md`](adr/002-single-database-per-tenant.md) | Single database per tenant |
| [`adr/003-elasticsearch-observability.md`](adr/003-elasticsearch-observability.md) | Elasticsearch para observabilidade |
| [`adr/004-local-ai-first.md`](adr/004-local-ai-first.md) | Local AI first |
| [`adr/005-react-frontend-stack.md`](adr/005-react-frontend-stack.md) | React frontend stack |
| [`adr/006-graphql-protobuf-roadmap.md`](adr/006-graphql-protobuf-roadmap.md) | GraphQL/Protobuf — Superseded (implementados em Wave G.3 e H.1) |
| [`adr/007-data-contracts.md`](adr/007-data-contracts.md) | Data Contracts como cidadão de primeira classe (Partially Implemented — Wave AQ.1) |
| [`adr/008-change-confidence-score-v2.md`](adr/008-change-confidence-score-v2.md) | Change Confidence Score 2.0 decomponível com sub-scores explicáveis (Proposed) |
| [`adr/009-ai-evaluation-harness.md`](adr/009-ai-evaluation-harness.md) | AI Evaluation Harness interno para benchmarking de modelos (Proposed) |
| [`adr/010-server-side-ingestion-pipeline.md`](adr/010-server-side-ingestion-pipeline.md) | Ingestion Pipeline configurável por tenant — DLQ, observabilidade, regras (Proposed) |

---

## 4. AI

| Ficheiro | Descrição |
|---------|-----------|
| [`AI-EVOLUTION-ROADMAP.md`](AI-EVOLUTION-ROADMAP.md) | Roadmap de evolução AI — Fases 0–1 ✅ implementadas; Fases 2–4 pendentes |
| [`AI-INNOVATION-BLUEPRINT.md`](AI-INNOVATION-BLUEPRINT.md) | Blueprint de inovações AI sem concorrência — OME, Adaptive Contracts, etc. (Fase 4) |
| [`AI-AGENT-LIGHTNING.md`](AI-AGENT-LIGHTNING.md) | Agent Lightning (RL) — plano de integração para Fase 2 |
| [`AI-MODELS-ANALYSIS.md`](AI-MODELS-ANALYSIS.md) | **Análise de modelos de IA** — recomendações, requisitos de servidor, licenciamento |
| [`AI-ENTERPRISE-CAPABILITIES.md`](AI-ENTERPRISE-CAPABILITIES.md) | Capacidades enterprise da plataforma AI |

---

## 5. Contratos e Change Intelligence

| Ficheiro | Descrição |
|---------|-----------|
| [`SERVICE-CONTRACT-GOVERNANCE.md`](SERVICE-CONTRACT-GOVERNANCE.md) | Governança de serviços e contratos — modelo de domínio implementado |
| [`INGESTION-PIPELINE-IMPLEMENTATION.md`](INGESTION-PIPELINE-IMPLEMENTATION.md) | Plano de implementação do Ingestion Pipeline avançado (PIP-01..06) |

---

## 6. Frontend & UX

| Ficheiro | Descrição |
|---------|-----------|
| [`DESIGN-SYSTEM.md`](DESIGN-SYSTEM.md) | Design system |
| [`DESIGN.md`](DESIGN.md) | UX e design visual |
| [`BRAND-IDENTITY.md`](BRAND-IDENTITY.md) | Identidade visual |
| [`PERSONA-MATRIX.md`](PERSONA-MATRIX.md) | Matriz de personas |
| [`V3-EVOLUTION-FRONTEND-DASHBOARDS.md`](V3-EVOLUTION-FRONTEND-DASHBOARDS.md) | V3 Frontend Evolution — 12 waves de evolução da superfície operacional |

---

## 7. Engineering & Development

| Ficheiro | Descrição |
|---------|-----------|
| [`BACKEND-MODULE-GUIDELINES.md`](BACKEND-MODULE-GUIDELINES.md) | Guidelines de backend |
| [`GUIDELINE.md`](GUIDELINE.md) | Guidelines gerais |
| [`LOCAL-SETUP.md`](LOCAL-SETUP.md) | Setup local de desenvolvimento |
| [`ENVIRONMENT-VARIABLES.md`](ENVIRONMENT-VARIABLES.md) | Variáveis de ambiente |
| [`TESTING-STRATEGY.md`](TESTING-STRATEGY.md) | Estratégia de testes |
| [`dev-setup/user-secrets-guide.md`](dev-setup/user-secrets-guide.md) | Guia de user secrets |
| [`dev/VALIDATOR-TEMPLATE.md`](dev/VALIDATOR-TEMPLATE.md) | Template de validator |

---

## 8. Segurança — `docs/security/`

| Ficheiro | Descrição |
|---------|-----------|
| [`SECURITY.md`](SECURITY.md) | Modelo de segurança |
| [`security/BACKEND-ENDPOINT-AUTH-AUDIT.md`](security/BACKEND-ENDPOINT-AUTH-AUDIT.md) | Auditoria de autenticação de endpoints |
| [`security/KEY-ROTATION.md`](security/KEY-ROTATION.md) | Rotação de chaves |
| [`security/PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md`](security/PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md) | Checklist baseline produção |
| [`security/PHASE-1-SECRETS-BASELINE.md`](security/PHASE-1-SECRETS-BASELINE.md) | Baseline de segredos |
| [`security/REQUIRED-ENVIRONMENT-CONFIGURATION.md`](security/REQUIRED-ENVIRONMENT-CONFIGURATION.md) | Configuração de ambiente obrigatória |
| [`security/application-hardening-checklist.md`](security/application-hardening-checklist.md) | Checklist de hardening |
| [`security/application-onprem-hardening-notes.md`](security/application-onprem-hardening-notes.md) | Notas de hardening on-prem |
| [`security/application-privacy-lgpd-gdpr-notes.md`](security/application-privacy-lgpd-gdpr-notes.md) | Notas de privacidade LGPD/GDPR |
| [`security/application-security-review.md`](security/application-security-review.md) | Revisão de segurança |
| [`security/security-backend-infra-integration-notes.md`](security/security-backend-infra-integration-notes.md) | Notas de integração infra backend |

---

## 9. Deployment e Operações — `docs/deployment/`

| Ficheiro | Descrição |
|---------|-----------|
| [`deployment/PRODUCTION-BOOTSTRAP.md`](deployment/PRODUCTION-BOOTSTRAP.md) | **Guia de bootstrap para produção** |
| [`deployment/CI-CD-PIPELINES.md`](deployment/CI-CD-PIPELINES.md) | Pipelines CI/CD |
| [`deployment/DOCKER-AND-COMPOSE.md`](deployment/DOCKER-AND-COMPOSE.md) | Docker e Docker Compose |
| [`deployment/ENVIRONMENT-CONFIGURATION.md`](deployment/ENVIRONMENT-CONFIGURATION.md) | Configuração de ambientes |
| [`deployment/MIGRATION-STRATEGY.md`](deployment/MIGRATION-STRATEGY.md) | Estratégia de migrações |

---

## 10. Runbooks — `docs/runbooks/`

| Ficheiro | Descrição |
|---------|-----------|
| [`runbooks/PRODUCTION-DEPLOY-RUNBOOK.md`](runbooks/PRODUCTION-DEPLOY-RUNBOOK.md) | Deploy em produção |
| [`runbooks/STAGING-DEPLOY-RUNBOOK.md`](runbooks/STAGING-DEPLOY-RUNBOOK.md) | Deploy em staging |
| [`runbooks/INCIDENT-RESPONSE-PLAYBOOK.md`](runbooks/INCIDENT-RESPONSE-PLAYBOOK.md) | Playbook de resposta a incidentes |
| [`runbooks/DRIFT-AND-ENVIRONMENT-ANALYSIS-RUNBOOK.md`](runbooks/DRIFT-AND-ENVIRONMENT-ANALYSIS-RUNBOOK.md) | Análise de drift e ambientes |
| [`runbooks/ROLLBACK-RUNBOOK.md`](runbooks/ROLLBACK-RUNBOOK.md) | Rollback |
| [`runbooks/BACKUP-OPERATIONS-RUNBOOK.md`](runbooks/BACKUP-OPERATIONS-RUNBOOK.md) | Operações de backup |
| [`runbooks/RESTORE-OPERATIONS-RUNBOOK.md`](runbooks/RESTORE-OPERATIONS-RUNBOOK.md) | Operações de restore |
| [`runbooks/MIGRATION-FAILURE-RUNBOOK.md`](runbooks/MIGRATION-FAILURE-RUNBOOK.md) | Falha de migração |
| [`runbooks/POST-DEPLOY-VALIDATION.md`](runbooks/POST-DEPLOY-VALIDATION.md) | Validação pós-deploy |
| [`runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md`](runbooks/AI-PROVIDER-DEGRADATION-RUNBOOK.md) | Degradação de provider AI |
| [`runbooks/PRODUCTION-SECRETS-PROVISIONING.md`](runbooks/PRODUCTION-SECRETS-PROVISIONING.md) | Provisioning de segredos |
| [`runbooks/contracts-operations.md`](runbooks/contracts-operations.md) | Operações de contratos |

---

## 11. Observabilidade — `docs/observability/`

| Ficheiro | Descrição |
|---------|-----------|
| [`observability/README.md`](observability/README.md) | Índice de observabilidade |
| [`observability/architecture-overview.md`](observability/architecture-overview.md) | Visão geral da arquitectura |
| [`observability/DRIFT-DETECTION-PIPELINE.md`](observability/DRIFT-DETECTION-PIPELINE.md) | Pipeline de detecção de drift |
| [`observability/ENVIRONMENT-COMPARISON-ARCHITECTURE.md`](observability/ENVIRONMENT-COMPARISON-ARCHITECTURE.md) | Comparação de ambientes |
| [`observability/INGESTION-API-ROLE-AND-FLOW.md`](observability/INGESTION-API-ROLE-AND-FLOW.md) | API de ingestão |
| [`observability/troubleshooting.md`](observability/troubleshooting.md) | Troubleshooting |
| [`observability/configuration/`](observability/configuration/) | Configuração de observabilidade |
| [`observability/collection/`](observability/collection/) | Colecção (IIS/CLR, Kafka, K8s) |
| [`observability/providers/`](observability/providers/) | Providers (ClickHouse, Elastic) |
| [`OTEL-INTEGRATION-GUIDE.md`](OTEL-INTEGRATION-GUIDE.md) | Guia de integração OTel Collector |
| [`telemetry/TELEMETRY-ARCHITECTURE.md`](telemetry/TELEMETRY-ARCHITECTURE.md) | Arquitectura de telemetria |

---

## 12. SDK / NexTrace Agent — `docs/sdk/`

| Ficheiro | Descrição |
|---------|-----------|
| [`sdk/README.md`](sdk/README.md) | Índice do SDK |
| [`NEXTTRACE-AGENT.md`](NEXTTRACE-AGENT.md) | Visão geral do NexTrace Agent (OTel Collector custom) |
| [`sdk/NEXTTRACE-AGENT-INSTALLER.md`](sdk/NEXTTRACE-AGENT-INSTALLER.md) | Instalação do agent por plataforma |
| [`sdk/NEXTTRACE-AGENT-PARAMETRIZATION.md`](sdk/NEXTTRACE-AGENT-PARAMETRIZATION.md) | Parametrização e variáveis de ambiente |
| [`sdk/github-action.md`](sdk/github-action.md) | GitHub Action integration |
| [`sdk/nuget-client.md`](sdk/nuget-client.md) | NuGet client SDK |

---

## 13. User Guide — `docs/user-guide/`

| Ficheiro | Descrição |
|---------|-----------|
| [`user-guide/README.md`](user-guide/README.md) | Índice do user guide |
| [`user-guide/getting-started.md`](user-guide/getting-started.md) | Getting started |
| [`user-guide/service-catalog.md`](user-guide/service-catalog.md) | Service Catalog |
| [`user-guide/change-governance.md`](user-guide/change-governance.md) | Change Governance |
| [`user-guide/governance-reports.md`](user-guide/governance-reports.md) | Governance Reports |
| [`user-guide/operations.md`](user-guide/operations.md) | Operations |
| [`user-guide/ai-hub.md`](user-guide/ai-hub.md) | AI Hub |
| [`user-guide/troubleshooting.md`](user-guide/troubleshooting.md) | Troubleshooting |

---

## 14. On-Prem — `docs/onprem/`

| Ficheiro | Descrição |
|---------|-----------|
| [`onprem/INDEX.md`](onprem/INDEX.md) | Índice on-prem |
| [`onprem/WAVE-01-INSTALLATION.md`](onprem/WAVE-01-INSTALLATION.md) | Instalação |
| [`onprem/WAVE-02-SELF-MONITORING.md`](onprem/WAVE-02-SELF-MONITORING.md) | Self-monitoring |
| [`onprem/WAVE-03-UPDATE-RECOVERY.md`](onprem/WAVE-03-UPDATE-RECOVERY.md) | Update e recovery |
| [`onprem/WAVE-04-AI-LOCAL.md`](onprem/WAVE-04-AI-LOCAL.md) | AI local |
| [`onprem/WAVE-05-SECURITY-NETWORK.md`](onprem/WAVE-05-SECURITY-NETWORK.md) | Segurança e rede |
| [`onprem/WAVE-06-RESOURCES-FINOPS.md`](onprem/WAVE-06-RESOURCES-FINOPS.md) | Recursos e FinOps |
| [`onprem/WAVE-07-OBSERVABILITY.md`](onprem/WAVE-07-OBSERVABILITY.md) | Observabilidade |
| [`onprem/WAVE-08-FUTURE.md`](onprem/WAVE-08-FUTURE.md) | Futuro |

---

## 15. Roadmap e Inovação — `docs/analysis/`

| Ficheiro | Descrição |
|---------|-----------|
| [`analysis/INOVACAO-ROADMAP.md`](analysis/INOVACAO-ROADMAP.md) | Roadmap de inovação e propostas de novas funcionalidades |
| [`analysis/INFRA-EVOLUTION-OVERVIEW.md`](analysis/INFRA-EVOLUTION-OVERVIEW.md) | Visão geral das 4 fases de evolução de infraestrutura |
| [`analysis/INFRA-PHASE-1-POSTGRES-HARDENING.md`](analysis/INFRA-PHASE-1-POSTGRES-HARDENING.md) | Fase 1 — PostgreSQL Hardening (PgBouncer, partitioning, read replica) |
| [`analysis/INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md`](analysis/INFRA-PHASE-2-CLICKHOUSE-MIGRATION.md) | Fase 2 — ClickHouse como provider padrão de observabilidade |
| [`analysis/INFRA-PHASE-3-HOST-INFRASTRUCTURE.md`](analysis/INFRA-PHASE-3-HOST-INFRASTRUCTURE.md) | Fase 3 — Host Infrastructure Module (novo bounded context) |
| [`analysis/INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md`](analysis/INFRA-PHASE-4-TOPOLOGY-COMPLETIONS.md) | Fase 4 — Topology UI Time-Travel e SignalR real-time |

---

*Última consolidação: 2026-04-25*
