# Relatório de Menu e Navegação do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Auditoria completa dos 45+ itens de menu lateral  
> **Status global:** GAP_IDENTIFIED  
> **Ficheiro do sidebar:** `src/frontend/src/components/shell/AppSidebar.tsx`

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Secções do sidebar | 12 |
| Itens de menu total | 49 |
| Itens com rota funcional | 46 |
| Itens com rota quebrada | 3 |
| Itens sem tela correspondente | 0 (exceto os 3 quebrados) |
| Telas sem item de menu | 62+ (sub-páginas + admin + notificações) |
| Permissões distintas usadas no menu | 18 |

---

## 2. Inventário Completo do Sidebar

### 2.1 Secção: home (1 item)

| # | Chave i18n | Rota | Permissão | Ícone | Status |
|---|-----------|------|-----------|-------|--------|
| 1 | `sidebar.dashboard` | `/` | *(nenhuma)* | Home | ✅ FUNCIONAL |

### 2.2 Secção: services (2 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 2 | `sidebar.serviceCatalog` | `/services` | `catalog:assets:read` | ✅ FUNCIONAL |
| 3 | `sidebar.dependencyGraph` | `/services/graph` | `catalog:assets:read` | ✅ FUNCIONAL |

### 2.3 Secção: knowledge (2 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 4 | `sidebar.sourceOfTruth` | `/source-of-truth` | `catalog:assets:read` | ✅ FUNCIONAL |
| 5 | `sidebar.developerPortal` | `/portal` | `developer-portal:read` | ✅ FUNCIONAL |

### 2.4 Secção: contracts (6 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 6 | `sidebar.contractCatalog` | `/contracts` | `contracts:read` | ✅ FUNCIONAL |
| 7 | `sidebar.createContract` | `/contracts/new` | `contracts:write` | ✅ FUNCIONAL |
| 8 | `sidebar.contractStudio` | `/contracts/studio` | `contracts:read` | ✅ FUNCIONAL (redirect) |
| 9 | `sidebar.contractGovernance` | `/contracts/governance` | `contracts:read` | ❌ QUEBRADA |
| 10 | `sidebar.spectralRulesets` | `/contracts/spectral` | `contracts:write` | ❌ QUEBRADA |
| 11 | `sidebar.canonicalEntities` | `/contracts/canonical` | `contracts:read` | ❌ QUEBRADA |

**Impacto:** Os itens 9, 10 e 11 aparecem no menu mas navegam para rotas não registadas em `App.tsx`. O utilizador vê uma página em branco ou comportamento inesperado.

### 2.5 Secção: changes (4 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 12 | `sidebar.changeConfidence` | `/changes` | `change-intelligence:read` | ✅ FUNCIONAL |
| 13 | `sidebar.changeIntelligence` | `/releases` | `change-intelligence:releases:read` | ✅ FUNCIONAL |
| 14 | `sidebar.workflow` | `/workflow` | `workflow:read` | ✅ FUNCIONAL |
| 15 | `sidebar.promotion` | `/promotion` | `promotion:read` | ✅ FUNCIONAL |

### 2.6 Secção: operations (5 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 16 | `sidebar.incidents` | `/operations/incidents` | `operations:incidents:read` | ✅ FUNCIONAL |
| 17 | `sidebar.runbooks` | `/operations/runbooks` | `operations:runbooks:read` | ✅ FUNCIONAL |
| 18 | `sidebar.reliability` | `/operations/reliability` | `operations:reliability:read` | ✅ FUNCIONAL |
| 19 | `sidebar.automation` | `/operations/automation` | `operations:automation:read` | ✅ FUNCIONAL |
| 20 | `sidebar.environmentComparison` | `/operations/runtime-comparison` | `operations:runtime:read` | ✅ FUNCIONAL |

### 2.7 Secção: aiHub (9 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 21 | `sidebar.aiAssistant` | `/ai/assistant` | `ai:assistant:read` | ✅ FUNCIONAL |
| 22 | `sidebar.aiAgents` | `/ai/agents` | `ai:assistant:read` | ✅ FUNCIONAL |
| 23 | `sidebar.modelRegistry` | `/ai/models` | `ai:governance:read` | ✅ FUNCIONAL |
| 24 | `sidebar.aiPolicies` | `/ai/policies` | `ai:governance:read` | ✅ FUNCIONAL |
| 25 | `sidebar.aiRouting` | `/ai/routing` | `ai:governance:read` | ✅ FUNCIONAL |
| 26 | `sidebar.aiIde` | `/ai/ide` | `ai:governance:read` | ✅ FUNCIONAL |
| 27 | `sidebar.aiBudgets` | `/ai/budgets` | `ai:governance:read` | ✅ FUNCIONAL |
| 28 | `sidebar.aiAudit` | `/ai/audit` | `ai:governance:read` | ✅ FUNCIONAL |
| 29 | `sidebar.aiAnalysis` | `/ai/analysis` | `ai:runtime:write` | ✅ FUNCIONAL |

