# Feature Flags no detalhe do serviço (flagship "menu → consulta") — Design

**Data:** 2026-07-16
**Contexto:** Ciclo 39. Primeiro passo de uma iniciativa: mover itens per-entidade do menu de Catalog para dentro da consulta (detalhe do serviço) ou do cadastro. Sucede o ciclo 38 (criação de contrato ancorada + consolidação APIs/Interfaces).

---

## Motivação

O utilizador observou que muitos itens do menu de Catalog são, na verdade, **funções per-entidade** que foram promovidas a destino de topo. Feature Flags é o caso mais limpo: a página `/services/feature-flags` mostra um dashboard agregado de flags de *todos* os serviços, mas cada flag é escopada a um serviço (`serviceId`). O sítio natural para ver e gerir as flags de um serviço é o **detalhe desse serviço**.

**Decisão (AskUserQuestion):** trazer a funcionalidade para o contexto **E remover o item de menu** — desta vez na ordem correta (primeiro in-place, depois remover), com autorização explícita (ao contrário do ciclo 36, que restruturou o menu sem trazer nada in-place e foi revertido).

**Padrão a estabelecer:** este flagship define o molde reutilizável para os restantes itens (Discovery→cadastro, Maturidade→filtro, Pipeline/CDCT/Publicação→workspace do contrato, Notas→Knowledge) em ciclos seguintes.

---

## Fonte de dados (verificado)

`features/catalog/pages/ServiceFeatureFlagsPage.tsx`:
- `GET /catalog/feature-flags` → `ServiceFeatureFlagDashboard` `{ totalFlags, enabledFlags, disabledFlags, affectedServices, flags: ServiceFeatureFlag[] }`.
- `ServiceFeatureFlag` `{ id, serviceId, serviceName, flagKey, displayName, description?, enabled, environment, updatedAt, updatedBy? }`.
- `PATCH /catalog/feature-flags/{flagId}` `{ enabled }` — toggle.
- Query env-scoped: key `['service-feature-flags', activeEnvironmentId]` (`useEnvironment().activeEnvironmentId`).
- Não há endpoint per-serviço; a página filtra client-side. Como cada flag tem `serviceId`, o corte per-serviço é `flags.filter(f => f.serviceId === serviceId)`.

---

## Arquitetura

### 1. Extrair api+tipos para módulo partilhado (DRY)

Novo `features/catalog/api/featureFlags.ts` com `ServiceFeatureFlag`, `ServiceFeatureFlagDashboard` e `serviceFeatureFlagsApi` (`getDashboard`, `toggle`) — movidos verbatim de `ServiceFeatureFlagsPage.tsx`. A página passa a importar deste módulo (comportamento inalterado). Evita duplicar strings de endpoint entre a página e a nova tab.

### 2. `ServiceFeatureFlagsTab` (a consulta in-place)

Novo `features/catalog/components/ServiceFeatureFlagsTab.tsx`:
- Assinatura: `export function ServiceFeatureFlagsTab({ serviceId }: { serviceId: string })`.
- Usa `useEnvironment().activeEnvironmentId`, `useQuery(['service-feature-flags', activeEnvironmentId], serviceFeatureFlagsApi.getDashboard)` — **mesma query key** da página → TanStack deduplica.
- `const flags = (data?.flags ?? []).filter(f => f.serviceId === serviceId)`.
- Renderiza uma tabela per-serviço: colunas **Flag** (displayName + flagKey + description), **Ambiente**, **Atualizado** (data + updatedBy), **Status** (`Toggle` que chama a mutation `toggle`). **Sem** a coluna "Serviço" (redundante no contexto).
- Mutation de toggle idêntica à da página (`invalidateQueries(['service-feature-flags'])`).
- Estados: loading (spinner), erro (bloco crítico), vazio (`featureFlags.serviceEmpty` — "Este serviço não tem feature flags registadas.").
- **Deep-link ao agregado:** no cabeçalho da secção, um link "Ver todas as flags do portefólio →" para `/services/feature-flags` (`featureFlags.viewPortfolio`). Mantém o dashboard transversal reachable a partir do contexto (padrão ciclos 32-34).
- Envolvido num `Card`/secção coerente com as outras tabs do detalhe.

### 3. Wiring no `ServiceDetailPage`

- Adicionar `'featureFlags'` ao tipo `ServiceTab`.
- Adicionar item ao `viewTabItems` **a seguir a `score`**: `{ id: 'featureFlags', label: t('serviceDetail.tabFeatureFlags', 'Feature Flags'), icon: <Sliders size={14} /> }` (import `Sliders` de lucide-react).
- Render: `{activeViewTab === 'featureFlags' && <ServiceFeatureFlagsTab serviceId={serviceId} />}`.

### 4. Remover do menu

- Remover a linha `sidebar.featureFlags` (`/services/feature-flags`) de `navItems` em `AppSidebar.tsx` (linha ~63). **Não** limpar o import `Sliders` — continua usado por `releaseControlParameters` (linha ~94).
- **Manter** a rota `/services/feature-flags` e a página `ServiceFeatureFlagsPage` (agregado de portefólio, agora reachable via o deep-link da tab). Só sai do menu.

---

## i18n (4 locales: en, es, pt-BR, pt-PT)

Reusar as chaves `featureFlags.*` existentes. Novas chaves:
- `serviceDetail.tabFeatureFlags` — EN "Feature Flags" · ES "Feature Flags" · pt "Feature Flags"
- `featureFlags.viewPortfolio` — EN "View all portfolio flags" · ES "Ver todas las flags del portafolio" · pt-BR "Ver todas as flags do portfólio" · pt-PT "Ver todas as flags do portefólio"
- `featureFlags.serviceEmpty` — EN "This service has no registered feature flags." · ES "Este servicio no tiene feature flags registradas." · pt-BR "Este serviço não tem feature flags registradas." · pt-PT "Este serviço não tem feature flags registadas."

---

## Testes (Vitest, só em `src/__tests__/**`)

- **`ServiceFeatureFlagsTab`:** renderiza só as flags do `serviceId` dado (mock do dashboard com flags de 2 serviços → só as de 1 aparecem); estado vazio quando o serviço não tem flags; o toggle chama a mutation `toggle` com `{flagId, enabled}`; o deep-link ao portefólio existe (`href="/services/feature-flags"`).
- **`ServiceDetailPage`:** a tab "Feature Flags" existe no tab bar.
- **Sidebar:** o item `/services/feature-flags` já **não** aparece nos navItems (teste de `AppSidebar` ou asserção sobre `navItems`, conforme padrão existente).
- Gates: `tsc`, `eslint`, `build`, suíte completa. Verificação no stub.

---

## Fora de âmbito

- Rollout dos restantes itens (Discovery, Maturidade, Pipeline/CDCT/Publicação, Notas) — ciclos seguintes, mesmo padrão.
- Apagar/redesenhar o dashboard de portefólio `ServiceFeatureFlagsPage` (mantém-se como agregado deep-linkado).
- Criar/registar novas flags a partir do detalhe (cadastro de flags) — a tab é consulta + toggle; a criação de flags não está no âmbito deste flagship.
