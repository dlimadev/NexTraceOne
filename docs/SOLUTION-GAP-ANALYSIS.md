# SOLUTION-GAP-ANALYSIS.md

## Visão geral

Este documento detalha a aderência da solução atual à visão oficial do NexTraceOne,
classificando cada área como **OK**, **Reposicionar**, **Refatorar** ou **Criar do zero**.

---

## 1. Já alinhado (OK)

| Área | Estado | Notas |
|------|--------|-------|
| Arquitetura modular monolítica | OK | 7 bounded contexts com DDD, CQRS, Result pattern, strongly typed IDs |
| IdentityAccess | OK | RBAC, sessões, break glass, JIT, delegações, revisão de acessos |
| Catalog — EngineeringGraph | OK | ServiceAsset, ApiAsset, consumers, discovery, health, snapshots |
| Catalog — Contracts | OK | ContractVersion, diffs, artifacts, scorecard, provenance, locking |
| Catalog — DeveloperPortal | OK | Subscriptions, playground, code gen, analytics, saved searches |
| ChangeGovernance — ChangeIntelligence | OK | Release, blast radius, scoring, baseline, rollback, markers |
| ChangeGovernance — Workflow | OK | Templates, stages, approvals, evidence packs, SLA policies |
| ChangeGovernance — Promotion | OK | Promotion requests, gates, evaluation, freeze windows |
| ChangeGovernance — RulesetGovernance | OK | Rulesets, lint execution, bindings, default installs |
| CommercialGovernance | OK | Licensing, vendor ops, telemetry consent |
| OperationalIntelligence — Runtime | OK | Snapshots, baselines, drift detection, observability profiles |
| OperationalIntelligence — Cost | OK | Cost snapshots, attribution, trends, anomaly alerts |
| AIKnowledge — ExternalAI | OK | Providers, policies, consultations, knowledge capture |
| AIKnowledge — Orchestration | OK | Conversations, test generation, version suggestion, classification |
| AuditCompliance | OK | Audit log base |
| BuildingBlocks | OK | Core, Application, Infrastructure, Observability, Security |
| Frontend i18n | OK | 4 locales (en, pt-BR, pt-PT, es) |
| Frontend auth | OK | RBAC, ProtectedRoute, permissões visuais |
| 1109 testes | OK | Todos a passar sem falhas |

## 2. Reposicionar

| Área | De | Para | Impacto |
|------|----|----|---------|
| Sidebar — estrutura de navegação | Platform / Admin | Services / Contracts / Changes / Operations / AI Hub / Governance / Admin | Frontend — Sidebar.tsx, i18n |
| Sidebar — tagline | "Change Intelligence" | "Engineering Governance Platform" | i18n |
| Rota /graph | URL técnica | /services (Service Catalog) | App.tsx, Sidebar |
| Dashboard subtitle | "Overview of your change intelligence platform" | "Source of truth for services, contracts, changes and operational knowledge" | i18n |
| Nomes de menu | "Engineering Graph", "Releases" | "Service Catalog", "Change Intelligence" | Sidebar i18n |

## 3. Refatorar

| Área | O que refatorar | Prioridade |
|------|----------------|-----------|
| Dashboard | Adicionar segmentação por persona (Engineer, Tech Lead, Executive) | Alta |
| Contract Studio | Expandir Contracts page com editor de contratos assistido por IA | Alta |
| Source of Truth views | Criar vista consolidada de serviços+contratos+ownership+dependências | Alta |
| Observabilidade contextual | Vincular métricas a serviços, equipas e mudanças | Média |
| Persona-aware UI | Variar home, menu, widgets e nível de detalhe por persona | Média |

## 4. Criar do zero

| Área | Módulo produto | Bounded context backend | Prioridade |
|------|---------------|------------------------|-----------|
| Incidents & Mitigation | Operations | OperationalIntelligence (novo sub-context) | Alta |
| Runbooks | Operations | OperationalIntelligence (novo sub-context) | Média |
| AI Assistant (contextualizado) | AI Hub | AIKnowledge | Alta |
| Model Registry UI | AI Hub | AIKnowledge | Média |
| AI Policies UI | AI Hub | AIKnowledge | Média |
| Token & Budget Governance UI | AI Hub | AIKnowledge | Média |
| Reports por persona | Governance | Novo módulo ou query layer | Média |
| Risk Center | Governance | Novo módulo | Baixa |
| Compliance packs | Governance | AuditCompliance (expandir) | Baixa |
| FinOps contextual UI | Governance | OperationalIntelligence.Cost | Baixa |
| Contract Studio (editor) | Contracts | Catalog.Contracts (expandir) | Alta |
| IDE integrations | AI Hub | AIKnowledge (expandir) | Baixa |
| Executive views | Governance | Query layer cross-module | Baixa |

---

## Aderência por documento de referência

| Documento | Aderência | Gaps principais |
|-----------|-----------|----------------|
| PRODUCT-VISION.md | 70% | Falta Source of Truth views, persona-aware UI |
| PRODUCT-SCOPE.md | 65% | MVP core existe, faltam Operations e AI Hub UI |
| PERSONA-MATRIX.md | 30% | UI não segmentada por persona |
| MODULES-AND-PAGES.md | 55% | Navegação reestruturada; faltam Operations, AI Hub, Governance pages |
| SERVICE-CONTRACT-GOVERNANCE.md | 80% | Domain rico; falta Contract Studio UI e approval workflow UI |
| SOURCE-OF-TRUTH-STRATEGY.md | 60% | Dados existem; falta vista consolidada |
| CHANGE-CONFIDENCE.md | 85% | Release, blast radius, scoring, rollback implementados |
| AI-ASSISTED-OPERATIONS.md | 40% | Domain existe; falta UI e integração contextual |
| AI-GOVERNANCE.md | 40% | Policies e providers no domain; falta UI e token governance |
| DESIGN.md | 70% | Visual consistente; falta persona-aware layouts |
| FRONTEND-ARCHITECTURE.md | 80% | Feature-based, i18n, reusables; faltam novos feature modules |
| BACKEND-MODULE-GUIDELINES.md | 90% | DDD, CQRS, Result, strongly typed IDs seguidos |

---

## Sequência recomendada de refatoração

### Núcleo obrigatório (Fase 1)
1. ✅ Reestruturar navegação frontend — MODULES-AND-PAGES.md
2. Service Catalog — reposicionar "Engineering Graph" como "Service Catalog" + Source of Truth view
3. Contract Studio — expandir Contracts page com editor visual
4. Change Confidence — consolidar vista de confiança em mudanças

### Confiabilidade operacional (Fase 2)
5. Incidents & Mitigation — domain + application + UI
6. Runbooks — domain + application + UI
7. AI Assistant — UI contextualizada com serviços, contratos, incidentes
8. Operational Consistency — métricas operacionais por equipa

### Governança e otimização (Fase 3)
9. Reports por persona — views segmentadas
10. Compliance — expandir AuditCompliance
11. Risk Center — novo módulo
12. FinOps contextual — UI sobre OperationalIntelligence.Cost
13. AI Governance — Model Registry UI, policies UI, token budgets, IDE integrations
