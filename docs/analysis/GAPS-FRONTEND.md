# Gaps — Frontend
> Análise dos gaps encontrados no frontend React do NexTraceOne.

---

## 1. Cobertura de Testes Unitários — Crítico

**Situação actual:** 13 ficheiros de teste para 278 páginas e 338+ componentes.

**Cobertura estimada:** ~4% dos componentes testados unitariamente.

**Módulos sem qualquer teste unitário:**
- `features/catalog/` — catálogo de serviços, contratos, topology
- `features/ai-hub/` — assistente IA, model registry, agents
- `features/operations/` — incidents, reliability, runbooks
- `features/governance/` — compliance, risk, finops
- `features/change-governance/` — releases, promotion, DORA
- `features/platform-admin/` — user management, branding, integrations

**Risco concreto:** Qualquer refactoring de componente pode quebrar comportamento silenciosamente. Regressões só são detectadas em E2E ou manualmente.

**Threshold de cobertura:** Não está configurado em `vite.config.ts`. O CI não falha por cobertura baixa.

---

## 2. Páginas que Dependem de Features Backend PLANNED

As páginas abaixo existem no frontend mas dependem de features backend marcadas como PLANNED ou não implementadas:

| Página Frontend | Feature Backend Dependente | Status Backend |
|----------------|--------------------------|----------------|
| Integration Hub | IIntegrationContextResolver | PLANNED |
| Change Risk Analysis | IPromotionRiskSignalProvider | PLANNED |
| Change-to-Incident Correlation | IDistributedSignalCorrelationService | PLANNED |
| Licensing & Entitlements | Licensing enforcement | 30% |
| IDE Extensions Management | IDE integration protocol | Foundation apenas |

**Impacto:** Estas páginas provavelmente renderizam sem dados reais ou com estados de erro que não foram desenhados para esse cenário.

---

## 3. Estado Vazio vs. Estado de Erro — Distinção Incompleta

**Observação:** O frontend tem `EmptyState` e `ErrorState` como componentes separados, o que é correcto.

**Gap:** Não há evidência de que páginas dependentes de features PLANNED mostrem estado claro de "funcionalidade não disponível neste plano/configuração" vs. "erro de rede" vs. "sem dados ainda".

**Impacto em UX:** Utilizador não sabe se a feature não existe, se falhou ou se ainda não tem dados.

---

## 4. Sem Zustand — Gestão de Estado Partilhado

**Situação:** O projeto não usa Zustand. Toda a state é gerida via React Contexts + TanStack Query.

**Contextos existentes:** AuthContext, EnvironmentContext, BrandingContext, ThemeContext, PersonaContext.

**Gap:** Não existe store global para:
- Estado de notificações em tempo real (WebSocket/SSE)
- Estado de operações em background (uploads, processamentos longos)
- Estado de selecção multi-item em listas (ex: aprovar múltiplos releases)
- Preferências do utilizador além do tema

**Impacto:** À medida que o produto cresce, a ausência de store global vai forçar prop-drilling ou Context re-renders excessivos.

---

## 5. Ausência de Testes para Componentes Críticos de Segurança

**Componentes sem teste:**
- `ProtectedRoute.tsx` — não está testado unitariamente
- `AuthContext.tsx` — lógica de refresh token não coberta por testes unitários
- `tokenStorage.ts` — tem testes (1 ficheiro encontrado) ✓
- `sanitize.ts` — tem testes (1 ficheiro encontrado) ✓

**Risco:** Erros no fluxo de autenticação e autorização são os mais críticos e os que têm menor cobertura.

---

## 6. Streaming SSE — Sem Testes

**Ficheiro:** `src/features/ai-hub/api/aiGovernance.ts`

O `sendMessageStreaming` usa `fetch` com SSE parsing manual. Não há testes para:
- Parsing de chunks parciais
- Handling de `[DONE]` signal
- AbortController cleanup
- Reconexão após falha de rede

**Impacto:** Bugs no streaming de IA são silenciosos e difíceis de reproduzir.

---

## 7. Playwright E2E — Sem Page Objects

**Estado:** 16 specs E2E existem mas sem padrão de Page Object.

**Consequência:**
- Selectores repetidos em múltiplos ficheiros
- Mudança de um selector exige actualização em N ficheiros
- Testes frágeis a mudanças de UI

---

## 8. Resumo de Prioridades Frontend

| Gap | Severidade | Esforço |
|-----|-----------|---------|
| Cobertura unitária ~4% | P0 — Crítico | Muito alto |
| Páginas com backend PLANNED | P0 — Crítico | Depende do backend |
| Threshold de cobertura no CI | P1 — Alto | Baixo |
| Estado vazio vs erro vs unavailable | P1 — Alto | Médio |
| Testes de AuthContext e ProtectedRoute | P1 — Alto | Médio |
| Testes de SSE streaming | P1 — Alto | Médio |
| Page Objects para Playwright | P2 — Médio | Médio |
| Store global (Zustand) | P2 — Médio | Alto |
