# SOLUTION-GAP-ANALYSIS.md

## Visão geral

Este documento detalha a aderência da solução atual à visão oficial do NexTraceOne,
classificando cada área como **OK**, **Reposicionar**, **Refatorar** ou **Criar do zero**.

Última atualização: 2026-03-14 (limpeza arquitetural executada)

---

## 1. Já alinhado (OK)

| Área | Estado | Notas |
|------|--------|-------|
| Arquitetura modular monolítica | OK | 7 bounded contexts com DDD, CQRS, Result pattern, strongly typed IDs |
| IdentityAccess | OK | RBAC, sessões, break glass, JIT, delegações, revisão de acessos (186 testes) |
| Catalog — ServiceCatalog (ex-EngineeringGraph) | OK | ServiceAsset, ApiAsset, consumers, discovery, health, snapshots (388 testes) |
| Catalog — Contracts | OK | ContractVersion, diffs, artifacts, scorecard, provenance, locking |
| Catalog — DeveloperPortal | OK | Subscriptions, playground, code gen, analytics, saved searches |
| ChangeGovernance — ChangeIntelligence | OK | Release, blast radius, scoring, baseline, rollback, markers (171 testes) |
| ChangeGovernance — Workflow | OK | Templates, stages, approvals, evidence packs, SLA policies |
| ChangeGovernance — Promotion | OK | Promotion requests, gates, evaluation, freeze windows |
| ChangeGovernance — RulesetGovernance | OK | Rulesets, lint execution, bindings, default installs |
| CommercialGovernance | OK | Licensing, vendor ops, telemetry consent (124 testes) |
| OperationalIntelligence — Runtime | OK | Snapshots, baselines, drift detection, observability profiles (71 testes) |
| OperationalIntelligence — Cost | OK | Cost snapshots, attribution, trends, anomaly alerts |
| AIKnowledge — ExternalAI | OK | Providers, policies, consultations, knowledge capture (47 testes) |
| AIKnowledge — Orchestration | OK | Conversations, test generation, version suggestion, classification |
| AuditCompliance | OK | Audit log base |
| BuildingBlocks | OK | Core, Application, Infrastructure, Observability, Security (118 testes) |
| Frontend — Sidebar | OK | 8 seções alinhadas com MODULES-AND-PAGES.md |
| Frontend — CommandPalette | OK | Espelha sidebar com todos os módulos do produto |
| Frontend — i18n | OK | 4 locales (en, pt-BR, pt-PT, es); auth.tagline alinhado |
| Frontend — auth | OK | RBAC, ProtectedRoute, permissões visuais |
| 1095 testes backend | OK | Todos a passar sem falhas |

## 2. Reposicionar

| Área | De | Para | Estado |
|------|----|----|--------|
| ✅ Sidebar — estrutura | Platform / Admin | 8 seções (MODULES-AND-PAGES.md) | Concluído |
| ✅ Sidebar — tagline | "Change Intelligence" | "Engineering Governance Platform" | Concluído |
| ✅ Login — auth.tagline | "Sovereign Change Intelligence Platform" | "Sovereign Engineering Governance Platform" | Concluído |
| ✅ Rota /graph | URL técnica | /services (+ redirect backward-compat) | Concluído |
| ✅ Dashboard subtitle | "change intelligence platform" | "Source of truth..." | Concluído |
| ✅ Nomes de menu | "Engineering Graph", "Releases" | "Service Catalog", "Change Intelligence" | Concluído |
| ✅ CommandPalette | Itens antigos (/graph, labels legados) | Espelha sidebar com 8 seções | Concluído |
| EngineeringGraphPage.tsx (nome) | "EngineeringGraphPage" | "ServiceCatalogPage" | Pendente — renomear ficheiro |

## 3. Refatorar

| Área | O que refatorar | Prioridade |
|------|----------------|-----------|
| Dashboard | Adicionar segmentação por persona (Engineer, Tech Lead, Executive) | Alta |
| Contract Studio | Expandir Contracts page com editor de contratos assistido por IA | Alta |
| Source of Truth views | Criar vista consolidada de serviços+contratos+ownership+dependências | Alta |
| Observabilidade contextual | Vincular métricas a serviços, equipas e mudanças | Média |
| Persona-aware UI | Variar home, menu, widgets e nível de detalhe por persona | Média |
| VersionCommunication (domínio órfão) | Conectar ao application/infrastructure layer ou remover | Média |
| Ingestion.Api (stubs) | Implementar endpoints ou integrar no ApiHost | Baixa |

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

## 5. Código removido (limpeza executada)

