# Relatório de UX e Layout do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Auditoria de consistência UX, padrões de layout, estados visuais e gaps  
> **Status global:** GAP_IDENTIFIED  
> **Design system:** `src/frontend/src/shared/design-system/`  
> **Componentes UI:** `src/frontend/src/components/`

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Componentes reutilizáveis | 68 |
| Componentes UI base | 27 |
| Componentes de shell/layout | 18 |
| Componentes de formulário | 8 |
| Componentes de estado | 5 |
| Componentes complexos | 10 |
| Personas com UX diferenciada | 7 |
| Padrões de layout identificados | 5 |

---

## 2. Inventário de Componentes Reutilizáveis

### 2.1 Componentes UI Base (27)

| Componente | Ficheiro | Propósito | Uso estimado |
|-----------|---------|-----------|-------------|
| Button | `components/Button.tsx` | Botão primário, secundário, ghost | Alto |
| IconButton | `components/IconButton.tsx` | Botão com ícone | Alto |
| Badge | `components/Badge.tsx` | Labels e indicadores | Alto |
| Card | `components/Card.tsx` | Container visual de conteúdo | Alto |
| Divider | `components/Divider.tsx` | Separador visual | Médio |
| Tooltip | `components/Tooltip.tsx` | Informação contextual | Alto |
| Loader | `components/Loader.tsx` | Indicador de carregamento | Alto |
| Skeleton | `components/Skeleton.tsx` | Placeholder de carregamento | Médio |
| Modal | `components/Modal.tsx` | Diálogo modal | Alto |
| Drawer | `components/Drawer.tsx` | Painel lateral | Médio |
| Tabs | `components/Tabs.tsx` | Navegação por tabs | Alto |
| Breadcrumbs | `components/Breadcrumbs.tsx` | Navegação hierárquica | Alto |
| Typography | `components/Typography.tsx` | Texto tipográfico padronizado | Alto |
| InlineMessage | `components/InlineMessage.tsx` | Mensagens inline (info, warning, error) | Médio |
| StatCard | `components/StatCard.tsx` | Card de estatística | Médio |
| HomeWidgetCard | `components/HomeWidgetCard.tsx` | Widget da home por persona | Médio |
| DemoBanner | `components/DemoBanner.tsx` | Banner de demonstração | Baixo |
| QuickActions | `components/QuickActions.tsx` | Ações rápidas por persona | Médio |
| OnboardingHints | `components/OnboardingHints.tsx` | Dicas de onboarding | Baixo |
| PersonaQuickstart | `components/PersonaQuickstart.tsx` | Quickstart por persona | Baixo |
| CommandPalette | `components/CommandPalette.tsx` | Paleta de comandos (21.5 KB) | Alto |
| ErrorBoundary | `components/ErrorBoundary.tsx` | Fronteira de erro React | Alto |
| ReleaseScopeGate | `components/ReleaseScopeGate.tsx` | Feature flag visual | Médio |
| ModuleHeader | `components/ModuleHeader.tsx` | Header de módulo | Alto |
| TextField | `components/TextField.tsx` | Input de texto | Alto |
| TextArea | `components/TextArea.tsx` | Área de texto multi-linha | Médio |
| SearchInput | `components/SearchInput.tsx` | Input de busca | Alto |

### 2.2 Componentes de Formulário (8)

| Componente | Ficheiro | Propósito |
|-----------|---------|-----------|
| TextField | `components/TextField.tsx` | Input de texto com label |
| TextArea | `components/TextArea.tsx` | Texto multi-linha |
| Select | `components/Select.tsx` | Dropdown de seleção |
| Checkbox | `components/Checkbox.tsx` | Caixa de verificação |
| Radio | `components/Radio.tsx` | Botão de rádio |
| Toggle | `components/Toggle.tsx` | Interruptor on/off |
| FilterChip | `components/FilterChip.tsx` | Filtro tipo chip |
| PasswordInput | `components/PasswordInput.tsx` | Input de senha com toggle |

### 2.3 Componentes de Shell/Layout (18)

