# NexTraceOne — Comparativo Documentação vs Código

**Data:** 2026-03-24
**Metodologia:** Cruzamento entre estrutura de código real e documentação existente
**Princípio:** O código é a fonte de verdade. A documentação é avaliada contra o código.

---

## Legenda de Gaps

| Código | Tipo de Gap |
|--------|-------------|
| `DOC_ATRASADA` | Documentação existe mas não acompanhou evolução do código |
| `DOC_CONTRADITORIA` | Documentação contradiz o que o código mostra |
| `DOC_INCOMPLETA` | Documentação existe mas cobre apenas parte do que o código implementa |
| `DOC_OTIMISTA` | Documentação descreve features como prontas que o código marca como SIM/PLAN |
| `SEM_DOC` | Código sem qualquer documentação associada |
| `DOC_SEM_CODIGO` | Documentação descreve features que não existem no código |
| `OK` | Alinhamento aceitável entre documentação e código |

---

## PARTE 1 — Módulos Documentados vs Existentes no Código

### Backend — 9 módulos no código

| Módulo Backend | Doc Arquitetural Dedicada | Gap |
|----------------|---------------------------|-----|
| `AIKnowledge` | Parcial — `docs/AI-ARCHITECTURE.md`, `docs/aiknowledge/` | `DOC_INCOMPLETA` — espalhada em 5+ ficheiros sem visão unificada |
| `AuditCompliance` | **Nenhuma** | `SEM_DOC` — módulo ativo sem qualquer documentação de módulo |
| `Catalog` | Parcial — `docs/SERVICE-CONTRACT-GOVERNANCE.md`, `docs/SOURCE-OF-TRUTH-STRATEGY.md`, `docs/CONTRACT-STUDIO-VISION.md` | `DOC_INCOMPLETA` — cobre contratos mas não Graph, Portal em detalhe |
| `ChangeGovernance` | Parcial — `docs/CHANGE-CONFIDENCE.md` | `DOC_INCOMPLETA` — cobre Change Confidence mas não Promotion, Workflow, Ruleset |
| `Configuration` | **Nenhuma** | `SEM_DOC` — 30+ guias de execução existem mas sem doc de módulo |
| `Governance` | Parcial — `docs/governance/PHASE-5-GOVERNANCE-ENRICHMENT.md` | `DOC_ATRASADA` — doc de fase, não estado atual |
| `IdentityAccess` | Parcial — `docs/SECURITY-ARCHITECTURE.md`, `docs/security/BACKEND-ENDPOINT-AUTH-AUDIT.md` | `DOC_INCOMPLETA` — cobre segurança mas não identidade/tenancy em detalhe |
| `Notifications` | Parcial — `docs/execution/NOTIFICATIONS-*` (25+ ficheiros) | `DOC_INCOMPLETA` — muito detalhe de configuração, sem visão arquitetural do módulo |
| `OperationalIntelligence` | Parcial — `docs/reliability/` | `DOC_INCOMPLETA` — cobre reliability mas não Automation, Cost, Incidents, Runtime |

**Conclusão:** **Nenhum dos 9 módulos tem documentação arquitetural completa e atual.**

---

## PARTE 2 — Documentação vs Estrutura Real do Produto

### 2.1 MODULES-AND-PAGES.md vs Realidade

**`docs/MODULES-AND-PAGES.md`** descreve o produto em 7 módulos e ~30 linhas:

```
Home — Executive dashboard, Operational dashboard, Engineer dashboard
Services — Service Catalog, Ownership, Dependencies, Reliability
Contracts — API Contracts, Event Contracts, Contract Studio, Versioning, Diff
Changes — Change Intelligence, Blast Radius, Production Confidence
Operations — Incidents, Mitigation, Runbooks, Operational consistency
AI Hub — AI Assistant, Model Registry, AI Policies, AI Usage, Token Governance
Governance — Reports, Risk, Compliance, FinOps
```

**Realidade do código:**
- 15 módulos frontend
- 100+ páginas
- 12 secções de sidebar
- Módulos inteiros ausentes da documentação: `identity-access` (12 páginas!), `integrations`, `notifications`, `product-analytics`, `audit-compliance`, `configuration`

**Gap:** `DOC_INCOMPLETA` / `DOC_ATRASADA` — documento é placeholder, não inventário real.

---

### 2.2 ARCHITECTURE-OVERVIEW.md vs Código

**Expectativa da doc:** Deve descrever os 9 módulos, 5 building blocks, 3 plataformas e 20 DbContexts.

