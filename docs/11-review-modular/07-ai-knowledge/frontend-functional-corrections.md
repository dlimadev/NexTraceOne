# PARTE 9 — Frontend Functional Corrections

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Inventário de páginas

| # | Página | Rota | Ficheiro | LOC | Estado |
|---|--------|------|----------|-----|--------|
| FE-01 | AI Assistant | `/ai/assistant` | `AiAssistantPage.tsx` | ~57K | ⚠️ Chat funcional, mock response generator presente |
| FE-02 | AI Agents | `/ai/agents` | `AiAgentsPage.tsx` | ~28K | ⚠️ Listagem OK, tools cosméticos |
| FE-03 | Agent Detail | `/ai/agents/:agentId` | `AgentDetailPage.tsx` | ~25K | ⚠️ Detail OK, execução parcial |
| FE-04 | Model Registry | `/ai/models` | `ModelRegistryPage.tsx` | ~8.8K | ⚠️ Listagem OK, registo desativado |
| FE-05 | AI Policies | `/ai/policies` | `AiPoliciesPage.tsx` | ~7K | ⚠️ CRUD parcial |
| FE-06 | AI Routing | `/ai/routing` | `AiRoutingPage.tsx` | ~17K | ⚠️ UI completa, backend incerto |
| FE-07 | IDE Integrations | `/ai/ide` | `IdeIntegrationsPage.tsx` | ~17K | ❌ Preview — sem backend real |
| FE-08 | Token Budgets | `/ai/budgets` | `TokenBudgetPage.tsx` | ~7.3K | ⚠️ UI OK, enforcement ausente |
| FE-09 | AI Audit | `/ai/audit` | `AiAuditPage.tsx` | ~9.1K | ⚠️ Listagem parcial |
| FE-10 | AI Analysis | `/ai/analysis` | `AiAnalysisPage.tsx` | ~22K | ⚠️ Preview mode |
| FE-11 | AI Config (admin) | `/platform/configuration/ai-integrations` | `AiIntegrationsConfigurationPage.tsx` | ~21K | ✅ Funcional |

---

## 2. Rotas e menu

### 2.1 Rotas (App.tsx)

Todas as 10 rotas estão registadas em `App.tsx` com `ProtectedRoute` e permissões corretas.

✅ Sem rotas mortas.

### 2.2 Menu lateral (AppSidebar.tsx)

| # | Label | Rota | Permissão | Problema |
|---|-------|------|-----------|----------|
| M-01 | AI Assistant | `/ai/assistant` | ai:assistant:read | ✅ OK |
| M-02 | AI Agents | `/ai/agents` | ai:assistant:read | ✅ OK |
| M-03 | Model Registry | `/ai/models` | ai:governance:read | ⚠️ Registo desativado — confuso para utilizador |
| M-04 | AI Policies | `/ai/policies` | ai:governance:read | ✅ OK |
| M-05 | AI Routing | `/ai/routing` | ai:governance:read | ⚠️ Backend incerto |
| M-06 | IDE Integrations | `/ai/ide` | ai:governance:read | 🔴 Preview sem backend — esconder ou badge |
| M-07 | Token Budgets | `/ai/budgets` | ai:governance:read | ⚠️ Sem enforcement visual |
| M-08 | AI Audit | `/ai/audit` | ai:governance:read | ✅ OK |
| M-09 | AI Analysis | `/ai/analysis` | ai:runtime:write | ⚠️ Preview — backend parcial |

**Problema:** 9 items no menu para um módulo com 25% de backend. Consolidated review recomendou reduzir para 5-6.

---

## 3. Revisão de dashboards/painéis

| # | Painel | Página | Dados reais? | Problema |
|---|--------|--------|-------------|----------|
| D-01 | Chat panel | AiAssistantPage | ⚠️ Parcial | `AssistantPanel.tsx` contém mock response generator |
| D-02 | Agent list + execute | AiAgentsPage | ⚠️ Parcial | Listagem real, execução sem tools |
| D-03 | Model list | ModelRegistryPage | ⚠️ Parcial | Listagem real, registo desativado |
| D-04 | Policy list | AiPoliciesPage | ⚠️ Parcial | CRUD parcial |
| D-05 | Routing dashboard | AiRoutingPage | ⚠️ Incerto | Backend provavelmente stub |
| D-06 | IDE dashboard | IdeIntegrationsPage | ❌ Mock | Sem extensões reais |
| D-07 | Budget dashboard | TokenBudgetPage | ⚠️ Parcial | Sem enforcement |
| D-08 | Audit log | AiAuditPage | ⚠️ Parcial | Dados parciais |
| D-09 | Analysis panel | AiAnalysisPage | ⚠️ Preview | Backend parcial |

