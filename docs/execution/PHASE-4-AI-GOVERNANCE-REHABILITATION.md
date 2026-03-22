# Phase 4 — AI Governance Rehabilitation

## Escopo da Fase

A Fase 4 do NexTraceOne tem como objetivo transformar o pilar de **AI Governance** de "implementado porém escondido" em **capacidade real de produto enterprise em escopo de produção**.

## Features Reabilitadas

| Feature | Rota | Backend | Frontend | Persistência | Testes |
|---------|------|---------|----------|-------------|--------|
| AI Model Registry | `/ai/models` | ✅ CRUD completo | ✅ ModelRegistryPage.tsx | ✅ ai_gov_models | ✅ 38 entity + 7 frontend |
| AI Policies | `/ai/policies` | ✅ CRUD completo | ✅ AiPoliciesPage.tsx | ✅ ai_gov_access_policies | ✅ Incluído nos 38 entity + 6 frontend |
| AI Routing | `/ai/routing` | ✅ Strategies + Decisions | ✅ AiRoutingPage.tsx | ✅ ai_gov_routing_strategies | ✅ 17 entity + 6 frontend |
| AI Token Budgets | `/ai/budgets` | ✅ CRUD completo | ✅ TokenBudgetPage.tsx | ✅ ai_gov_budgets | ✅ Incluído nos 38 entity + 7 frontend |
| AI Audit | `/ai/audit` | ✅ Query/Listagem | ✅ AiAuditPage.tsx | ✅ ai_gov_usage_entries | ✅ Incluído nos 38 entity + 8 frontend |
| IDE Integrations | `/ai/ide` | ✅ Clientes + Políticas | ✅ IdeIntegrationsPage.tsx | ✅ ai_gov_ide_* | ✅ 11 entity + 6 frontend |

## Exclusões Removidas

As 6 rotas de AI Governance foram removidas de `finalProductionExcludedRoutePrefixes` em `releaseScope.ts`:

```typescript
// REMOVIDAS da exclusão:
// '/ai/models'
// '/ai/policies'
// '/ai/routing'
// '/ai/ide'
// '/ai/budgets'
// '/ai/audit'
```

## Preview Removido

Nenhuma das 6 rotas de AI Governance possuía a flag `preview: true` no sidebar. As rotas já apareciam no menu de navegação para utilizadores com permissão `ai:governance:read`. A única barreira era a exclusão em `releaseScope.ts`, que foi removida.

## Impacto na Completude do Produto

Antes da Fase 4:
- 14 rotas excluídas de produção
- AI Governance inteiramente oculto

Depois da Fase 4:
- 8 rotas excluídas de produção (reduziu 43%)
- AI Governance totalmente visível e funcional
- Pilar de governança de IA operacional

## Permissões

Todas as features de AI Governance estão protegidas por permissões:
- `ai:governance:read` — leitura de modelos, políticas, routing, budgets, audit
- `ai:governance:write` — criação e modificação
- `ai:ide:read` — leitura de integrações IDE
- `ai:ide:write` — registo de clientes IDE
