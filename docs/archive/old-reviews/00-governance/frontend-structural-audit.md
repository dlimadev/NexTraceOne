# Auditoria Estrutural do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Análise completa da aplicação frontend React/TypeScript  
> **Status global:** GAP_IDENTIFIED  
> **Prioridade geral:** HIGH

---

## 1. Resumo Executivo

### 1.1 Estado Geral

O frontend do NexTraceOne apresenta uma arquitetura modular sólida com **14 módulos de features**, **108 componentes de página**, **130+ rotas** e **45 itens de menu lateral**. A aplicação utiliza React 18 com TypeScript, React Router v6, React Query para gestão de estado assíncrono e Context API para estado global (AuthContext, PersonaContext, EnvironmentContext).

**Pontos fortes:**
- Arquitetura modular bem definida com separação clara de responsabilidades
- Sistema de permissões granular (60+ tipos) com guard component `ProtectedRoute`
- Suporte a 7 personas com configuração UX personalizada
- 12 de 14 módulos com integração API real
- Design system emergente com 68+ componentes reutilizáveis
- Suporte i18n para 4 locales (en, pt-PT, pt-BR, es)

**Riscos identificados:**
- 3 rotas do sidebar apontam para páginas sem rota registada em `App.tsx` (CRITICAL)
- 9 páginas órfãs sem rota ou referência ativa
- Gaps significativos em i18n: pt-BR (-11 chaves), es (-8 chaves), pt-PT (-1 chave)
- 1 ficheiro de página com 0 bytes (`ProductAnalyticsOverviewPage.tsx`)
- Módulo `operational-intelligence` sem integração API

### 1.2 Métricas Consolidadas

| Métrica | Valor | Avaliação |
|---------|-------|-----------|
| Módulos de features | 14 | ✅ Adequado |
| Componentes de página | 108 | ✅ Amplo |
| Rotas registadas | 130+ | ✅ Cobertura extensa |
| Itens de menu lateral | 45+ | ✅ Navegação rica |
| Permissões distintas | 60+ | ✅ Granularidade adequada |
| Personas suportadas | 7 | ✅ Segmentação completa |
| Locales | 4 (en, pt-PT, pt-BR, es) | ⚠️ Gaps em 3 locales |
| Rotas quebradas | 3 | ❌ CRITICAL |
| Páginas órfãs | 9 | ⚠️ HIGH |
| Módulos com API real | 12/14 | ⚠️ 2 sem integração |
| Componentes reutilizáveis | 68 | ✅ Biblioteca sólida |

### 1.3 Divergências em relação à visão do produto

| Área | Expectativa | Realidade | Gap |
|------|------------|-----------|-----|
| Contract Governance | Pilar central do produto | 3 rotas quebradas no módulo contracts | CRITICAL |
| Source of Truth | Módulo completo e funcional | Funcional mas sem cobertura de contratos canónicos | HIGH |
| FinOps contextual | FinOps por serviço/equipa/domínio | Páginas existem mas sem API completa em `operational-intelligence` | MEDIUM |
| i18n completo | 100% cobertura em todos os locales | pt-BR falta 11 namespaces, es falta 8 | HIGH |
| Contract Studio | Criação e edição completa | Funcional com integração API | ✅ OK |

---

## 2. Inventário Modular

### 2.1 Tabela Resumo

| # | Módulo | Ficheiros | Páginas | Rotas | Menu | API | Classificação |
|---|--------|-----------|---------|-------|------|-----|---------------|
| 1 | contracts | 70 | 8 | 5 ativas (+3 sem rota) | 6 | Sim (3 ficheiros) | PARTIAL |
| 2 | governance | 30 | 25 | 25+ | 7 | Sim (4 ficheiros) | COMPLETE_APPARENT |
| 3 | catalog | 23 | 12 | 9 (+3 órfãs) | 2 | Sim (7 ficheiros) | PARTIAL |
| 4 | identity-access | 23 | 15 | 15 | 8 | Sim (2 ficheiros) | COMPLETE_APPARENT |
| 5 | ai-hub | 15 | 11 | 11 | 9 | Sim (2 ficheiros) | COMPLETE_APPARENT |
| 6 | operations | 16 | 10 | 10 | 5 | Sim (5 ficheiros) | COMPLETE_APPARENT |
| 7 | change-governance | 13 | 6 | 6 | 4 | Sim (5 ficheiros) | COMPLETE_APPARENT |
| 8 | notifications | 11 | 3 | 3 | 0 | Sim (1 ficheiro) | COMPLETE_APPARENT |
| 9 | product-analytics | 8 | 6 | 5 (+1 vazio) | 1 | Sim (1 ficheiro) | PARTIAL |
| 10 | integrations | 6 | 4 | 4 | 1 | Sim (1 ficheiro) | COMPLETE_APPARENT |
| 11 | configuration | 6 | 2 | 2 | 1 | Sim (1 ficheiro) | COMPLETE_APPARENT |
| 12 | audit-compliance | 4 | 1 | 1 | 1 | Sim (2 ficheiros) | COMPLETE_APPARENT |
| 13 | operational-intelligence | 1 | 1 | 1 | 0 | Não | PARTIAL |
| 14 | shared | 2 | 1 | 1 | 1 | N/A | N/A |

