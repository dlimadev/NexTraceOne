# Relatório de Layout e UX — NexTraceOne Frontend

> **Data:** 2026-03-26
> **Foco:** App Shell · Estrutura de páginas · Navegação · Padrões de layout · Hierarquia visual · Fluxos de utilizador

---

## 1. Objetivo

Avaliar se a estrutura de layout, navegação e experiência do utilizador do NexTraceOne suporta adequadamente as tarefas reais de cada persona — com foco em clareza de organização, coerência de padrões e capacidade de tomada de decisão operacional.

---

## 2. App Shell

### Estado Atual

**Ficheiro:** `src/components/shell/AppShell.tsx`

O AppShell implementa um padrão sólido de layout enterprise:
- Sidebar fixa à esquerda com colapso suave (animação `--nto-motion-medium`)
- Topbar horizontal superior
- `EnvironmentBanner` para indicação de ambiente ativo
- `CommandPalette` via Cmd+K
- MobileDrawer para viewport < 1024px
- Outlet (React Router) no `AppContentFrame`

```
┌─────────────────────────────────────────────────┐
│ AppTopbar (global search, actions, user menu)   │
├────────────┬────────────────────────────────────┤
│ AppSidebar │ EnvironmentBanner (condicional)     │
│            ├────────────────────────────────────┤
│ (colapsável│ AppContentFrame → <Outlet />        │
│  lg:fixed) │ (PageContainer → conteúdo da rota) │
│            │                                     │
└────────────┴────────────────────────────────────┘
```

**O que funciona bem:**
- Transição de largura da sidebar com CSS custom property `--shell-sidebar-w` é elegante
- EnvironmentBanner dá contexto de ambiente antes do conteúdo
- Cmd+K acessível globalmente com atalho de teclado

**Problemas identificados:**

| Problema | Gravidade | Evidência |
|----------|-----------|-----------|
| AppTopbar sem breadcrumbs visíveis por rota | Alto | Não há `Breadcrumbs` renderizado dentro do shell — o utilizador perde o fio de navegação em rotas profundas como `/services/:id/contracts/:contractId` |
| Ausência de ContextStrip por entidade | Alto | `ContextStrip.tsx` existe em `src/components/shell/` mas não está integrado no shell principal — o utilizador não sabe em que serviço/contrato/ambiente está ao navegar |
| Inline `<style>` no AppShell | Baixo | `AppShell.tsx:81-85` — injeção de `<style>` para controlar margin-left via CSS custom property; solução funcional mas pouco idiomática com Tailwind 4 |

---

## 3. Sidebar — Análise Profunda

**Ficheiro:** `src/components/shell/AppSidebar.tsx`

### Volume de Navegação

A sidebar contém **50 itens** distribuídos em **12 seções**. Este volume é o problema central de UX da navegação:

```
home        (1 item)
services    (2 itens)
knowledge   (2 itens)
contracts   (6 itens)
changes     (4 itens)
operations  (5 itens)
aiHub       (9 itens)
governance  (10 itens)
organization (2 itens)
analytics   (1 item)
integrations (1 item)
admin       (8 itens)
```

**10 itens em governance** e **9 itens em aiHub** são excessivos para a navegação principal. Para personas como Executive ou Engineer, a maioria destes itens é ruído.

A implementação do sistema de persona (`config.sectionOrder`) está correta — o problema é que a filtragem não reduz suficientemente o número de itens visíveis para cada papel.

### Ícones Duplicados

| Rota | Ícone | Conflito |
|------|-------|---------|
| `/ai/assistant` | `<Bot size={18} />` | Idêntico a `/ai/agents` |
| `/ai/agents` | `<Bot size={18} />` | Idêntico a `/ai/assistant` |
| `/releases` | `<Zap size={18} />` | Idêntico a `/operations/automation` |
| `/operations/automation` | `<Zap size={18} />` | Idêntico a `/releases` |

Ícones duplicados prejudicam a leitura escaneável da sidebar, especialmente quando colapsada (modo icon-only).

### Ausência de Badge de Alerta

A sidebar não mostra contadores de alertas ativos (ex: "3 incidentes abertos", "2 mudanças pendentes") nos itens relevantes. Plataformas enterprise de referência usam este padrão para guiar a atenção sem abrir cada módulo.

---

