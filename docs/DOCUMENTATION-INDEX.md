# Índice de Documentação — NexTraceOne

> **Última actualização:** 2026-04-07 — PLATFORM-CUSTOMIZATION-EVOLUTION Fases 1-8 COMPLETO; FUTURE-ROADMAP 6.1 e 6.2 IMPLEMENTADO

Este índice serve como ponto de entrada único para navegar toda a documentação do repositório.

---

## 1. Documentação Principal

| Ficheiro | Descrição |
|---------|-----------|
| [`README.md`](README.md) | Introdução e navegação |
| [`FUTURE-ROADMAP.md`](FUTURE-ROADMAP.md) | **Roadmap de funcionalidades futuras** — 6.1 Unit Tests ✅, 6.2 E2E Tests ✅ |
| [`PLATFORM-CUSTOMIZATION-EVOLUTION.md`](PLATFORM-CUSTOMIZATION-EVOLUTION.md) | **Plano de evolução de customização da plataforma** — Fases 1-8 ✅ COMPLETO |
| [`IMPLEMENTATION-STATUS.md`](IMPLEMENTATION-STATUS.md) | Estado de implementação por módulo |
| [`PRODUCT-VISION.md`](PRODUCT-VISION.md) | Visão do produto |
| [`NEXTRACEONE-PRESENTATION.md`](NEXTRACEONE-PRESENTATION.md) | Documento de apresentação do produto com base no codebase atual |
| [`MODULES-AND-PAGES.md`](MODULES-AND-PAGES.md) | Módulos e páginas do produto |
| [`PLATFORM-CAPABILITIES.md`](PLATFORM-CAPABILITIES.md) | Capacidades da plataforma |

---

## 2. Arquitectura

| Ficheiro | Descrição |
|---------|-----------|
| [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) | Visão geral da arquitectura |
| [`DOMAIN-BOUNDARIES.md`](DOMAIN-BOUNDARIES.md) | Fronteiras de domínio |
| [`DATA-ARCHITECTURE.md`](DATA-ARCHITECTURE.md) | Arquitectura de dados |
| [`SECURITY-ARCHITECTURE.md`](SECURITY-ARCHITECTURE.md) | Arquitectura de segurança |
| [`FRONTEND-ARCHITECTURE.md`](FRONTEND-ARCHITECTURE.md) | Arquitectura frontend |
| [`DEPLOYMENT-ARCHITECTURE.md`](DEPLOYMENT-ARCHITECTURE.md) | Arquitectura de deployment |
| [`INTEGRATIONS-ARCHITECTURE.md`](INTEGRATIONS-ARCHITECTURE.md) | Arquitectura de integrações |
| [`SOURCE-OF-TRUTH-STRATEGY.md`](SOURCE-OF-TRUTH-STRATEGY.md) | Estratégia Source of Truth |
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
| [`adr/006-graphql-protobuf-roadmap.md`](adr/006-graphql-protobuf-roadmap.md) | GraphQL/Protobuf roadmap |

---

## 4. AI

| Ficheiro | Descrição |
|---------|-----------|
| [`AI-ARCHITECTURE.md`](AI-ARCHITECTURE.md) | Arquitectura AI |
| [`AI-GOVERNANCE.md`](AI-GOVERNANCE.md) | Governança AI |
| [`AI-ASSISTED-OPERATIONS.md`](AI-ASSISTED-OPERATIONS.md) | Operações assistidas por AI |
| [`AI-DEVELOPER-EXPERIENCE.md`](AI-DEVELOPER-EXPERIENCE.md) | Developer Experience AI |

---

## 5. Contratos e Change Intelligence

| Ficheiro | Descrição |
|---------|-----------|
| [`CONTRACT-STUDIO-VISION.md`](CONTRACT-STUDIO-VISION.md) | Visão do Contract Studio |
| [`SERVICE-CONTRACT-GOVERNANCE.md`](SERVICE-CONTRACT-GOVERNANCE.md) | Governança de serviços e contratos |
| [`CHANGE-CONFIDENCE.md`](CHANGE-CONFIDENCE.md) | Change Confidence |

---

## 6. Frontend & UX

| Ficheiro | Descrição |
|---------|-----------|
| [`DESIGN-SYSTEM.md`](DESIGN-SYSTEM.md) | Design system |
| [`DESIGN.md`](DESIGN.md) | UX e design visual |
| [`BRAND-IDENTITY.md`](BRAND-IDENTITY.md) | Identidade visual |
| [`UX-PRINCIPLES.md`](UX-PRINCIPLES.md) | Princípios UX |
| [`PERSONA-MATRIX.md`](PERSONA-MATRIX.md) | Matriz de personas |
| [`PERSONA-UX-MAPPING.md`](PERSONA-UX-MAPPING.md) | Mapeamento UX por persona |
| [`I18N-STRATEGY.md`](I18N-STRATEGY.md) | Estratégia de internacionalização |

---

## 7. Engineering & Development

| Ficheiro | Descrição |
|---------|-----------|
| [`BACKEND-MODULE-GUIDELINES.md`](BACKEND-MODULE-GUIDELINES.md) | Guidelines de backend |
| [`GUIDELINE.md`](GUIDELINE.md) | Guidelines gerais |
| [`LOCAL-SETUP.md`](LOCAL-SETUP.md) | Setup local de desenvolvimento |
| [`ENVIRONMENT-VARIABLES.md`](ENVIRONMENT-VARIABLES.md) | Variáveis de ambiente |
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
| [`deployment/PHASE-7-DELIVERY-AND-DEPLOYMENT.md`](deployment/PHASE-7-DELIVERY-AND-DEPLOYMENT.md) | Delivery e deployment |

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
| [`observability/PHASE-6-OBSERVABILITY-COMPLETION.md`](observability/PHASE-6-OBSERVABILITY-COMPLETION.md) | Conclusão de observabilidade |
| [`observability/pipeline-validation-report.md`](observability/pipeline-validation-report.md) | Relatório de validação do pipeline |
| [`observability/troubleshooting.md`](observability/troubleshooting.md) | Troubleshooting |
| [`observability/configuration/`](observability/configuration/) | Configuração de observabilidade |
| [`observability/collection/`](observability/collection/) | Colecção (IIS/CLR, Kafka, K8s) |
| [`observability/providers/`](observability/providers/) | Providers (ClickHouse, Elastic) |

---

## 12. Telemetria — `docs/telemetry/`

| Ficheiro | Descrição |
|---------|-----------|
| [`telemetry/TELEMETRY-ARCHITECTURE.md`](telemetry/TELEMETRY-ARCHITECTURE.md) | Arquitectura de telemetria |

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

## 14. Legacy / Mainframe — `docs/legacy/`

| Ficheiro | Descrição |
|---------|-----------|
| [`LEGACY-MAINFRAME-WAVES.md`](LEGACY-MAINFRAME-WAVES.md) | **Documento mestre** — visão geral das 13 ondas |
| [`legacy/WAVE-00-STRATEGY.md`](legacy/WAVE-00-STRATEGY.md) – [`legacy/WAVE-12-SECURITY-READINESS.md`](legacy/WAVE-12-SECURITY-READINESS.md) | Ondas 0–12 |

---

*Última consolidação: 2026-04-07*