### 2.2 Módulos com problemas

| Módulo | Problema | Prioridade | Status |
|--------|----------|------------|--------|
| contracts | 3 rotas do sidebar sem registo em App.tsx | CRITICAL | GAP_IDENTIFIED |
| catalog | 3 páginas sem rota nem referência | HIGH | GAP_IDENTIFIED |
| product-analytics | 1 página com 0 bytes | MEDIUM | GAP_IDENTIFIED |
| operational-intelligence | Sem integração API (1 ficheiro apenas) | MEDIUM | GAP_IDENTIFIED |

---

## 3. Páginas e Rotas

### 3.1 Rotas quebradas (CRITICAL)

| Rota no Sidebar | Página existente | Problema |
|-----------------|-----------------|----------|
| `/contracts/governance` | `features/contracts/governance/ContractGovernancePage.tsx` | Rota **NÃO** registada em `App.tsx` |
| `/contracts/spectral` | `features/contracts/spectral/SpectralRulesetManagerPage.tsx` | Rota **NÃO** registada em `App.tsx` |
| `/contracts/canonical` | `features/contracts/canonical/CanonicalEntityCatalogPage.tsx` | Rota **NÃO** registada em `App.tsx` |

**Impacto:** O utilizador clica no menu lateral, a aplicação navega para a rota, mas o React Router não a encontra — resultado: página em branco ou redirect inesperado.

### 3.2 Páginas órfãs (9 identificadas)

| # | Página | Ficheiro | Tipo de problema |
|---|--------|----------|-----------------|
| 1 | ContractGovernancePage | `features/contracts/governance/ContractGovernancePage.tsx` | Sidebar link existe, sem rota |
| 2 | SpectralRulesetManagerPage | `features/contracts/spectral/SpectralRulesetManagerPage.tsx` | Sidebar link existe, sem rota |
| 3 | CanonicalEntityCatalogPage | `features/contracts/canonical/CanonicalEntityCatalogPage.tsx` | Sidebar link existe, sem rota |
| 4 | ContractPortalPage | `features/contracts/portal/ContractPortalPage.tsx` | Sem referência ativa |
| 5 | ContractDetailPage | `features/catalog/pages/ContractDetailPage.tsx` | Sem referência ativa |
| 6 | ContractListPage | `features/catalog/pages/ContractListPage.tsx` | Sem referência ativa |
| 7 | ContractsPage | `features/catalog/pages/ContractsPage.tsx` | Sem referência ativa |
| 8 | ProductAnalyticsOverviewPage | `features/product-analytics/ProductAnalyticsOverviewPage.tsx` | Ficheiro vazio (0 bytes) |
| 9 | InvitationPage | `features/identity-access/pages/InvitationPage.tsx` | Import eager apenas (redirect) |

---

## 4. Menu e Navegação

### 4.1 Estrutura do Sidebar

O sidebar está organizado em **12 secções** com **45+ itens**:

| Secção | Itens | Descrição |
|--------|-------|-----------|
| home | 1 | Dashboard |
| services | 2 | Catálogo de serviços, grafo de dependências |
| knowledge | 2 | Source of Truth, Developer Portal |
| contracts | 6 | Catálogo, criação, studio, governance, spectral, canonical |
| changes | 4 | Mudanças, releases, workflow, promoção |
| operations | 5 | Incidentes, runbooks, reliability, automação, comparação |
| aiHub | 9 | Assistente, agentes, modelos, políticas, routing, IDE, budgets, auditoria, análise |
| governance | 7 | Executivo, relatórios, compliance, risk, FinOps, políticas, packs |
| organization | 2 | Equipas, domínios |
| integrations | 1 | Hub de integrações |
| analytics | 1 | Product analytics |
| admin | 8+ | Utilizadores, break-glass, JIT, delegações, sessões, auditoria, plataforma |

### 4.2 Inconsistências de navegação

| Problema | Detalhe | Prioridade |
|----------|---------|------------|
| 3 itens do menu sem rota funcional | contracts/governance, contracts/spectral, contracts/canonical | CRITICAL |
| Notificações sem item de menu | 3 páginas de notificações sem entrada no sidebar | LOW |
| Notifications acessíveis apenas via topbar | Padrão intencional mas não documentado | LOW |
| Configuração avançada sem menu dedicado | Acessível via rotas de admin | LOW |