**Risco:** Documento pode ainda referenciar arquitetura de fase anterior (antes da separação de DbContexts por subdomínio). Migrations recentes (2026-03-23: `SeparateSharedEntityOwnership`) sugerem refactoring recente não documentado.

**Gap:** `DOC_ATRASADA` — confirmar na revisão detalhada.

---

### 2.3 FRONTEND-ARCHITECTURE.md vs Código Real

**Expectativa:** Deve mencionar React 19.2.0, React Router 7.13.1, TanStack Query 5.90.21, i18next 25.8.18.

**Risco:** Versões podem ter mudado sem atualização documental.

**Gap:** `DOC_ATRASADA` — confirmar versões reais vs documentadas.

---

### 2.4 IMPLEMENTATION-STATUS.md vs Código

Este é o documento mais confiável e alinhado com o código real. Usa taxonomia clara (IMPL/PARTIAL/SIM/PLAN/DEF/PREV).

**Gaps identificados (parciais):**

| Feature | IMPLEMENTATION-STATUS.md | Evidência no código |
|---------|--------------------------|---------------------|
| Environments | `PARTIAL` — IsProductionLike sem lógica | A confirmar |
| Dependencies/Topology | `PLAN` — IObservedTopologyWriter/Reader | DbContext não encontrado para topology |
| IContractsModule cross-module | `PLAN` | Interface definida, sem implementation |

**Gap:** `OK` — este documento parece o mais atualizado. Mas precisa de atualização para incluir módulos adicionados após a sua última revisão.

---

## PARTE 3 — Páginas Documentadas vs Existentes

### 3.1 Páginas sem Documentação

As seguintes páginas existem no código e não têm documentação funcional associada:

| Módulo | Páginas sem doc funcional |
|--------|--------------------------|
| `identity-access` | 12 páginas — apenas `docs/security/BACKEND-ENDPOINT-AUTH-AUDIT.md` |
| `product-analytics` | 5 páginas — sem qualquer documentação |
| `audit-compliance` | 1 página — sem documentação de módulo |
| `notifications` | 3 páginas — doc de configuração existe mas não user guide |
| `integrations` | 4 páginas — sem user guide |
| `configuration` | 2 páginas admin — 30+ guias de configuração mas sem inventário das páginas |
| Contracts (4 páginas órfãs) | `governance`, `spectral`, `canonical`, `portal` — sem documentação e sem rota |

**Gap:** `SEM_DOC` — maioria das páginas não tem documentação funcional.

### 3.2 Documentação que Não Corresponde a Páginas Reais

| Documento | Problema |
|-----------|---------|
| `docs/MODULES-AND-PAGES.md` menciona "Ownership" em Services | Nenhuma rota ou página `OwnershipPage` encontrada |
| `docs/MODULES-AND-PAGES.md` menciona "Operational consistency" em Operations | Nenhuma página correspondente |
| `docs/user-guide/service-catalog.md` | Pode descrever features não implementadas ou com nome diferente |

**Gap:** `DOC_SEM_CODIGO` — potencial para docs descreverem features que não existem como páginas.

---

## PARTE 4 — Arquitetura Descrita vs Implementada

### 4.1 Event Bus

- **ADR-003-event-bus-limitations.md** documenta decisão de usar event bus in-process (não distribuído)
- **Código:** `BuildingBlocks.Infrastructure` tem outbox pattern
- **Gap:** `OK` — ADR existe e é consistente com implementação

### 4.2 Multi-Tenant / DbContext por Subdomínio

- **Código:** 20 DbContexts separados por subdomínio (ex: `ContractsDbContext`, `CatalogGraphDbContext`, `DeveloperPortalDbContext` — todos em `catalog`)
- **ADR-001-database-strategy.md** — documenta estratégia de banco de dados
- **Migration recente:** `20260323201957_SeparateSharedEntityOwnership` — indica refactoring recente
- **Gap:** `DOC_ATRASADA` — migration muito recente pode não estar refletida na documentação de arquitetura

### 4.3 Tenant Isolation

- **`docs/architecture/phase-0/ADR-001-tenant-environment-context-refactor.md`** — documenta refactoring de tenancy
- **Código:** `TenantEndpoints`, `EnvironmentEndpoints`, `RuntimeContextEndpoints` em IdentityAccess
- **Migration:** `StandardizeTenantIdToGuid`, `FixTenantIdToUuid` — ajustes recentes de tenancy
- **Gap:** `DOC_ATRASADA` — migrations recentes sugerem que o modelo de tenancy ainda evoluiu após a documentação

