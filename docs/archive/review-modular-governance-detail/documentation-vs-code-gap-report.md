# Documentação vs Código — Relatório de Divergências — NexTraceOne

> **Data:** 2026-03-24  
> **Tipo:** Auditoria Estrutural — Parte 5  
> **Fonte de verdade:** Código do repositório cruzado com documentação existente

---

## Resumo

| Tipo de Divergência | Quantidade |
|---------------------|-----------|
| Módulos documentados que não existem no código | 0 |
| Módulos existentes sem documentação dedicada | 4 |
| Páginas documentadas que não existem | ~5 |
| Páginas existentes sem documentação | ~20 |
| Documentação atrasada | ~15 documentos |
| Documentação contraditória | ~5 instâncias |
| Documentação incompleta | ~10 documentos |
| Documentação excessivamente otimista | ~8 instâncias |
| Código sem documentação | Significativo |
| Documentação sem aderência ao produto atual | ~12 documentos |

---

## 1. Módulos Documentados vs Existentes

### ✅ Módulos Corretamente Documentados

| Módulo Backend | Documentação Principal | Alinhamento |
|----------------|----------------------|-------------|
| Identity & Access | assessment/08-SECURITY-AUDIT.md, security/*.md | ✅ Bom |
| Catalog | assessment/05-BACKEND-AUDIT.md, user-guide/service-catalog.md | ✅ Bom |
| Change Governance | CHANGE-CONFIDENCE.md, user-guide/change-governance.md | ✅ Bom |
| Operational Intelligence | user-guide/operations.md, reliability/*.md | ✅ Parcial |
| AI Knowledge | AI-ARCHITECTURE.md, AI-GOVERNANCE.md, aiknowledge/*.md | ✅ Bom (apesar de backend ser parcial) |
| Governance | assessment/02-FUNCTIONAL-MODULE-MAP.md | ✅ Parcial |
| Audit & Compliance | assessment/05-BACKEND-AUDIT.md | ✅ Parcial |

### ❌ Módulos sem Documentação Dedicada

| Módulo | Código Existente | Documentação | Gap |
|--------|-----------------|--------------|-----|
| **Configuration** | 345+ definições, 251 testes, 8 fases de seed | Apenas fragmentado em 35 ficheiros execution/CONFIGURATION-* | **Sem documento unificado** de referência |
| **Notifications** | 3 entidades, API, 4 hooks frontend | 12 ficheiros execution/NOTIFICATIONS-* | **Sem documento de visão ou arquitetura unificada** |
| **Integrations** | 4 páginas frontend, endpoints backend no módulo Governance | Nenhum | **Zero documentação dedicada** |
| **Product Analytics** | 5 páginas frontend, endpoints analytics no módulo Governance | Nenhum | **Zero documentação dedicada** |

### ❌ Building Blocks sem Documentação

| Componente | Código | Documentação |
|-----------|--------|-------------|
| BuildingBlocks.Core | Result<T>, entidades base, especificações | Zero |
| BuildingBlocks.Application | MediatR, CQRS | Zero |
| BuildingBlocks.Infrastructure | RepositoryBase, OutboxEventBus, TenantRlsInterceptor, AuditInterceptor | Menções parciais em SECURITY.md e ARCHITECTURE-OVERVIEW.md |
| BuildingBlocks.Security | AesGcmEncryptor, PermissionAuthorizationHandler | Menções parciais em security/*.md |
| BuildingBlocks.Observability | Serilog, health checks, tracing | Menções parciais em OBSERVABILITY-STRATEGY.md |

---

## 2. Páginas Documentadas vs Existentes

### Páginas Documentadas que NÃO Existem como Esperado

| Página Documentada | Documento Fonte | Realidade no Código |
|-------------------|----------------|-------------------|
| "Contract Governance" como funcionalidade integrada | SERVICE-CONTRACT-GOVERNANCE.md | Página existe (`ContractGovernancePage.tsx`) mas **não está roteada** no App.tsx |
| "Spectral Rulesets" como funcionalidade core | CONTRACT-STUDIO-VISION.md | Página existe (`SpectralRulesetManagerPage.tsx`) mas **não está roteada** |
| "Canonical Entities" como feature | CONTRACT-STUDIO-VISION.md | Página existe (`CanonicalEntityCatalogPage.tsx`) mas **não está roteada** |
| "Contract Portal" como portal público | Referências em PRODUCT-SCOPE.md | Página existe (`ContractPortalPage.tsx`) mas **órfã** — sem rota nem menu |
| IDE Extensions reais (VS Code, Visual Studio) | AI-DEVELOPER-EXPERIENCE.md | Página `IdeIntegrationsPage.tsx` existe mas **sem extensões reais** implementadas |

### Páginas Existentes sem Documentação Relevante

| Página | Módulo | Observação |
|--------|--------|------------|
| ProductAnalyticsOverviewPage | product-analytics | Zero documentação |
| ModuleAdoptionPage | product-analytics | Zero documentação |
| PersonaUsagePage | product-analytics | Zero documentação |
| JourneyFunnelPage | product-analytics | Zero documentação |
| ValueTrackingPage | product-analytics | Zero documentação |
| AdvancedConfigurationConsolePage | configuration | Apenas relatório Phase-8 |
| BenchmarkingPage | governance | Sem documentação específica |
| DelegatedAdminPage | governance | Sem documentação específica |
| EnterpriseControlsPage | governance | Sem documentação específica |
| EvidencePackagesPage | governance | Sem documentação específica |
| MaturityScorecardsPage | governance | Sem documentação específica |
| IngestionExecutionsPage | integrations | Sem documentação específica |
| IngestionFreshnessPage | integrations | Sem documentação específica |
| ConnectorDetailPage | integrations | Sem documentação específica |
| AutomationAdminPage | operations | Sem documentação específica |
| AutomationWorkflowDetailPage | operations | Sem documentação específica |
| ServiceReliabilityDetailPage | operations | Sem documentação específica |
| EnvironmentComparisonPage | operations | Referenciada em ENVIRONMENT-COMPARISON-ARCHITECTURE.md |
| PlatformOperationsPage | operations | Sem documentação específica |
| GlobalSearchPage | catalog | Sem documentação específica |

---

## 3. Arquitetura Descrita vs Implementada

### 3.1 Alinhamento Geral

| Aspecto | Documentação | Implementação | Alinhamento |
|---------|-------------|---------------|-------------|
| Modular Monolith | ARCHITECTURE-OVERVIEW.md | 9 módulos reais, 71 projetos | ✅ Alinhado |
| DDD + CQRS | BACKEND-MODULE-GUIDELINES.md | MediatR, Domain/Application/Infrastructure | ✅ Alinhado |
| Multi-database | DATA-ARCHITECTURE.md | 16+ DbContexts | ⚠️ Docs descrevem 5 entidades; real tem 16+ |
| Multi-tenancy RLS | SECURITY.md | TenantRlsInterceptor | ✅ Alinhado |
| Persona-aware UX | PERSONA-MATRIX.md | 7 personas no AppSidebar | ✅ Alinhado |
| i18n | I18N-STRATEGY.md | 4 locales, ~639 KB | ⚠️ Docs superficiais |
| AI Governance | AI-GOVERNANCE.md | AiGovernanceDbContext, políticas, budgets | ⚠️ Docs prometem mais do que implementado (~20-25%) |
| Event Bus | ADR-003-event-bus-limitations.md | InProcessEventBus + OutboxEventBus | ✅ Alinhado (limitação documentada) |

### 3.2 Divergências Significativas

| Aspecto | Documentação Diz | Código Mostra |
|---------|-----------------|---------------|
| **Número de módulos** | DOMAIN-BOUNDARIES.md lista 6 bounded contexts | Existem **9 módulos** reais (inclui Configuration, Notifications, Governance) |
| **Databases** | DATA-ARCHITECTURE.md descreve 5 entidades core | Existem **16+ DbContexts** distribuídos por módulos |
| **Frontend** | MODULES-AND-PAGES.md lista ~30 páginas | Existem **105 páginas** reais |
| **AI maturity** | AI-ARCHITECTURE.md descreve sistema completo | Backend AI está a **20-25%** conforme AI-LOCAL-IMPLEMENTATION-AUDIT.md |
| **IDE Extensions** | AI-DEVELOPER-EXPERIENCE.md descreve VS Code e Visual Studio extensions | Apenas **página de UI** existe, sem extensões reais |
| **Contract Studio** | CONTRACT-STUDIO-VISION.md descreve modos completos | DraftStudioPage existe mas sem visão completa implementada |

---

## 4. Classificação dos Gaps

### 4.1 Documentação Atrasada

Documentação que **era correta** num ponto mas o código evoluiu e ela não acompanhou.

| Documento | Gap |
|-----------|-----|
| `ARCHITECTURE-OVERVIEW.md` | 19 linhas — não reflete 71 projetos, 16+ DbContexts |
| `FRONTEND-ARCHITECTURE.md` | 16 linhas — não reflete 105 páginas, 13 features, hooks pattern |
| `DATA-ARCHITECTURE.md` | Descreve 5 entidades quando existem 50+ |
| `MODULES-AND-PAGES.md` | Lista ~30 páginas quando existem 105 |
| `I18N-STRATEGY.md` | 6 pontos para 4 locales com ~639 KB |
| `PERSONA-MATRIX.md` | Não reflete a implementação completa de persona-awareness no AppSidebar |
| `frontend/AUDIT-REPORT.md` | Junho 2025 — 9 meses desatualizado |
| `frontend/TECHNICAL-INVENTORY.md` | Junho 2025 — 9 meses desatualizado |

### 4.2 Documentação Contraditória

Documentos que se **contradizem** entre si.

| Documento A | Documento B | Contradição |
|------------|------------|-------------|
| `DOMAIN-BOUNDARIES.md` (6 contexts) | Código real (9 módulos) | Número de bounded contexts diverge |
| `ARCHITECTURE-OVERVIEW.md` (7 contexts) | `DOMAIN-BOUNDARIES.md` (6 contexts) | Inconsistência no número de contexts |
| `PRODUCT-REFOUNDATION-PLAN.md` ("parar expansão") | Código mostra expansão contínua (Product Analytics, Integrations Hub) | Estratégia vs realidade |
| `ADR-001-database-strategy.md` | `adr/ADR-001-database-consolidation-plan.md` | Dois ADRs com numeração igual e conteúdo divergente |
| `ADR-002-migration-policy.md` | `adr/ADR-002-migration-policy.md` | Duplicata com possíveis divergências |

### 4.3 Documentação Incompleta

Documentação que cobre o tema mas de forma **insuficiente**.

| Documento | O que Falta |
|-----------|-------------|
| `ARCHITECTURE-OVERVIEW.md` | Detalhes dos 9 módulos, building blocks, database strategy, event bus |
| `FRONTEND-ARCHITECTURE.md` | Lista de features, padrão de hooks, lazy loading, React Query |
| `DATA-ARCHITECTURE.md` | Todos os 16+ DbContexts, relações entre databases, migration strategy |
| `I18N-STRATEGY.md` | Lista de locales, estrutura das chaves, processo de tradução |
| `INTEGRATIONS-ARCHITECTURE.md` | Detalhes de implementação dos conectores |
| `OBSERVABILITY-STRATEGY.md` | Detalhes da implementação real vs plano |

### 4.4 Documentação Excessivamente Otimista

Documentação que descreve funcionalidades como **completas** quando estão parciais ou em stubs.

| Documento | Realidade |
|-----------|-----------|
| `AI-ARCHITECTURE.md` | Descreve sistema multi-camada completo; backend está a 20-25% |
| `AI-ASSISTED-OPERATIONS.md` | Descreve 3 tipos de IA operacional; implementação real é básica |
| `AI-DEVELOPER-EXPERIENCE.md` | Descreve IDE extensions; não existem extensões reais |
| `CONTRACT-STUDIO-VISION.md` | Descreve visão completa; DraftStudioPage é parcial |
| `SERVICE-CONTRACT-GOVERNANCE.md` | Descreve capabilities obrigatórias; nem todas implementadas |
| `PLATFORM-CAPABILITIES.md` | Lista capabilities "core" e "futuras" sem distinção clara de estado |
| `SOURCE-OF-TRUTH-STRATEGY.md` | Descreve NexTraceOne como fonte oficial; implementação parcial |
| `DEPLOYMENT-ARCHITECTURE.md` | Descreve arquitetura de deployment completa; pipeline pode não estar implementada |

### 4.5 Código sem Documentação

| Componente | Linhas de Código Estimadas | Impacto |
|-----------|---------------------------|---------|
| Product Analytics (5 páginas) | ~1500 | Alto — funcionalidade sem referência |
| Configuration module (345 defs) | ~5000+ | Alto — módulo complexo sem doc unificada |
| Building Blocks (5 bibliotecas) | ~5000+ | Alto — fundação sem referência |
| Integrations Hub (4 páginas) | ~1000 | Médio |
| Platform Hosts (3 projetos) | ~2000+ | Médio |
| 22 páginas de Governance | ~5000+ | Médio — funcionalidades sem docs individuais |
| Frontend hooks (18+) | ~1500 | Médio — padrões sem referência |

### 4.6 Documentação sem Aderência ao Produto Atual

| Documento | Problema |
|-----------|---------|
| `WAVE-1-CONSOLIDATED-VALIDATION.md` | Validação de wave 1 — referência histórica |
| `WAVE-1-VALIDATION-TRACKER.md` | Tracker histórico |
| `EXECUTION-BASELINE-PR1-PR16.md` | Baseline de PRs concluídos |
| `POST-PR16-EVOLUTION-ROADMAP.md` | Estratégia parcialmente suplantada pelo ROADMAP.md |
| `AI-LOCAL-IMPLEMENTATION-AUDIT.md` | Auditoria marcada como histórica |
| Documentos `architecture/phase-0/` a `phase-8/` | Fases concluídas — valor histórico apenas |
| `docs/audits/PHASE-0-*` a `PHASE-4-*` | Auditorias de fases iniciais |
| Maioria dos `docs/audits/WAVE-*` | Reports de waves passadas |

---

## 5. Resumo de Ações Recomendadas

### Prioridade Alta

1. **Criar documentação para módulos sem docs** — Configuration (unificada), Product Analytics, Integrations, Building Blocks
2. **Atualizar documentação atrasada** — ARCHITECTURE-OVERVIEW.md, FRONTEND-ARCHITECTURE.md, DATA-ARCHITECTURE.md, MODULES-AND-PAGES.md
3. **Resolver contradições** — ADRs duplicados, número de bounded contexts, docs de AI vs realidade

### Prioridade Média

4. **Consolidar documentos sobrepostos** — DESIGN*.md, SECURITY*.md, PERSONA*.md, ADRs duplicados
5. **Calibrar documentação otimista** — Adicionar indicadores de maturidade real (%, estado) em docs de AI e Contract Studio
6. **Atualizar frontend docs** — AUDIT-REPORT.md e TECHNICAL-INVENTORY.md têm 9 meses de atraso

### Prioridade Baixa

7. **Arquivar documentação histórica** — Mover phase reports, wave reports e baselines para pasta archive/
8. **Criar docs para páginas escondidas** — 22+ páginas de Governance, sub-rotas de Analytics e Integrations