## 4. Padrões de Layout de Páginas

### 4.1 Páginas de Detalhe de Entidade

**Padrão atual (inconsistente):**

| Página | Container | Header | Coluna lateral | Tabs |
|--------|-----------|--------|----------------|------|
| `ServiceDetailPage` | `div.p-6` (manual) | Manual (div inline) | Sim (3+1 grid) | Não |
| `IncidentDetailPage` | `PageContainer` | Manual (div inline) | Não (2+2 grid) | Não |
| `ExecutiveOverviewPage` | `PageContainer` | `PageHeader` component | Não | Não |

**Problema:** Cada página de detalhe reinventa a estrutura. Não existe template de `EntityDetailLayout`.

**Padrão recomendado para detalhe de entidade:**

```
EntityHeader
  ├── Nome da entidade
  ├── Tipo / Criticidade / Status (badges)
  ├── Owner / Team
  └── Quick Actions (botões primários)

EntityContextRibbon
  ├── Ambiente ativo
  ├── Última modificação
  └── Metadados chave

ContentTabs (ou scroll-sections)
  ├── Tab: Visão geral
  ├── Tab: Dependências
  ├── Tab: Mudanças/Timeline
  ├── Tab: Incidentes
  └── Tab: Contratos

AssistantPanel (collapsível)
```

### 4.2 Páginas de Lista/Catálogo

**O que funciona:**
- `FilterBar.tsx` existe como componente
- `TableWrapper.tsx` existe como container
- Padrões de loading com Skeleton estão implementados

**O que falta:**
- Sem padrão de "side panel de detalhe rápido" (split view) — o utilizador navega para página de detalhe em vez de ver preview lateral
- Sem "saved views" ou filtros persistidos
- Sem colunas configuráveis

### 4.3 Dashboards

**`DashboardPage.tsx` — Problemas de Layout**

O dashboard atual apresenta:
1. Barra de contexto (título + badge de persona) — **Bom**
2. QuickActions (links rápidos) — **Bom**
3. PersonaQuickstart (onboarding) — **Bom mas deveria desaparecer após uso**
4. 5 KPI StatCards — **Funcional mas sem drill-down**
5. Alertas de atenção — **Bom (contextual e semântico)**
6. HomeWidgetCards (persona-specific) — **Bom**
7. Cards operacionais (Services + Contracts + Changes + Incidents) — **Fraco**

O problema dos cards operacionais (secção 7): são grade de números sem narrativa. "Pending: 3 / Approved: 12 / Rejected: 1" são contadores sem contexto de tendência, sem ação clara, sem indicação de se a situação é boa ou má.

**`ExecutiveOverviewPage.tsx` — Problemas de Layout**

O dashboard executivo empilha 5 cards verticalmente com conteúdo idêntico em estrutura:
```
[Operational Trend Card] → 3 StatCards em grid
[Risk Summary Card]       → 2 StatCards + 2 Badge cards em grid
[Maturity Summary Card]   → 4 progress bars em grid
[Change Safety Card]      → 3 StatCards + 1 Badge card em grid
[Incident Trend Card]     → 4 StatCards + 1 Badge card em grid
```

Este layout:
- Torna todos os blocos visualmente equivalentes (sem hierarquia de prioridade)
- Não usa gráficos apesar de ECharts estar disponível
- Não distingue o que exige ação imediata do que é informação de contexto
- Falha em comunicar a narrativa executiva: "O que preciso de saber? O que preciso de fazer?"

---

## 5. Navegação em Profundidade

### Rotas Sem Breadcrumbs

Rotas como:
- `/services/:serviceId` — sem breadcrumb "Serviços > Nome do Serviço"
- `/contracts/studio/:contractId` — sem trail de navegação
- `/governance/teams/:teamId` — sem trail
- `/operations/incidents/:incidentId` — usa "← Back" mas não breadcrumb contextual

**Impacto:** Em sessões longas, o utilizador perde o contexto de onde está no produto.

### Back Link vs Breadcrumbs

`ServiceDetailPage.tsx:116-120` e `IncidentDetailPage.tsx:167-169` usam um padrão de "← Back" em vez de breadcrumbs. Este padrão:
- Funciona para navegação de 1 nível
- Falha quando o utilizador chegou via busca global ou link direto
- Não mostra o caminho completo (Domain > Service > API, por exemplo)

