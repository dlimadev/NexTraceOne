> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Frontend Shared / Shell / App Host — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
106 páginas, 14 feature modules, 4 locales, TanStack Router/Query, Axios API client com JWT + tenant headers. Infraestrutura frontend sólida. Gaps concentrados em: error handling (30 páginas), empty states (52+ páginas), componentes não usados, e DashboardPage genérico.

## 2. Gaps críticos
Nenhum gap crítico.

## 3. Gaps altos

### 3.1 DashboardPage genérica sem semântica de persona
- **Severidade:** HIGH
- **Classificação:** WRONG_DESIGN
- **Descrição:** `DashboardPage.tsx` em `src/frontend/src/features/shared/pages/` é genérica. Não reflecte ownership, ambiente, serviços do utilizador autenticado de forma clara. Não muda conteúdo por persona.
- **Impacto:** Primeira experiência do utilizador é genérica, sem valor contextual. Contradiz princípio §6 (Persona awareness obrigatório) e §5 (Source of Truth por desenho).
- **Evidência:** `src/frontend/src/features/shared/pages/DashboardPage.tsx`

## 4. Gaps médios

### 4.1 30 Páginas sem Error Handling
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** 30 de 106 páginas não têm `isError`/`ErrorBoundary` handling. Distribuição: AI Hub (4), Catalog (1), Change Governance (1), Contracts (1), Governance (16), Identity Access (10, maioria auth flows), Notifications (1), Operational Intelligence (1), Product Analytics (1).
- **Impacto:** Erros de API são silenciosos em 28% das páginas.
- **Evidência:** Scan automatizado de todos os `*Page.tsx` files

### 4.2 52+ Páginas sem Empty State Pattern
- **Severidade:** MEDIUM
- **Classificação:** INCOMPLETE
- **Descrição:** 52 de 106 páginas não têm empty state pattern (noData/noResults/emptyState). Maior concentração em Governance (18 páginas) e Identity Access (12 páginas).
- **Impacto:** UX inconsistente — utilizador vê conteúdo vazio sem feedback.
- **Evidência:** Scan automatizado

### 4.3 `DemoBanner.tsx` — Componente Morto
- **Severidade:** LOW
- **Classificação:** CLEANUP_REQUIRED
- **Descrição:** `DemoBanner.tsx` existe em `src/frontend/src/components/` e está exportado via `index.ts`. Porém **zero importações** em qualquer feature page.
- **Impacto:** Código morto. Confusão para developers.
- **Evidência:** `src/frontend/src/components/DemoBanner.tsx` — zero imports fora de `components/index.ts`

### 4.4 Knowledge Module sem Frontend
- **Severidade:** HIGH
- **Classificação:** INCOMPLETE
- **Descrição:** Nenhuma pasta `knowledge` em `src/frontend/src/features/`. Backend Knowledge funcional sem UI.
- **Evidência:** `src/frontend/src/features/` — 14 feature modules, nenhum `knowledge`

## 5. Itens mock / stub / placeholder
**Zero mock inline em páginas de produção.** Todos os mock references encontrados estão em ficheiros `.test.` apenas.

**CORRECÇÃO DE REGISTO IMPORTANTE:** O `frontend-state-report.md` de Março 2026 afirma:
- "IncidentsPage.tsx usa mockIncidents hardcoded inline" — **FALSO**
- "AiAssistantPage.tsx usa mockConversations hardcoded" — **FALSO**
- "9 páginas com mock inline" — **FALSO** (zero em produção)
- "Governance 25 páginas com DemoBanner" — **FALSO** (DemoBanner não é importado por nenhuma feature page)

## 6. Erros de desenho / implementação incorreta
- `DashboardPage` genérica sem persona awareness — contradiz visão oficial

## 7. N/A (frontend-only)

## 8-12. N/A (frontend-only)

## 13. Ações corretivas obrigatórias
1. Redesenhar `DashboardPage` com semântica de persona (engineer, tech lead, exec, admin)
2. Adicionar error handling (`isError` pattern) a 30 páginas
3. Adicionar empty state pattern a 52+ páginas
4. Remover `DemoBanner.tsx` (dead component)
5. Criar feature module `knowledge` no frontend
6. Actualizar `frontend-state-report.md` (completamente desactualizado)