---

## 5. Permissões

### 5.1 Modelo de permissões

O sistema utiliza permissões granulares no formato `modulo:recurso:ação`, verificadas via:
- **`ProtectedRoute`** — guard de rota em `App.tsx`
- **`usePermissions()`** — hook para controlo de visibilidade
- **Sidebar** — cada item verifica permissão antes de renderizar

### 5.2 Permissões por domínio

| Domínio | Permissões | Exemplo |
|---------|-----------|---------|
| Identity | 5 | `identity:users:read`, `identity:roles:assign`, `identity:sessions:revoke` |
| Catalog | 2 | `catalog:assets:read`, `catalog:assets:write` |
| Contracts | 3 | `contracts:read`, `contracts:write`, `contracts:import` |
| Developer Portal | 2 | `developer-portal:read`, `developer-portal:write` |
| Changes | 4 | `change-intelligence:read`, `change-intelligence:releases:read`, `workflow:read`, `promotion:read` |
| Operations | 5 | `operations:incidents:read`, `operations:runbooks:read`, `operations:reliability:read`, `operations:automation:read`, `operations:runtime:read` |
| AI | 4 | `ai:assistant:read`, `ai:governance:read`, `ai:governance:write`, `ai:runtime:write` |
| Governance | 1 | `governance:read` |
| Audit | 1 | `audit:read` |
| Integrations | 1 | `integrations:read` |
| Analytics | 1 | `analytics:read` |
| Platform | 1 | `platform:admin:read` |

### 5.3 Lacunas de permissões

| Problema | Detalhe | Prioridade |
|----------|---------|------------|
| Governance monolítica | Todas as páginas de governance usam `governance:read` — sem granularidade para FinOps, Risk, Compliance | MEDIUM |
| Sem permissão de escrita em governance | Não há `governance:write` para edição de políticas | MEDIUM |
| Notifications sem permissão | Páginas de notificações não requerem permissão específica | LOW |

---

## 6. i18n — Internacionalização

### 6.1 Cobertura por locale

| Locale | Chaves top-level | Diferença vs en | Status |
|--------|-----------------|-----------------|--------|
| en (referência) | 63 | — | ✅ REFERÊNCIA |
| pt-PT | 62 | -1 | ⚠️ PARTIAL |
| pt-BR | 52 | -11 | ❌ GAP_IDENTIFIED |
| es | 55 | -8 | ❌ GAP_IDENTIFIED |

### 6.2 Chaves em falta

| Locale | Namespaces em falta |
|--------|-------------------|
| pt-PT | `agents` |
| pt-BR | `agents`, `analytics`, `automation`, `breadcrumbs`, `domainBadges`, `governancePacks`, `integrations`, `onboarding`, `persona`, `productPolish`, `shell` |
| es | `activation`, `agents`, `domainBadges`, `forgotPassword`, `invitation`, `mfa`, `preview`, `resetPassword` |

---

## 7. Integração com API

### 7.1 Resumo

| Classificação | Módulos | Quantidade |
|--------------|---------|-----------|
| API_REAL | ai-hub, audit-compliance, catalog, change-governance, configuration, contracts, governance, identity-access, integrations, notifications, operations, product-analytics | 12 |
| SEM_API | operational-intelligence | 1 |
| N/A | shared | 1 |

### 7.2 Ficheiros API por módulo

| Módulo | Ficheiros API | Exports principais |
|--------|--------------|-------------------|
| catalog | 7 | serviceCatalogApi, contractsApi, developerPortalApi, globalSearchApi, sourceOfTruthApi |
| change-governance | 5 | changeIntelligenceApi, workflowApi, promotionApi, changeConfidenceApi |
| operations | 5 | incidentsApi, reliabilityApi, automationApi, runtimeIntelligenceApi, platformOpsApi |
| governance | 4 | organizationGovernanceApi, evidenceApi, executiveApi, finOpsApi |
| contracts | 3 | contractsApi, contractStudioApi |
| ai-hub | 2 | aiGovernanceApi |
| audit-compliance | 2 | auditApi |
| identity-access | 2 | identityApi |
| configuration | 1 | configurationApi |
| integrations | 1 | integrationsApi |
| notifications | 1 | notificationsApi |
| product-analytics | 1 | productAnalyticsApi |

---

## 8. UX e Layout

### 8.1 Componentes reutilizáveis

| Categoria | Quantidade | Exemplos |
|-----------|-----------|---------|
| UI base | 27 | Button, Card, Badge, Modal, Tabs, Tooltip, Skeleton |
| Shell/Layout | 18 | AppShell, AppSidebar, AppTopbar, ContentGrid, FilterBar |
| Formulários | 8 | TextField, TextArea, Select, Checkbox, Radio, Toggle, SearchInput |
| Estados | 5 | PageLoadingState, PageErrorState, EmptyState, ErrorBoundary, Loader |
| Complexos | 10 | CommandPalette, StatCard, QuickActions, OnboardingHints, PersonaQuickstart |