### Command Palette

`CommandPalette.tsx` existe e está integrado no AppShell com Cmd+K. Esta é uma decisão enterprise correta — permite navegação rápida sem dependência exclusiva da sidebar.

**Não foi possível verificar o conteúdo completo** (o ficheiro não foi lido), mas a presença do componente é positiva.

---

## 6. Padrões de UX Operacional

### 6.1 Contexto Operacional

| Padrão | Presente | Qualidade |
|--------|----------|-----------|
| Indicador de ambiente ativo | Sim (`EnvironmentBanner`) | Bom |
| Owner visível na entidade | Sim (ServiceDetailPage, IncidentDetailPage) | Adequado |
| Criticidade visível | Sim (badges) | Inconsistente (ver inconsistências) |
| Risco visível na lista | Parcial | Apenas em tabelas específicas |
| Timeline de eventos | Sim (IncidentDetailPage inline) | Fraco — sem componente reutilizável |
| Correlação de mudanças | Sim (IncidentDetailPage) | Bom — contexto rico |
| Evidência operacional | Sim (IncidentDetailPage) | Bom |

### 6.2 Fluxo de Aprovação / Review

Existe `ReviewPanel` mencionado na visão do produto mas não encontrado como componente. O fluxo de aprovação de contratos (workspace) existe como conjunto de seções, mas sem padrão visual unificado de "Review".

### 6.3 GUIDs Expostos ao Utilizador

**Achado:** `IncidentDetailPage.tsx:354` — `svc.serviceId` mostrado como sub-texto nos itens de lista de serviços impactados: `{svc.serviceId} · {svc.serviceType}`. O `serviceId` é provavelmente um UUID e a sua exposição direta ao utilizador é desnecessária. Deveria ser substituído por um identificador humano (nome do serviço, código de referência).

---

## 7. Estados de Interface

### Estados Presentes e Bem Implementados
- Loading: `PageLoadingState`, `Skeleton`, skeletons inline
- Erro: `PageErrorState`, `ErrorState`, `ErrorBoundary`
- Vazio: `EmptyState` com ação e contexto

### Estado de Módulo Indisponível
- `ModuleUnavailable.tsx` existe — boa prática: não remover silenciosamente funcionalidade preview

### Estado de Onboarding
- `PersonaQuickstart.tsx` e `OnboardingHints.tsx` existem — adequados mas precisam de condição de "dispensar" para não poluir sessões de utilizadores experientes

---

## 8. Densidade de Informação

| Área | Densidade | Avaliação |
|------|-----------|-----------|
| Dashboard principal | Média | Adequada — nem sobrecarga nem vazio |
| ServiceDetailPage | Média-Alta | Aceitável mas sem tabs fragmenta em scroll longo |
| IncidentDetailPage | Alta | Densa mas justificada — contexto operacional rico |
| ExecutiveOverviewPage | Baixa | Subutiliza o espaço — cards com muito branco, sem charts |
| Sidebar (expandida) | Alta | Excessiva para personas não-técnicas |

---

## 9. Recomendações de Layout

### 9.1 Criar EntityDetailLayout template
Template padronizado para ServiceDetailPage, IncidentDetailPage, TeamDetailPage, DomainDetailPage, AgentDetailPage.

### 9.2 Integrar Breadcrumbs no AppShell
Adicionar `Breadcrumbs` como filho do `AppContentFrame`, lendo a rota ativa do React Router.

### 9.3 Adicionar contadores de alerta na sidebar
Items como `incidents`, `changes`, `governance/risk` deveriam mostrar badge numérico com contagem de itens a exigir atenção.

### 9.4 Redesenhar ExecutiveOverview com hierarquia
- Secção hero: 3 métricas críticas com maior tipografia
- Secção de alertas: "O que exige ação agora" com links diretos
- Gráficos de tendência: linha/barra por período
- Secção informativa: maturidade, maturity progress bars

### 9.5 Split View nas listas
Para ServiceCatalogListPage, ContractCatalogPage, IncidentsPage: implementar padrão de lista + painel lateral de detalhe rápido (sem navegar para nova rota).

### 9.6 Reduzir densidade da sidebar por persona
Engineer: serviços, mudanças, operações, AI
Executive: dashboard, governance, reports
Auditor: compliance, evidence, audit, governance