| Componente | Ficheiro | Propósito |
|-----------|---------|-----------|
| AppShell | `components/shell/AppShell.tsx` | Layout principal da aplicação |
| AppSidebar | `components/shell/AppSidebar.tsx` | Menu lateral |
| AppSidebarHeader | `components/shell/AppSidebarHeader.tsx` | Header do sidebar |
| AppSidebarGroup | `components/shell/AppSidebarGroup.tsx` | Grupo de itens no sidebar |
| AppSidebarItem | `components/shell/AppSidebarItem.tsx` | Item individual do sidebar |
| AppSidebarFooter | `components/shell/AppSidebarFooter.tsx` | Footer do sidebar |
| AppTopbar | `components/shell/AppTopbar.tsx` | Barra superior |
| AppTopbarSearch | `components/shell/AppTopbarSearch.tsx` | Busca na topbar |
| AppTopbarActions | `components/shell/AppTopbarActions.tsx` | Ações na topbar |
| AppUserMenu | `components/shell/AppUserMenu.tsx` | Menu do utilizador |
| AppContentFrame | `components/shell/AppContentFrame.tsx` | Frame de conteúdo |
| WorkspaceSwitcher | `components/shell/WorkspaceSwitcher.tsx` | Seleção de tenant |
| EnvironmentBanner | `components/shell/EnvironmentBanner.tsx` | Banner de ambiente |
| MobileDrawer | `components/shell/MobileDrawer.tsx` | Drawer para mobile |
| ContextStrip | `components/shell/ContextStrip.tsx` | Faixa de contexto |
| ContentGrid | `components/shell/ContentGrid.tsx` | Grid de conteúdo |
| FilterBar | `components/shell/FilterBar.tsx` | Barra de filtros |
| DetailPanel | `components/shell/DetailPanel.tsx` | Painel de detalhe |

### 2.4 Componentes de Estado (5)

| Componente | Ficheiro | Propósito |
|-----------|---------|-----------|
| PageLoadingState | `components/PageLoadingState.tsx` | Estado de carregamento de página |
| PageErrorState | `components/PageErrorState.tsx` | Estado de erro de página |
| PageStateDisplay | `components/PageStateDisplay.tsx` | Display genérico de estado |
| EmptyState | `components/EmptyState.tsx` | Estado vazio (sem dados) |
| ErrorState | `components/ErrorState.tsx` | Estado de erro genérico |

---

## 3. Padrões de Layout

### 3.1 Padrões identificados

| Padrão | Componentes | Uso típico | Consistência |
|--------|-----------|-----------|--------------|
| Página de listagem | PageHeader + FilterBar + TableWrapper | Catálogos, listas | ⚠️ PARCIAL |
| Página de detalhe | PageHeader + PageContainer + PageSection + DetailPanel | Detalhe de serviço, contrato | ✅ CONSISTENTE |
| Dashboard/Overview | ContentGrid + StatCard + HomeWidgetCard | Home, executive overview | ✅ CONSISTENTE |
| Página de formulário | PageHeader + PageContainer + Form components | Criação, edição | ⚠️ PARCIAL |
| Página de configuração | PageHeader + Tabs + Form | Admin, configuração | ✅ CONSISTENTE |

### 3.2 Hierarquia de layout

```
AppShell
├── AppSidebar
│   ├── AppSidebarHeader (logo + workspace)
│   ├── AppSidebarGroup × N
│   │   └── AppSidebarItem × N
│   └── AppSidebarFooter
├── AppTopbar
│   ├── AppTopbarSearch
│   ├── EnvironmentBanner
│   ├── ContextStrip
│   └── AppTopbarActions
│       └── AppUserMenu
└── AppContentFrame
    └── [Page Component]
        ├── PageHeader / ModuleHeader
        ├── PageContainer
        │   ├── PageSection × N
        │   ├── ContentGrid
        │   ├── FilterBar + TableWrapper
        │   └── DetailPanel (opcional)
        └── PageLoadingState / PageErrorState / EmptyState
```

---

## 4. Gaps de UX Identificados

### 4.1 Estados visuais

| Estado | Componente disponível | Cobertura estimada | Gap |
|--------|---------------------|-------------------|-----|
| Loading (carregamento) | PageLoadingState, Loader, Skeleton | ~80% das páginas | ⚠️ PARCIAL |
| Error (erro) | PageErrorState, ErrorState, ErrorBoundary | ~70% das páginas | ⚠️ PARCIAL |
| Empty (sem dados) | EmptyState | ~50% das páginas | ❌ GAP |
| Success (sucesso) | InlineMessage | ~40% das ações | ❌ GAP |
| Permission denied | UnauthorizedPage, ProtectedRoute redirect | ~95% das rotas | ✅ ADEQUADO |