### 2.8 Secção: governance (7 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 30 | `sidebar.executiveOverview` | `/governance/executive` | `governance:read` | ✅ FUNCIONAL |
| 31 | `sidebar.reports` | `/governance/reports` | `governance:read` | ✅ FUNCIONAL |
| 32 | `sidebar.compliance` | `/governance/compliance` | `governance:read` | ✅ FUNCIONAL |
| 33 | `sidebar.riskCenter` | `/governance/risk` | `governance:read` | ✅ FUNCIONAL |
| 34 | `sidebar.finops` | `/governance/finops` | `governance:read` | ✅ FUNCIONAL |
| 35 | `sidebar.policies` | `/governance/policies` | `governance:read` | ✅ FUNCIONAL |
| 36 | `sidebar.packs` | `/governance/packs` | `governance:read` | ✅ FUNCIONAL |

### 2.9 Secção: organization (2 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 37 | `sidebar.teams` | `/governance/teams` | `governance:read` | ✅ FUNCIONAL |
| 38 | `sidebar.domains` | `/governance/domains` | `governance:read` | ✅ FUNCIONAL |

### 2.10 Secção: integrations (1 item)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 39 | `sidebar.integrationHub` | `/integrations` | `integrations:read` | ✅ FUNCIONAL |

### 2.11 Secção: analytics (1 item)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 40 | `sidebar.productAnalytics` | `/analytics` | `analytics:read` | ✅ FUNCIONAL |

### 2.12 Secção: admin (9 itens)

| # | Chave i18n | Rota | Permissão | Status |
|---|-----------|------|-----------|--------|
| 41 | `sidebar.users` | `/users` | `identity:users:read` | ✅ FUNCIONAL |
| 42 | `sidebar.breakGlass` | `/break-glass` | `identity:sessions:read` | ✅ FUNCIONAL |
| 43 | `sidebar.jitAccess` | `/jit-access` | `identity:users:read` | ✅ FUNCIONAL |
| 44 | `sidebar.delegations` | `/delegations` | `identity:users:read` | ✅ FUNCIONAL |
| 45 | `sidebar.accessReview` | `/access-reviews` | `identity:users:read` | ✅ FUNCIONAL |
| 46 | `sidebar.mySessions` | `/my-sessions` | `identity:sessions:read` | ✅ FUNCIONAL |
| 47 | `sidebar.audit` | `/audit` | `audit:read` | ✅ FUNCIONAL |
| 48 | `sidebar.platformOperations` | `/platform/operations` | `platform:admin:read` | ✅ FUNCIONAL |
| 49 | `sidebar.platformConfiguration` | `/platform/configuration` | `platform:admin:read` | ✅ FUNCIONAL |

---

## 3. Análise de Agrupamento

### 3.1 Distribuição de itens por secção

| Secção | Itens | % do total | Observação |
|--------|-------|-----------|------------|
| aiHub | 9 | 18.4% | Secção mais rica — adequado ao pilar de IA |
| admin | 9 | 18.4% | Adequado — gestão de plataforma |
| governance | 7 | 14.3% | Alinhado com pilar de governance |
| contracts | 6 | 12.2% | **3 dos 6 itens quebrados** |
| operations | 5 | 10.2% | Adequado |
| changes | 4 | 8.2% | Adequado |
| services | 2 | 4.1% | Adequado |
| knowledge | 2 | 4.1% | Adequado |
| organization | 2 | 4.1% | Sub-secção de governance |
| home | 1 | 2.0% | Adequado |
| integrations | 1 | 2.0% | Adequado |
| analytics | 1 | 2.0% | Adequado |

### 3.2 Análise de agrupamento por persona

O sidebar responde à persona ativa via `PersonaContext`. A ordem das secções e a visibilidade dos itens adaptam-se conforme o mapeamento:

