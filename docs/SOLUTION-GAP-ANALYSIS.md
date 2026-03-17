# SOLUTION-GAP-ANALYSIS.md

## Visão geral

Este documento detalha a aderência da solução atual à visão oficial do NexTraceOne,
classificando cada área como **OK**, **Reposicionar**, **Refatorar** ou **Criar do zero**.

Última atualização: 2026-03-16 (Pós-finalização Onda 1)

> **Nota importante:** "OK" nesta análise significa que a *arquitetura e contratos* estão alinhados
> com a visão do produto. Não significa que a feature tem persistência real ou está pronta para
> produção. Para o estado real de persistência (real vs mock), consultar [REBASELINE.md](./REBASELINE.md).

---

## 1. Já alinhado (OK)

| Área | Estado | Notas |
|------|--------|-------|
| Arquitetura modular monolítica | OK | 8 bounded contexts com DDD, CQRS, Result pattern, strongly typed IDs |
| IdentityAccess | OK | RBAC, sessões, break glass, JIT, delegações, revisão de acessos (186 testes) |
| Catalog — ServiceCatalog | OK | Namespaces renomeados; API route /api/v1/catalog (430 testes) |
| Catalog — Contracts | OK | ContractVersion, diffs, artifacts, scorecard, provenance, locking, multi-protocol |
| Catalog — DeveloperPortal | OK | Subscriptions, playground, code gen, analytics, saved searches |
| Catalog — SourceOfTruth | OK | Vista consolidada serviços+contratos+ownership+coverage |
| ChangeGovernance — ChangeIntelligence | OK | Release, blast radius, scoring, baseline, rollback, markers (179 testes) |
| ChangeGovernance — Workflow | OK | Templates, stages, approvals, evidence packs, SLA policies |
| ChangeGovernance — Promotion | OK | Promotion requests, gates, evaluation, freeze windows. RequirePermission enforced |
| ChangeGovernance — RulesetGovernance | OK | Rulesets, lint execution, bindings, default installs. RequirePermission enforced |
| ~~CommercialGovernance~~ | REMOVED | Módulo removido no PR-17 — fora do núcleo do produto |
| OperationalIntelligence — Runtime | OK | Snapshots, baselines, drift detection, observability profiles. RequirePermission enforced |
| OperationalIntelligence — Cost | OK | Cost snapshots, attribution, trends, anomaly alerts. RequirePermission enforced |
| OperationalIntelligence — Incidents | OK | Domain, queries, correlation, evidence, mitigation (164 testes). **⚠️ Handlers usam dados mock — sem persistência real.** Ver REBASELINE.md |
| OperationalIntelligence — Reliability | OK | Team reliability, service reliability detail. **⚠️ Dados mock hardcoded — sem persistência real.** |
| AIKnowledge — ExternalAI | OK | Providers, policies, consultations, knowledge capture (75 testes) |
| AIKnowledge — Orchestration | OK | Conversations, test generation, version suggestion, classification |
| AIKnowledge — Governance | OK | Model registry, AI policies, access policies, token/budget governance, audit. RequirePermission enforced |
| AuditCompliance | OK | Audit log, trail, search, verify chain, compliance report. RequirePermission enforced |
| Governance | OK | Reports por persona, Risk Center, Compliance, FinOps contextual. RequirePermission enforced |
| BuildingBlocks | OK | Core, Application, Infrastructure, Observability, Security (103 testes) |
| Frontend — Sidebar | OK | 9 seções alinhadas com MODULES-AND-PAGES.md, persona-aware ordering |
| Frontend — CommandPalette | OK | Espelha sidebar com todos os módulos do produto |
| Frontend — i18n | OK | 4 locales (en, pt-BR, pt-PT, es); 1,650+ chaves; zero hardcoded strings visíveis nas áreas críticas |
| Frontend — auth | OK | RBAC, ProtectedRoute, persona derivation, permissões visuais |
| Frontend — Persona-aware UX | OK | 7 personas, PersonaContext, config por persona, home adaptativa |
| Frontend — Dark Enterprise Theme | OK | Identidade visual única |
| Frontend — AI Hub | OK | AI Assistant, Model Registry, AI Policies, Token Budget, AI Audit pages |
| Frontend — Operations | OK | Incidents, Runbooks, Reliability pages com domain connection |
| Frontend — Governance | OK | Reports (persona-aware), Risk Center, Compliance, FinOps pages |
| 1,447 testes backend | OK | Todos a passar sem falhas (atualizado pós-PR-16) |
| Frontend — 23 testes core flows | OK | SoT Explorer (5), IncidentDetail (8), IncidentsPage (5), AiAssistant (5) |
| Frontend — Navegação cross-entity | OK | Links bidirecionais: serviço ↔ contrato ↔ change ↔ incident |
| Frontend — Error/Loading states | OK | Error states em ChangeCatalog, SoT Explorer, Dashboard; Loading em todas as páginas |
| Segurança — RequirePermission | OK | Todas as 22 endpoint modules com RequirePermission enforced |

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
| ✅ EngineeringGraphPage.tsx (nome) | "EngineeringGraphPage" | "ServiceCatalogPage" | Concluído |
| ✅ Backend namespaces (96 ficheiros) | NexTraceOne.EngineeringGraph.* | NexTraceOne.Catalog.*.Graph.* | Concluído — Fase 3 |
| ✅ API route group | /api/v1/engineeringgraph | /api/v1/catalog | Concluído — Fase 3 |
| ✅ DbContext | EngineeringGraphDbContext | CatalogGraphDbContext | Concluído — Fase 3 |
| ✅ Endpoint module | EngineeringGraphEndpointModule | ServiceCatalogEndpointModule | Concluído — Fase 3 |
| ✅ Error class | EngineeringGraphErrors | CatalogGraphErrors | Concluído — Fase 3 |
| ✅ DI method | AddEngineeringGraphModule | AddCatalogGraphModule | Concluído — Fase 3 |
| ✅ Cross-module interface | IEngineeringGraphModule | ICatalogGraphModule | Concluído — Fase 3 |
| ✅ Module service | EngineeringGraphModuleService | CatalogGraphModuleService | Concluído — Fase 3 |
| ✅ Permission keys | engineering-graph:assets:* | catalog:assets:* | Concluído — Fase 3 |
| ✅ Connection string key | EngineeringGraphDatabase | CatalogDatabase | Concluído — Fase 3 |
| ✅ Frontend API routes | /engineeringgraph/* | /catalog/* | Concluído — Fase 3 |
| ✅ i18n subtitles | Linguagem genérica | Source of Truth, governança, auditoria | Concluído — Fase 3 |

## 3. Refatorar

| Área | O que refatorar | Prioridade | Estado |
|------|----------------|-----------|--------|
| ✅ Dashboard | Segmentação por persona (Engineer, Tech Lead, Executive) | Alta | Concluído |
| ✅ Source of Truth views | Vista consolidada de serviços+contratos+ownership+dependências | Alta | Concluído |
| ✅ Persona-aware UI | Variar home, menu, widgets e nível de detalhe por persona | Média | Concluído |
| Contract Studio | Expandir Contracts page com editor de contratos assistido por IA | Alta | Parcial — Backend pronto, UI básica |
| Observabilidade contextual | Vincular métricas a serviços, equipas e mudanças | Média | Parcial — Runtime/Cost implementados |
| Ingestion.Api (stubs) | Implementar endpoints ou integrar no ApiHost | Baixa | Pendente |

## 4. Criar do zero

| Área | Módulo produto | Bounded context backend | Prioridade | Estado |
|------|---------------|------------------------|-----------|--------|
| ✅ Incidents & Mitigation | Operations | OperationalIntelligence.Incidents | Alta | Concluído — domain, application, API, UI, 58 testes |
| ✅ Runbooks | Operations | OperationalIntelligence | Média | Concluído — UI com mock data |
| ✅ AI Assistant (contextualizado) | AI Hub | AIKnowledge | Alta | Concluído — UI + governance |
| ✅ Model Registry UI | AI Hub | AIKnowledge.Governance | Média | Concluído |
| ✅ AI Policies UI | AI Hub | AIKnowledge.Governance | Média | Concluído |
| ✅ Token & Budget Governance UI | AI Hub | AIKnowledge.Governance | Média | Concluído |
| ✅ Reports por persona | Governance | Governance module | Média | Concluído — persona-aware views |
| ✅ Risk Center | Governance | Governance module | Baixa | Concluído |
| ✅ Compliance | Governance | Governance module | Baixa | Concluído |
| ✅ FinOps contextual UI | Governance | Governance module | Baixa | Concluído |
| ✅ Reliability (Team + Service) | Operations | OperationalIntelligence.Reliability | Alta | Concluído — domain, application, API, UI, 37 testes |
| Contract Studio (editor avançado) | Contracts | Catalog.Contracts (expandir) | Alta | Parcial — DEFER para roadmap |
| IDE integrations | AI Hub | AIKnowledge (expandir) | Baixa | DEFER para roadmap |
| Executive views (avançado) | Governance | Query layer cross-module | Baixa | Parcial — ReportsPage com persona-aware |

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
| VersionCommunication domain (6 files + tests) | REMOVE | Domínio completamente órfão — zero consumidores em Application/Infrastructure/API | -13 testes (de 1095 para 1082) |
| Placeholder tests redundantes (10 files) | REMOVE | Assert.True(true) em diretórios com testes reais — inflavam count sem testar nada | -10 testes (de 1082 para 1072) |
| EngineeringGraphPage → ServiceCatalogPage | RENAME | Página servia rota /services mas nome antigo causava confusão semântica | 0 — backward-compat mantido |
| engineeringGraph.ts → serviceCatalog.ts | RENAME | API client module renomeado para alinhar com produto | 0 — URLs backend inalterados |
| i18n engineeringGraph → serviceCatalog | RENAME | Chaves i18n renomeadas; title "Engineering Graph" → "Service Catalog" | 0 — 4 locales atualizados |

### Fase 3 — Renaming e Reposicionamento Semântico

| Item | Classificação | Motivo | Impacto |
|------|--------------|--------|---------|
| Backend namespaces NexTraceOne.EngineeringGraph.* (96 ficheiros, 451 linhas) | RENAME | Namespaces não alinhados com o nome do projeto (Catalog) nem com produto | 0 — testes mantidos |
| EngineeringGraphEndpointModule → ServiceCatalogEndpointModule | RENAME | Módulo de endpoint com nome antigo do produto | 0 — rotas atualizadas |
| EngineeringGraphDbContext → CatalogGraphDbContext | RENAME | DbContext com nome que causava confusão semântica | 0 — migrations preservadas |
| EngineeringGraphErrors → CatalogGraphErrors | RENAME | Classe de erros do domínio com nome antigo | 0 — error codes atualizados |
| IEngineeringGraphModule → ICatalogGraphModule | RENAME | Interface de contrato cross-módulo com nome antigo | 0 — backward-compat |
| AddEngineeringGraphModule → AddCatalogGraphModule | RENAME | Método DI com nome antigo | 0 |
| Permission engineering-graph:assets:* → catalog:assets:* | RENAME | Permissões com nome antigo afetavam RBAC semântico | 0 — frontend+backend |
| Connection string EngineeringGraphDatabase → CatalogDatabase | RENAME | Chave de connection string desalinhada com módulo | 0 |
| API route /api/v1/engineeringgraph → /api/v1/catalog | RENAME | URL pública com nome antigo do produto | 0 — frontend atualizado |
| Frontend API routes serviceCatalog.ts | RENAME | Todas as URLs atualizadas de /engineeringgraph/ para /catalog/ | 0 |
| Frontend permission keys | RENAME | engineering-graph:assets:* → catalog:assets:* em permissions.ts, Sidebar, CommandPalette | 0 |
| i18n subtitles (4 locales) | IMPROVE | Reforçar linguagem de Source of Truth, governança e auditoria | 0 |

---

## Aderência por documento de referência

| Documento | Aderência | Gaps principais |
|-----------|-----------|----------------|
| PRODUCT-VISION.md | 90% | Falta Contract Studio editor avançado, IDE integrations |
| PRODUCT-SCOPE.md | 85% | Core implementado; Operations e AI Hub com UI funcional |
| PERSONA-MATRIX.md | 80% | UI segmentada por persona; PersonaContext ativo; faltam customizações avançadas |
| MODULES-AND-PAGES.md | 90% | Navegação alinhada; todas as páginas core implementadas |
| SERVICE-CONTRACT-GOVERNANCE.md | 85% | Domain rico; Contract Studio parcial; approval workflow backend pronto |
| SOURCE-OF-TRUTH-STRATEGY.md | 85% | Vista consolidada implementada; search e explorer funcionais |
| CHANGE-CONFIDENCE.md | 90% | Release, blast radius, scoring, rollback, timeline implementados |
| AI-ASSISTED-OPERATIONS.md | 70% | Governance completa; AI Assistant UI funcional; falta grounding profundo |
| AI-GOVERNANCE.md | 80% | Model registry, policies, token budget, audit implementados |
| DESIGN.md | 90% | Visual enterprise dark theme consistente; persona-aware layouts |
| FRONTEND-ARCHITECTURE.md | 95% | Feature-based, i18n maduro, reusáveis, persona-aware, dark theme consistente |
| BACKEND-MODULE-GUIDELINES.md | 95% | DDD, CQRS, Result, RequirePermission enforced em todos os endpoints |
| SECURITY-ARCHITECTURE.md | 85% | RequirePermission em todos os endpoints; RBAC funcional; falta encryption at rest |
| I18N-STRATEGY.md | 95% | 4 locales, 1650+ chaves, zero hardcoded strings nas áreas críticas |

---

## Inventário de itens pendentes (DEPRECATE/REFACTOR)

| Item | Classificação | Motivo | Risco |
|------|--------------|--------|-------|
| Ingestion.Api endpoints (5 stubs) | REFACTOR | Endpoints retornam strings placeholder | Baixo — projeto separado |
| AIKnowledge — AiOrchestration endpoints (stub) | REFACTOR | EndpointModule com TODO, sem endpoints mapeados | Baixo — governance endpoints funcionais |
| AIKnowledge — ExternalAI endpoints (stub) | REFACTOR | EndpointModule com TODO, sem endpoints mapeados | Baixo — providers geridos via governance |
| sanitize.ts, navigation.ts (utils) | KEEP | Utilitários de segurança com testes — devem ser integrados | Nenhum |
| Placeholder tests restantes (5 projetos) | KEEP | São o único teste nos projetos AuditCompliance, E2E, Integration, Security, Infrastructure — manter até testes reais | Nenhum |
| Frontend — error/loading state patterns | DOCUMENT | Consolidado: EmptyState para entidade não encontrada, inline text para loading, Card com AlertTriangle para erros na DashboardPage | Resolvido |
| Governance module — mock data | REFACTOR | 4 endpoints retornam dados estáticos; conectar a queries cross-module | Médio |

---

## Sequência recomendada de refatoração

### Núcleo obrigatório (Fase 1 — concluída)
1. ✅ Reestruturar navegação frontend — MODULES-AND-PAGES.md
2. ✅ Alinhar narrativa de produto — auth.tagline, CommandPalette, sidebar
3. ✅ Remover código morto comprovado — Notifications, StatusPill, Skeleton, i18n stale keys
4. ✅ Remover domínio órfão VersionCommunication — zero consumidores
5. ✅ Renomear EngineeringGraphPage → ServiceCatalogPage + i18n namespace
6. ✅ Limpar placeholder tests redundantes — 10 ficheiros removidos

### Renaming e Reposicionamento (Fase 3 — concluída)
7. ✅ Renomear namespaces backend NexTraceOne.EngineeringGraph.* → NexTraceOne.Catalog.*.Graph.* (96 ficheiros)
8. ✅ Renomear API route /api/v1/engineeringgraph → /api/v1/catalog
9. ✅ Renomear DbContext, EndpointModule, ErrorClass, DI, Interface, Service
10. ✅ Renomear permission keys engineering-graph:* → catalog:*
11. ✅ Atualizar frontend API routes e permission references
12. ✅ Reforçar i18n subtitles para linguagem de governança e Source of Truth

### Persona-aware UX e Source of Truth (Fase 4 — concluída)
13. ✅ PersonaContext com 7 personas e segmentação de menu/home/widgets
14. ✅ Source of Truth Explorer com search e vista consolidada
15. ✅ Service e Contract Source of Truth detail pages

### Camada Operacional (Fases 5-6 — concluídas)
16. ✅ Reliability — domain, application, API, endpoints, UI (37 testes)
17. ✅ Incidents — domain, application, API, endpoints, UI (58 testes)
18. ✅ Runbooks — UI com mock data

### AI Governance & Developer Acceleration (Fase 7 — concluída)
19. ✅ AI Governance endpoints com RequirePermission
20. ✅ Model Registry, AI Policies, Token Budget, AI Audit pages
21. ✅ AI Assistant page

### UX Refinement (Fase 8 — concluída)
22. ✅ Dark enterprise theme consolidado
23. ✅ Login, Home, e módulos com mesma família visual
24. ✅ i18n maduro com 4 locales

### Reporting & Governance (Fase 9 — concluída)
25. ✅ Governance module com 4 endpoints
26. ✅ Reports persona-aware, Risk Center, Compliance, FinOps pages
27. ✅ Audit page funcional

### Hardening & Refoundation Closure (Fase Final — concluída)
28. ✅ i18n: Eliminar todas as hardcoded strings residuais (14 corrigidas)
29. ✅ Segurança: RequirePermission em todos os 40 endpoints desprotegidos
30. ~~Visual: VendorLicensingPage~~ — removida junto com módulo CommercialGovernance
31. ✅ Visual: Health status badges corrigidos para dark theme
32. ✅ Documentação: SOLUTION-GAP-ANALYSIS.md atualizado

### Próximos épicos pós-refoundation (Recomendação)
33. Contract Studio editor avançado com IA assistida
34. Change Confidence — consolidar vista de confiança em mudanças
35. AI grounding profundo — conectar IA a serviços/contratos/incidentes/runbooks reais
36. Governance module — conectar a queries cross-module reais
37. Frontend — consolidar padrão error/loading state com componente PageErrorState
38. IDE Extensions — VS Code e Visual Studio extensions
39. Encryption at rest para dados sensíveis
40. Testes de integração e E2E para fluxos críticos

---

## Fase Final — Hardening Gap Analysis

### Diagnóstico por área

#### Produto
- ✅ Identidade como Source of Truth para serviços, contratos, mudanças e conhecimento operacional
- ✅ Contratos como first-class citizens com governance, versioning e diff
- ✅ 8 bounded contexts coesos com DDD/CQRS
- ⚠️ Governance module retorna dados estáticos (mock) — necessita conexão a queries reais

#### Frontend
- ✅ i18n completo — zero hardcoded strings visíveis ao utilizador
- ✅ Dark enterprise theme consistente em todas as páginas
- ✅ Persona-aware navigation e home page
- ✅ 43 páginas implementadas em 9 feature modules
- ⚠️ Error/loading state patterns divergem entre EmptyState e inline text
- ⚠️ Algumas páginas com mock data não têm loading/error handling

#### Backend
- ✅ RequirePermission enforced em todas as 22 endpoint modules (130+ endpoints)
- ✅ DI pattern consistente em 56 ficheiros
- ✅ VSA pattern seguido em todas as features
- ✅ 1,159 testes a passar sem falhas
- ⚠️ AiOrchestration e ExternalAI endpoint modules são stubs com TODO
- ⚠️ NuGet NU1510 warnings (packages que não serão pruned)

#### Segurança
- ✅ RBAC com permissões granulares em todos os endpoints
- ✅ PersonaContext derivado de roles
- ⚠️ Falta encryption at rest
- ⚠️ Falta validação de tokens/sessions mais robusta

#### IA
- ✅ Model registry, policies, token/budget governance implementados
- ✅ AI Governance com RequirePermission
- ⚠️ AI Assistant usa mock data — falta grounding em domínio real
- ⚠️ AiOrchestration endpoints não mapeados

#### Documentação
- ✅ SOLUTION-GAP-ANALYSIS.md atualizado para fase final
- ✅ Docs de referência existentes (22 documentos)
- ⚠️ Alguns docs focam nas fases iniciais e não refletem implementação completa

#### Testes
- ✅ 1,447 testes backend a passar (atualizado pós-PR-16)
- ✅ Cobertura em todos os módulos core (Catalog, ChangeGovernance, IdentityAccess, OperationalIntelligence, AIKnowledge)
- ⚠️ Frontend tests limitados
- ⚠️ E2E/Integration tests são placeholder

### Classificação de pendências por severidade

| Severidade | Item | Classificação |
|------------|------|--------------|
| **Alta** | **Incidents/Automation/Reliability handlers mock — sem persistência** | **REFACTOR** |
| **Alta** | **IncidentsPage frontend usa mock inline** | **REFACTOR** |
| Média | Governance module mock data | REFACTOR |
| Média | AI Assistant grounding validation E2E | REFACTOR |
| Média | Error/loading state pattern consolidation | REFACTOR |
| Baixa | AiOrchestration/ExternalAI stubs | REFACTOR |
| Baixa | Ingestion.Api stubs | REFACTOR |
| Baixa | Frontend E2E tests | TEST |
| Baixa | Encryption at rest | FIX |
| Baixa | NU1510 NuGet warnings | FIX |
| Info | Docs alignment with final state | DOCUMENT |