### 4.2 Detalhes dos gaps

| # | Gap | Detalhe | Módulos afetados | Prioridade | Status |
|---|-----|---------|-----------------|------------|--------|
| 1 | EmptyState inconsistente | Nem todas as listagens implementam EmptyState quando não há dados | catalog, governance, operations | MEDIUM | GAP_IDENTIFIED |
| 2 | Loader vs Skeleton | Sem padrão definido para usar Loader (spinner) vs Skeleton (placeholder) | Todos | LOW | GAP_IDENTIFIED |
| 3 | Feedback de sucesso | Ações de criação/edição nem sempre mostram feedback visual de sucesso | contracts, governance | MEDIUM | GAP_IDENTIFIED |
| 4 | Rotas quebradas sem fallback | 3 rotas do contracts levam a página em branco — sem 404 gracioso | contracts | CRITICAL | GAP_IDENTIFIED |
| 5 | Página vazia | ProductAnalyticsOverviewPage.tsx (raiz) com 0 bytes | product-analytics | MEDIUM | GAP_IDENTIFIED |
| 6 | Mobile responsiveness | MobileDrawer existe mas cobertura mobile não é completa | Todos | LOW | IN_ANALYSIS |
| 7 | Breadcrumbs inconsistentes | Nem todas as páginas internas implementam breadcrumbs | governance, operations | LOW | GAP_IDENTIFIED |

---

## 5. Formulários

### 5.1 Componentes disponíveis

| Componente | Validação | i18n | Acessibilidade | Status |
|-----------|----------|------|----------------|--------|
| TextField | ✅ | ✅ (label, placeholder) | ✅ (aria-label) | OK |
| TextArea | ✅ | ✅ | ✅ | OK |
| Select | ✅ | ✅ | ✅ | OK |
| Checkbox | ✅ | ✅ | ✅ | OK |
| Radio | ✅ | ✅ | ✅ | OK |
| Toggle | ✅ | ✅ | ✅ | OK |
| PasswordInput | ✅ | ✅ | ✅ | OK |
| SearchInput | ⚠️ | ✅ | ✅ | OK |

### 5.2 Gaps em formulários

| # | Gap | Detalhe | Prioridade |
|---|-----|---------|------------|
| 1 | Validação client-side | Nem todos os formulários implementam validação antes de submit | MEDIUM |
| 2 | Mensagens de erro inline | Alguns formulários podem mostrar erros genéricos em vez de inline | LOW |
| 3 | Autosave | Formulários complexos (Contract Studio) devem ter autosave — verificar | MEDIUM |

---

## 6. Tabelas e Listagens

### 6.1 Componentes disponíveis

| Componente | Ficheiro | Funcionalidades |
|-----------|---------|----------------|
| TableWrapper | `components/shell/TableWrapper.tsx` | Wrapper para tabelas com estilo consistente |
| FilterBar | `components/shell/FilterBar.tsx` | Barra de filtros para listagens |
| FilterChip | `components/FilterChip.tsx` | Chips de filtro removíveis |
| StatsGrid | `components/shell/StatsGrid.tsx` | Grid de estatísticas |

### 6.2 Gaps em tabelas

| # | Gap | Detalhe | Prioridade |
|---|-----|---------|------------|
| 1 | Paginação | Verificar se todas as listagens implementam paginação | MEDIUM |
| 2 | Ordenação | Verificar se colunas são ordenáveis onde faz sentido | LOW |
| 3 | Busca inline | Nem todas as tabelas podem ter busca inline | LOW |
| 4 | Export | Funcionalidade de exportação (CSV, Excel) não verificada | LOW |
| 5 | Seleção em lote | Ações em lote em listagens não verificadas | LOW |

---

## 7. Exposição de Dados Técnicos

### 7.1 Avaliação

| Área | Risco | Detalhe | Prioridade |
|------|-------|---------|------------|
| IDs técnicos na URL | BAIXO | UUIDs em rotas como `/services/:serviceId` — padrão aceitável | LOW |
| Dados sensíveis em sessão | MÉDIO | Access token em sessionStorage — design intencional | MEDIUM |
| Refresh token | BAIXO | Apenas em memória — adequado | LOW |
| Stack traces | BAIXO | ErrorBoundary e ErrorState previnem exposição | LOW |
| Permissões no frontend | BAIXO | Frontend apenas controla UI — backend autoriza | LOW |