| Persona | Secções prioritárias | Observação |
|---------|---------------------|------------|
| Engineer | services, contracts, changes, operations | Foco técnico |
| TechLead | services, contracts, changes, governance | Foco em liderança técnica |
| Architect | contracts, governance, operations, ai-hub | Foco em arquitetura e governance |
| Product | analytics, governance, knowledge | Foco em produto |
| Executive | governance (executive), analytics | Foco executivo |
| PlatformAdmin | admin, operations, integrations, configuration | Foco em plataforma |
| Auditor | audit, governance, compliance | Foco em conformidade |

---

## 4. Itens com Problemas

### 4.1 Itens de menu sem tela funcional (3 — CRITICAL)

| Item | Secção | Rota | Problema | Prioridade |
|------|--------|------|----------|------------|
| Contract Governance | contracts | `/contracts/governance` | Rota não registada em App.tsx | CRITICAL |
| Spectral Rulesets | contracts | `/contracts/spectral` | Rota não registada em App.tsx | CRITICAL |
| Canonical Entities | contracts | `/contracts/canonical` | Rota não registada em App.tsx | CRITICAL |

### 4.2 Telas sem item de menu (categorias)

| Categoria | Páginas | Justificação |
|-----------|---------|--------------|
| Sub-páginas de detalhe | ~30 | Acesso via navegação interna (detalhe de serviço, incidente, etc.) |
| Páginas de notificação | 3 | Acesso via topbar (ícone de sino) |
| Páginas de configuração | ~8 | Acesso via secções de admin ou config |
| Páginas públicas (auth) | 7 | Fora do AppShell — sem menu |
| Páginas legacy/órfãs | 6 | Sem referência ativa |

### 4.3 Observações de agrupamento

| Observação | Detalhe | Prioridade |
|-----------|---------|------------|
| Organization dentro de governance | `sidebar.teams` e `sidebar.domains` navegam para `/governance/teams` e `/governance/domains` mas estão na secção `organization` | LOW — intencional |
| Admin muito extenso | 9 itens na secção admin — considerar sub-agrupamento | LOW |
| Notificações ausentes do sidebar | Acesso apenas via topbar icon — padrão válido mas não documentado | LOW |

---

## 5. Permissões Usadas no Menu

| Permissão | Itens que a usam | Contagem |
|-----------|-----------------|----------|
| `governance:read` | executiveOverview, reports, compliance, riskCenter, finops, policies, packs, teams, domains | 9 |
| `ai:governance:read` | modelRegistry, aiPolicies, aiRouting, aiIde, aiBudgets, aiAudit | 6 |
| `identity:users:read` | users, jitAccess, delegations, accessReview | 4 |
| `catalog:assets:read` | serviceCatalog, dependencyGraph, sourceOfTruth | 3 |
| `contracts:read` | contractCatalog, contractStudio, contractGovernance, canonicalEntities | 4 |
| `contracts:write` | createContract, spectralRulesets | 2 |
| `ai:assistant:read` | aiAssistant, aiAgents | 2 |
| `identity:sessions:read` | breakGlass, mySessions | 2 |
| `platform:admin:read` | platformOperations, platformConfiguration | 2 |
| `operations:incidents:read` | incidents | 1 |
| `operations:runbooks:read` | runbooks | 1 |
| `operations:reliability:read` | reliability | 1 |
| `operations:automation:read` | automation | 1 |
| `operations:runtime:read` | environmentComparison | 1 |
| `change-intelligence:read` | changeConfidence | 1 |
| `change-intelligence:releases:read` | changeIntelligence | 1 |
| `workflow:read` | workflow | 1 |
| `promotion:read` | promotion | 1 |
| `developer-portal:read` | developerPortal | 1 |
| `integrations:read` | integrationHub | 1 |
| `analytics:read` | productAnalytics | 1 |
| `audit:read` | audit | 1 |
| `ai:runtime:write` | aiAnalysis | 1 |
| *(nenhuma)* | dashboard | 1 |

---

## 6. Recomendações

| # | Ação | Prioridade | Esforço |
|---|------|------------|---------|
| 1 | Registar rotas em falta para os 3 itens quebrados de contracts | CRITICAL | Baixo |
| 2 | Documentar padrão de acesso a notificações via topbar | LOW | Baixo |
| 3 | Considerar sub-agrupamento da secção admin (9 itens) | LOW | Médio |
| 4 | Verificar se ordem do sidebar respeita configuração por persona | MEDIUM | Baixo |
| 5 | Garantir que todas as chaves i18n `sidebar.*` existem nos 4 locales | HIGH | Baixo |

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