### 4.4 Simulated Data / Demo Stubs

- **ADR-004-simulated-data-policy.md** documenta política de dados simulados
- **IMPLEMENTATION-STATUS.md** identifica handlers SIM vs IMPL
- **`docs/engineering/ANTI-DEMO-REGRESSION-CHECKLIST.md`** existe
- **Gap:** `OK` — documentação e código alinhados. Mas o estado real de quantos handlers ainda são SIM vs IMPL precisa de verificação handler a handler.

### 4.5 Personas e Configuração do Menu

- **`docs/PERSONA-MATRIX.md`** e **`docs/PERSONA-UX-MAPPING.md`** descrevem personas
- **Código:** `PersonaContext.tsx` existe, `config.sectionOrder` e `config.highlightedSections` controlam o menu
- **Gap:** `DOC_ATRASADA` — documentação de personas pode não estar sincronizada com as personas reais implementadas no `PersonaContext`

---

## PARTE 5 — Gaps por Área Funcional

### 5.1 Layout e Design System

| Gap | Detalhe |
|-----|---------|
| `docs/DESIGN-SYSTEM.md` | Verificar se menciona tokens CSS customizados (`--nto-motion-medium`, `--z-header`, `brand-gradient`, etc.) encontrados no código |
| `docs/DESIGN.md` | Verificar se menciona Tailwind CSS como sistema de styling (vs o que estava planeado) |
| Componentes shell | `AppSidebarGroup`, `AppSidebarItem`, `PageContainer`, `PageSection`, etc. — sem documentação de componentes |

**Gap:** `DOC_INCOMPLETA` — design system documentado pode não cobrir os tokens e componentes reais.

### 5.2 i18n

| Gap | Detalhe |
|-----|---------|
| `docs/I18N-STRATEGY.md` | Estratégia existe mas cobertura real por módulo não foi auditada |
| 4 locales existem (`en`, `es`, `pt-BR`, `pt-PT`) | Completude desconhecida |
| `sidebar.*` keys | Existem no código mas não sabemos se estão em todos os locales |
| Páginas novas | Cada nova página pode ter adicionado strings não traduzidas |

**Gap:** `DOC_INCOMPLETA` — estratégia documentada mas cobertura real desconhecida.

### 5.3 Backend AI Hub

| Gap | Detalhe |
|-----|---------|
| 5 documentos sobre AI (`AI-ARCHITECTURE.md`, `AI-GOVERNANCE.md`, `AI-ASSISTED-OPERATIONS.md`, `AI-DEVELOPER-EXPERIENCE.md`, `AI-LOCAL-IMPLEMENTATION-AUDIT.md`) | Espalhados, sem visão única |
| `ExternalAiEndpointModule` | Não está claro qual provider real está integrado (OpenAI? Anthropic?) |
| `AiRuntimeEndpointModule` | Runtime de IA — documentação de ADR-005 existe mas pode estar desatualizada |
| Agent runtime | ADR-006 existe mas página `AgentDetailPage` é recente — alinhamento? |

**Gap:** `DOC_INCOMPLETA` / `DOC_ATRASADA`

### 5.4 Contratos — Páginas Órfãs

| Gap | Detalhe |
|-----|---------|
| `ContractGovernancePage.tsx` | Página completa no código. Sem rota. Sem documentação que explique esta omissão. |
| `SpectralRulesetManagerPage.tsx` | Idem |
| `CanonicalEntityCatalogPage.tsx` | Idem |
| `ContractPortalPage.tsx` | Idem |

**Gap:** `SEM_DOC` + `DOC_SEM_CODIGO` — código existe sem rota e sem documentação que explique o estado.

### 5.5 Governance — Sub-páginas Escondidas

| Gap | Detalhe |
|-----|---------|
| `EnterpriseControlsPage`, `EvidencePackagesPage`, `MaturityScorecardsPage`, `BenchmarkingPage`, `WaiversPage`, `DelegatedAdminPage` | 6 páginas ativas sem item de menu |
| Nenhum documento descreve como o utilizador deve aceder a estas páginas | Navegação interna não documentada |

**Gap:** `DOC_INCOMPLETA` — funcionalidades existem no código mas não há documentação sobre como são acessadas.

### 5.6 OperationalIntelligence — Divergência Frontend/Backend

| Gap | Detalhe |
|-----|---------|
| Backend: 5 DbContexts, 7 endpoint modules (Automation, Cost, Incidents, Mitigation, Reliability, Runtime) | Módulo backend mais complexo por DbContexts |
| Frontend `operational-intelligence`: 1 página | Apenas config page |
| Frontend `operations`: 8 páginas | Consome este backend mas é um módulo separado |
| Documentação: confunde os dois módulos | `docs/reliability/` refere reliability mas não diz de qual módulo |