### 8.2 Padrões de layout

- **AppShell** como wrapper principal de todas as rotas protegidas
- **PageHeader + PageContainer + PageSection** como padrão de layout de página
- **ContentGrid** para layouts grid responsivos
- **FilterBar + TableWrapper** para páginas de listagem
- **DetailPanel** para painéis laterais de detalhe

### 8.3 Problemas de UX identificados

| Problema | Detalhe | Prioridade |
|----------|---------|------------|
| Rotas quebradas sem fallback gracioso | 3 rotas levam a página em branco | CRITICAL |
| Ficheiro de página vazio | ProductAnalyticsOverviewPage.tsx com 0 bytes | MEDIUM |
| Consistência de estados vazios | Nem todas as páginas implementam EmptyState | MEDIUM |
| Feedback de carregamento | Inconsistência entre Loader e Skeleton | LOW |

---

## 9. Fluxo de Autenticação

### 9.1 Arquitetura

| Componente | Descrição | Ficheiro |
|-----------|-----------|---------|
| AuthContext | Estado de autenticação global | `contexts/AuthContext.tsx` |
| PersonaContext | Persona UX do utilizador | `contexts/PersonaContext.tsx` |
| EnvironmentContext | Ambiente selecionado | `contexts/EnvironmentContext.tsx` |
| ProtectedRoute | Guard de permissão por rota | `components/ProtectedRoute.tsx` |

### 9.2 Tokens e segurança

| Aspeto | Implementação |
|--------|--------------|
| Access token | `sessionStorage` |
| Refresh token | Memória apenas (não persistido) |
| CSRF token | Enviado em mutações |
| Permissões | Derivadas do perfil do utilizador via backend |

### 9.3 Páginas públicas (sem auth)

LoginPage, TenantSelectionPage, ForgotPasswordPage, ResetPasswordPage, ActivationPage, MfaPage, InvitationPage

---

## 10. Recomendações

### 10.1 CRITICAL — Corrigir imediatamente

| # | Ação | Módulo | Esforço |
|---|------|--------|---------|
| 1 | Registar rota `/contracts/governance` em App.tsx | contracts | Baixo |
| 2 | Registar rota `/contracts/spectral` em App.tsx | contracts | Baixo |
| 3 | Registar rota `/contracts/canonical` em App.tsx | contracts | Baixo |

### 10.2 HIGH — Corrigir a curto prazo

| # | Ação | Módulo | Esforço |
|---|------|--------|---------|
| 4 | Completar i18n para pt-BR (11 namespaces em falta) | i18n | Médio |
| 5 | Completar i18n para es (8 namespaces em falta) | i18n | Médio |
| 6 | Decidir destino das 3 páginas órfãs de catalog | catalog | Baixo |
| 7 | Implementar conteúdo em ProductAnalyticsOverviewPage.tsx | product-analytics | Baixo |

### 10.3 MEDIUM — Planear a médio prazo

| # | Ação | Módulo | Esforço |
|---|------|--------|---------|
| 8 | Adicionar integração API ao módulo operational-intelligence | operational-intelligence | Médio |
| 9 | Granularizar permissão governance:read para sub-domínios | identity-access | Médio |
| 10 | Remover ou integrar ContractPortalPage.tsx (página órfã) | contracts | Baixo |
| 11 | Padronizar estados vazios (EmptyState) em todas as listagens | shared | Médio |

### 10.4 LOW — Melhoria contínua

| # | Ação | Módulo | Esforço |
|---|------|--------|---------|
| 12 | Documentar padrão de acesso a notificações via topbar | docs | Baixo |
| 13 | Completar chave i18n em falta para pt-PT (agents) | i18n | Baixo |
| 14 | Padronizar uso de Loader vs Skeleton | shared | Baixo |

---

## 11. Matriz de Risco

| Risco | Impacto | Probabilidade | Mitigação |
|-------|---------|--------------|-----------|
| Rotas quebradas em produção | ALTO | CERTO | Registar as 3 rotas em App.tsx |
| Utilizadores pt-BR/es sem traduções | MÉDIO | ALTO | Completar ficheiros i18n |
| Páginas órfãs acumulam dívida técnica | BAIXO | MÉDIO | Revisão e remoção/integração |
| Governance sem granularidade de permissão | MÉDIO | BAIXO | Plano de evolução de permissões |

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*  
*Caminho base do frontend: `src/frontend/src/`*  
*Router: `src/frontend/src/App.tsx`*  
*Sidebar: `src/frontend/src/components/shell/AppSidebar.tsx`*