| Item | Classificação | Motivo | Impacto |
|------|--------------|--------|---------|
| BuildingBlocks.Core/Notifications/ (12 entidades) | REMOVE | Zero consumidores — never implemented beyond domain models | 0 — nenhum módulo usava |
| BuildingBlocks.Application/Notifications/ (6 interfaces) | REMOVE | Zero implementações — planned but never connected | 0 |
| Tests/NotificationModelsTests.cs (14 testes) | REMOVE | Testavam código removido | -14 testes (de 1109 para 1095) |
| Frontend StatusPill.tsx | REMOVE | Zero imports em todo o frontend | 0 |
| Frontend Skeleton.tsx (4 componentes) | REMOVE | Zero imports em todo o frontend | 0 |
| i18n sidebar.releases | REMOVE | Substituído por sidebar.changeIntelligence | 0 |
| i18n sidebar.engineeringGraph | REMOVE | Substituído por sidebar.serviceCatalog | 0 |
| i18n sidebar.contracts | REMOVE | Substituído por sidebar.apiContracts | 0 |
| i18n sidebar.sectionPlatform | REMOVE | Substituído por seções granulares (Services, Contracts, etc.) | 0 |

---

## Aderência por documento de referência

| Documento | Aderência | Gaps principais |
|-----------|-----------|----------------|
| PRODUCT-VISION.md | 75% | Falta Source of Truth views, persona-aware UI |
| PRODUCT-SCOPE.md | 70% | MVP core existe, faltam Operations e AI Hub UI |
| PERSONA-MATRIX.md | 30% | UI não segmentada por persona |
| MODULES-AND-PAGES.md | 70% | Navegação alinhada; placeholder pages criados; faltam implementações reais |
| SERVICE-CONTRACT-GOVERNANCE.md | 80% | Domain rico; falta Contract Studio UI e approval workflow UI |
| SOURCE-OF-TRUTH-STRATEGY.md | 60% | Dados existem; falta vista consolidada |
| CHANGE-CONFIDENCE.md | 85% | Release, blast radius, scoring, rollback implementados |
| AI-ASSISTED-OPERATIONS.md | 40% | Domain existe; falta UI e integração contextual |
| AI-GOVERNANCE.md | 40% | Policies e providers no domain; falta UI e token governance |
| DESIGN.md | 75% | Visual consistente; tagline corrigido; falta persona-aware layouts |
| FRONTEND-ARCHITECTURE.md | 85% | Feature-based, i18n, reusables; CommandPalette alinhado; dead code removido |
| BACKEND-MODULE-GUIDELINES.md | 90% | DDD, CQRS, Result, strongly typed IDs seguidos; dead code removido |

---

## Inventário de itens pendentes (DEPRECATE/REFACTOR)

| Item | Classificação | Motivo | Risco |
|------|--------------|--------|-------|
| VersionCommunication (ChangeGovernance.Domain) | DEPRECATE | Domínio órfão sem Application/Infrastructure/API layers | Baixo — isolado no domain |
| Ingestion.Api endpoints (5 stubs) | REFACTOR | Endpoints retornam strings placeholder | Baixo — projeto separado |
| AIKnowledge features (16 handlers) | REFACTOR | Handlers com TODO stubs, sem implementação real | Médio — domínio está bom |
| EngineeringGraphPage.tsx (nome) | RENAME | Ficheiro ainda chamado "EngineeringGraph" mas rota é /services | Baixo — apenas naming |
| sanitize.ts, navigation.ts (utils) | KEEP | Utilitários de segurança sem consumidores mas com testes — devem ser integrados | Nenhum |
| Placeholder tests (Assert.True(true)) | REFACTOR | 5+ projetos de teste só com placeholder | Baixo — não dão falsos positivos perigosos |

---

## Sequência recomendada de refatoração

### Núcleo obrigatório (Fase 1)
1. ✅ Reestruturar navegação frontend — MODULES-AND-PAGES.md
2. ✅ Alinhar narrativa de produto — auth.tagline, CommandPalette, sidebar
3. ✅ Remover código morto comprovado — Notifications, StatusPill, Skeleton, i18n stale keys
4. Service Catalog — reposicionar EngineeringGraphPage como ServiceCatalogPage + Source of Truth view
5. Contract Studio — expandir Contracts page com editor visual
6. Change Confidence — consolidar vista de confiança em mudanças

### Confiabilidade operacional (Fase 2)
7. Incidents & Mitigation — domain + application + UI
8. Runbooks — domain + application + UI
9. AI Assistant — UI contextualizada com serviços, contratos, incidentes
10. Operational Consistency — métricas operacionais por equipa

### Governança e otimização (Fase 3)
11. Reports por persona — views segmentadas
12. Compliance — expandir AuditCompliance
13. Risk Center — novo módulo
14. FinOps contextual — UI sobre OperationalIntelligence.Cost
15. AI Governance — Model Registry UI, policies UI, token budgets, IDE integrations