---

## 4. Revisão de i18n

| Aspecto | Estado |
|---------|--------|
| Sidebar labels | ✅ Chaves i18n (`sidebar.aiAssistant`, etc.) |
| Page titles | ⚠️ Verificar se todas as páginas usam chaves i18n |
| Form labels | ⚠️ Verificar |
| Error messages | ⚠️ Verificar — podem estar hardcoded |
| Empty states | ⚠️ Verificar |
| Tooltips | ⚠️ Verificar |

---

## 5. Problemas identificados

### 5.1 Mock response generator

**Ficheiro:** `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx`

O componente AssistantPanel contém lógica de geração de resposta mock que simula respostas de IA sem chamar o backend. Isto mascara falhas reais e deve ser removido.

### 5.2 Botões desativados

| Página | Botão | Problema |
|--------|-------|----------|
| ModelRegistryPage | "Register Model" | Desativado sem explicação visível |
| IdeIntegrationsPage | "Install Extension" | Cosmético — sem extensão real |

### 5.3 Exposição de campos técnicos

| Página | Campo | Problema |
|--------|-------|----------|
| Agent Detail | `AllowedTools` (lista JSON) | Exposição técnica — deveria ser apresentado como lista legível |
| Routing | `CriteriaJson` | Campo JSON bruto exposto |

---

## 6. Backlog de correções frontend

| # | Item | Prioridade | Esforço | Ficheiro |
|---|------|------------|---------|----------|
| FE-B01 | Remover mock response generator do AssistantPanel | P0 | 2h | `AssistantPanel.tsx` |
| FE-B02 | Implementar streaming UI para chat (SSE) | P1 | 8h | `AiAssistantPage.tsx` |
| FE-B03 | Esconder ou marcar IDE Integrations como "coming soon" | P1 | 1h | `AppSidebar.tsx`, `IdeIntegrationsPage.tsx` |
| FE-B04 | Reduzir menu de 9 para 6 items | P1 | 2h | `AppSidebar.tsx` |
| FE-B05 | Ativar registo de modelo no frontend (ou remover botão) | P2 | 2h | `ModelRegistryPage.tsx` |
| FE-B06 | Substituir JSON bruto por apresentação legível em Agent Detail | P2 | 2h | `AgentDetailPage.tsx` |
| FE-B07 | Substituir JSON bruto por apresentação legível em Routing | P2 | 2h | `AiRoutingPage.tsx` |
| FE-B08 | Validar todas as páginas para i18n completo | P2 | 4h | Todas as páginas AI |
| FE-B09 | Adicionar empty states com mensagens claras | P2 | 2h | Várias páginas |
| FE-B10 | Adicionar indicadores visuais de enforcement de budget | P2 | 2h | `TokenBudgetPage.tsx` |
| FE-B11 | Melhorar UX de execução de agent (loading, resultado) | P2 | 4h | `AgentDetailPage.tsx` |
| FE-B12 | Validar integração real com API em AiAnalysisPage | P2 | 4h | `AiAnalysisPage.tsx` |
| FE-B13 | Revisar error handling em todas as chamadas API | P3 | 4h | `aiGovernance.ts` |
| FE-B14 | Adicionar testes de componente para páginas críticas | P3 | 8h | Novo test files |

**Total estimado:** ~47h (~6 dias de trabalho)

---

## 7. Proposta de menu reduzido

### Menu atual (9 items)
1. AI Assistant, AI Agents, Model Registry, AI Policies, AI Routing, IDE Integrations, Token Budgets, AI Audit, AI Analysis

### Menu proposto (6 items)
1. **AI Assistant** — chat + conversas (mantém)
2. **AI Agents** — agents + execuções (mantém)
3. **Models & Providers** — merge Model Registry + providers (merge)
4. **Policies & Budgets** — merge Policies + Budgets (merge)
5. **AI Audit** — audit log (mantém)
6. **AI Analysis** — análise assistida (mantém, se backend funcional)

### Removidos/ocultos
- ❌ **IDE Integrations** — ocultar até existirem extensões reais
- ❌ **AI Routing** — incorporar como sub-tab de Models & Providers