**Gap:** `DOC_CONTRADITORIA` / `DOC_INCOMPLETA` — a separação conceptual backend/frontend não está documentada.

---

## PARTE 6 — Promessas Documentais não Refletidas no Código

| Documento | Promessa | Estado no Código |
|-----------|---------|------------------|
| `MODULES-AND-PAGES.md` | "Executive dashboard, Operational dashboard, Engineer dashboard" em Home | Apenas `DashboardPage` — sem 3 dashboards distintos |
| `MODULES-AND-PAGES.md` | "Ownership" em Services | Sem página correspondente |
| `MODULES-AND-PAGES.md` | "Operational consistency" em Operations | Sem página correspondente |
| `PRODUCT-VISION.md` | Desconhecido — precisa de leitura | A verificar |
| `PLATFORM-CAPABILITIES.md` | Capacidades listadas | A verificar vs código |

**Gap:** `DOC_OTIMISTA` — documento de módulos e páginas descreve features não implementadas como se existissem.

---

## PARTE 7 — Código sem Documentação (SEM_DOC prioritários)

Áreas críticas do produto sem qualquer documentação:

| Área | Evidência de código | Estado documental |
|------|---------------------|-------------------|
| `AuditCompliance` módulo | `AuditEndpointModule.cs`, `AuditDbContext`, `AuditPage.tsx` | **Nenhuma** |
| `ProductAnalyticsEndpointModule` | 5 páginas de analytics, 1 endpoint module em Governance | Nenhuma doc de produto |
| `NotificationCenterEndpointModule` | Módulo de notificações completo | Apenas guias técnicos, sem user guide |
| `ConfigurationEndpointModule` | Módulo de config | Guias de execução existem mas sem doc de módulo |
| `ContractGovernancePage` | Página completa com componentes | Zero documentação |
| `SpectralRulesetManagerPage` | Idem | Zero documentação |
| `CanonicalEntityCatalogPage` | Idem | Zero documentação |
| `ContractPortalPage` | Idem | Zero documentação |
| `WorkspaceSwitcher` | Componente de troca de tenant | Sem documentação |
| `PersonaContext` + persona system | Sistema de personas completo | Docs de persona não refletem implementação atual |

---

## PARTE 8 — Resumo de Gaps por Categoria

| Tipo de Gap | Módulos/Áreas Afetadas | Severidade |
|-------------|------------------------|------------|
| `SEM_DOC` | AuditCompliance, Configuration (como módulo), Contracts (4 páginas órfãs), ProductAnalytics, Notifications (user guide) | Alta |
| `DOC_INCOMPLETA` | AIKnowledge, Catalog, ChangeGovernance, Governance, IdentityAccess, OperationalIntelligence, i18n, Design System | Alta |
| `DOC_ATRASADA` | ARCHITECTURE-OVERVIEW, FRONTEND-ARCHITECTURE, DATA-ARCHITECTURE, PERSONA-MATRIX, Governance | Média |
| `DOC_CONTRADITORIA` | MODULES-AND-PAGES, OperationalIntelligence vs operations (naming), Runbooks permissão | Média |
| `DOC_OTIMISTA` | MODULES-AND-PAGES (features inexistentes), PRODUCT-VISION | Média |
| `DOC_SEM_CODIGO` | Features descritas em MODULES-AND-PAGES não encontradas em App.tsx | Baixa-Média |
| `OK` | Event Bus ADR, IMPLEMENTATION-STATUS, Runbooks docs, Security docs | — |

---

## Recomendações Prioritárias

1. **Criar documentação de módulo** para: AuditCompliance, Configuration, Notifications
2. **Reescrever MODULES-AND-PAGES.md** completamente — usar este relatório como base
3. **Registar ou remover** as 4 páginas órfãs de contracts — decisão de produto necessária
4. **Atualizar ARCHITECTURE-OVERVIEW.md** para refletir 20 DbContexts e refactoring de 2026-03-23
5. **Auditar i18n cobertura** módulo a módulo — risco de strings não traduzidas em páginas recentes
6. **Documentar navegação interna** para sub-páginas de governance escondidas
7. **Atualizar IMPLEMENTATION-STATUS.md** para cobrir módulos mais recentes (se algum foi adicionado)
8. **Consolidar documentação de AI** em documento único por módulo