### 7.2 Recomendações

| # | Recomendação | Prioridade |
|---|-------------|------------|
| 1 | Verificar que ErrorBoundary não expõe stack traces em produção | MEDIUM |
| 2 | Garantir que mensagens de erro da API são sanitizadas antes de exibir | MEDIUM |
| 3 | Confirmar que console.log não expõe tokens ou dados sensíveis em produção | HIGH |

---

## 8. UX por Persona

### 8.1 Elementos persona-aware

| Elemento | Personalizado por persona | Ficheiro |
|---------|:------------------------:|---------|
| Home/Dashboard | ✅ | `DashboardPage.tsx` + `PersonaContext` |
| Sidebar (ordem das secções) | ✅ | `AppSidebar.tsx` |
| Quick Actions | ✅ | `QuickActions.tsx` |
| Widgets | ✅ | `HomeWidgetCard.tsx` |
| Onboarding | ✅ | `OnboardingHints.tsx` |
| Persona Quickstart | ✅ | `PersonaQuickstart.tsx` |
| Linguagem da UI | ⚠️ | Via i18n (parcial — falta namespace `persona` em pt-BR) |
| Escopo de dados | ⚠️ | Controlado por permissão, não diretamente por persona |
| Relatórios | ⚠️ | Filtragem básica — sem personalização avançada por persona |

### 8.2 Mapeamento persona → role

| Persona | Role (backend) | Prioridade de secções |
|---------|---------------|----------------------|
| Engineer | Developer | services, contracts, changes |
| TechLead | TechLead | services, contracts, changes, governance |
| Architect | SecurityReview | contracts, governance, operations |
| Product | Viewer | analytics, governance, knowledge |
| Executive | ApprovalOnly | governance (executive), analytics |
| PlatformAdmin | PlatformAdmin | admin, operations, integrations |
| Auditor | Auditor | audit, governance, compliance |

---

## 9. Design System

### 9.1 Estrutura

| Ficheiro | Propósito |
|---------|-----------|
| `shared/design-system/foundations.ts` | Tokens de design (cores, espaçamento, tipografia) |
| `shared/design-system/tokens.ts` | Tokens de cor e tema |
| `shared/design-system/index.ts` | Exportação centralizada |
| `shared/tokens/color-migration-guide.ts` | Guia de migração de cores |
| `shared/design-system/README.md` | Documentação do design system |

### 9.2 Observações

- Design system emergente — documentação existe mas pode não estar completa
- Tokens de cor definidos centralmente — boa prática
- Guia de migração sugere evolução recente do sistema de cores
- Sem Storybook ou catálogo visual de componentes (gap potencial)

---

## 10. Consolidação de Gaps e Recomendações

### 10.1 CRITICAL

| # | Gap | Ação | Esforço |
|---|-----|------|---------|
| 1 | 3 rotas quebradas em contracts | Registar rotas em App.tsx | Baixo |
| 2 | Página em branco sem fallback 404 | Adicionar catch-all route com página de erro | Baixo |

### 10.2 HIGH

| # | Gap | Ação | Esforço |
|---|-----|------|---------|
| 3 | Console.log em produção | Auditoria de console statements e remoção/condicional | Médio |
| 4 | Sanitização de erros API | Verificar que mensagens de erro da API são tratadas | Médio |

### 10.3 MEDIUM

| # | Gap | Ação | Esforço |
|---|-----|------|---------|
| 5 | EmptyState inconsistente | Padronizar uso de EmptyState em todas as listagens | Médio |
| 6 | Feedback de sucesso | Implementar padrão de toast/notification para ações de sucesso | Médio |
| 7 | Validação de formulários | Verificar e completar validação client-side | Médio |
| 8 | Ficheiro vazio | Implementar ou remover ProductAnalyticsOverviewPage.tsx (raiz) | Baixo |

### 10.4 LOW

| # | Gap | Ação | Esforço |
|---|-----|------|---------|
| 9 | Loader vs Skeleton | Definir guideline de quando usar cada um | Baixo |
| 10 | Breadcrumbs | Padronizar breadcrumbs em todas as sub-páginas | Médio |
| 11 | Storybook | Considerar implementação de catálogo visual de componentes | Alto |
| 12 | Mobile | Auditar e melhorar responsividade | Alto |

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
