# Contract Health Experience — Design (P2, fatia 2)

**Data:** 2026-07-09
**Módulo:** catalog / contracts (frontend)
**Persona:** owner de contrato (quer saber o que está mau e corrigir)
**Ciclo:** P2 fatia 2 — drill-through / experiência de saúde de contratos

---

## 1. Problema

As duas telas da jornada de saúde estão modernizadas em estilo mas com a
experiência partida:

- **`ContractHealthTimelinePage`** força o utilizador a **digitar manualmente um
  `apiAssetId`** (UUID que ninguém decora) num campo de texto antes de ver algo. E,
  apesar do título "evolution over time", renderiza uma **tabela** de versões — não
  há qualquer tendência visual.
- **`ContractHealthDashboardPage`** fixa os parâmetros da query
  (`page:1, pageSize:50`). O endpoint `getHealthDashboard` **aceita `domain` e
  `contractType`** mas a UI nunca os expõe — não dá para focar o board num domínio
  ou tipo.
- **Restrição de identidade (dado real):** as `topViolations` da dashboard só
  carregam `contractVersionId`; a timeline é indexada por `apiAssetId`
  (`getContractHealthTimeline(apiAssetId)`). Não existe mapeamento fiável
  violação→apiAssetId → **não há drill honesto por-violação dashboard→timeline**. O
  único drill honesto para a timeline vem de superfícies que já têm o `apiAssetId` —
  em particular o **workspace do contrato** (`detail.apiAssetId`).

## 2. Objetivo (fatia 2)

Tornar a experiência de saúde **utilizável e visual**, e ligar o **drill honesto do
produtor** (workspace → timeline do seu contrato). Não fabricar drills que o modelo
de dados não suporta.

Critério de sucesso: (a) chegar à timeline **com o contrato já carregado** (sem
digitar UUID); (b) ver a **tendência** do health score, não só uma tabela; (c) focar
a dashboard por `domain`/`contractType`; (d) a partir do workspace de um contrato,
saltar para a timeline desse contrato num clique.

## 3. Não-objetivos (deferidos)

- Drill por-violação dashboard→timeline (bloqueado pelo modelo de identidade — não
  fabricar).
- Jornada do portal do consumidor + playground → fatia 3.
- Loop de autoria/enforcement (Spectral/canónicas/publicação) → fatia 4.
- Biblioteca de gráficos: o sparkline é SVG puro inline (sem ECharts/Recharts) —
  YAGNI, leve e testável.

## 4. Desenho

### 4.1 Timeline — pré-carregamento por query param

`ContractHealthTimelinePage` passa a ler `apiAssetId` de `useSearchParams()`:
- Semeia `apiAssetId` (input) **e** `submittedId` com o valor do param; como a query
  tem `enabled: !!submittedId`, carrega automaticamente ao chegar com o param.
- A entrada manual atual mantém-se como fallback (quem chega sem param continua a
  poder pesquisar/colar um id). Nada da lógica de `handleAnalyze` muda.

### 4.2 Timeline — tendência visual (`HealthTrendSparkline`)

Novo componente **puro** `HealthTrendSparkline` renderiza uma polyline SVG do
`healthScore` ao longo dos pontos (ordem cronológica já fornecida pelo backend),
montado **acima** da tabela de versões existente.

- Assinatura: `HealthTrendSparkline({ points }: { points: { semVer: string; healthScore: number }[] })`.
- Honest-null: com **menos de 2 pontos** não há tendência → devolve `null` (a tabela
  já cobre 0/1 ponto).
- Sem dependências de gráficos: `<svg viewBox>` + `<polyline>` normalizada ao
  min/max do score; cor accent; marcadores de ponto simples. Área ~`h-16`.
- O componente é apresentacional e determinístico (mesmo input → mesmo path).

### 4.3 Health dashboard — filtros `domain` + `contractType`

Expor os filtros que o endpoint já aceita, com estado local na página:
- `domain` — `TextField` de pesquisa (texto livre; `domain` é campo livre no
  backend, não há fonte de facetas → texto é honesto).
- `contractType` — `Select` construído de `CONTRACT_TYPES`
  (`{ value, labelKey }[]` de `../shared/constants`), com uma opção vazia "todos".
- Ambos entram nos params de `contractsApi.getHealthDashboard({ domain, contractType, page:1, pageSize:50 })`
  e na `queryKey` (`['contract-health-dashboard', domain, contractType]`) para
  refetch. Campo vazio → param omitido (não enviar string vazia).
- Barra de filtros numa faixa DS acima do bloco de score/KPIs.

### 4.4 Drill do produtor — workspace → timeline

`ContractWorkspacePage` já tem `detail.apiAssetId` e um slot `actions` no
`PageHeader` (atualmente só `ContractLifecycleActions`). Envolver o slot para
adicionar um link "Health timeline":

```tsx
actions={
  <div className="flex items-center gap-3">
    <Link
      to={`/contracts/health/timeline?apiAssetId=${detail.apiAssetId}`}
      className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
    >
      <TrendingUp size={14} />
      {t('contracts.workspace.healthTimeline', 'Health timeline')}
    </Link>
    <ContractLifecycleActions ... />
  </div>
}
```

Honest-null: só renderizar o link quando `detail.apiAssetId` existe.

## 5. Componentes / ficheiros

- **Novo:** `features/contracts/governance/HealthTrendSparkline.tsx` — puro, SVG.
- **Modificar:** `features/contracts/governance/ContractHealthTimelinePage.tsx` —
  query param + montar o sparkline.
- **Modificar:** `features/contracts/governance/ContractHealthDashboardPage.tsx` —
  filtros `domain`/`contractType`.
- **Modificar:** `features/contracts/workspace/ContractWorkspacePage.tsx` — link
  "Health timeline" no header.
- **Modificar:** `locales/{en,es,pt-BR,pt-PT}.json` — chaves novas (§7).

## 6. Fluxo de dados

- Timeline: `getContractHealthTimeline(apiAssetId)` — sem alteração de contrato de
  API; o param só semeia o estado.
- Sparkline: consome os `points` já carregados; zero fetch.
- Dashboard: `getHealthDashboard` passa a receber `domain`/`contractType` (já
  suportados na assinatura). Campos vazios omitidos.
- Workspace: usa `detail.apiAssetId` já carregado. Zero fetch novo.

## 7. i18n (4 locales: en, es, pt-BR, pt-PT)

Chaves novas (com fallback inglês via `t('key','fallback')`):

- `contracts.healthTimeline.trend` (rótulo/aria da tendência)
- `contracts.healthDashboard.filterDomain` (placeholder do filtro de domínio)
- `contracts.healthDashboard.filterType` (label do Select de tipo)
- `contracts.healthDashboard.allTypes` (opção "todos" do Select)
- `contracts.workspace.healthTimeline` (link no header do workspace)

`validate:i18n` tem de passar (4 locales completos e em paridade).

## 8. Testes

- **`HealthTrendSparkline`** (novo): com ≥2 pontos renderiza uma `<polyline>`; com
  <2 pontos devolve `null` (nada no DOM).
- **`ContractHealthTimelinePage`**: com `?apiAssetId=asset-1` no router, o input
  reflete `asset-1` e a query dispara (mock de `getContractHealthTimeline` chamado
  com `asset-1`).
- **`ContractHealthDashboardPage`**: alterar o filtro de tipo re-chama
  `getHealthDashboard` com `contractType` no argumento; escrever no filtro de
  domínio idem.
- **`ContractWorkspacePage`**: o header expõe um `link` "Health timeline" com `href`
  `/contracts/health/timeline?apiAssetId=<id>` (usar mock mínimo de `detail`).
- **e2e (`contract-health-experience.spec.ts`):** ir a
  `/contracts/health/timeline?apiAssetId=asset-1` (rota mockada) → a timeline mostra
  os pontos sem digitação manual.

Gates: `npm run test` (suite completa verde), `validate:i18n` PASS, `npm run build`
exit 0, `eslint` 0 erros nos ficheiros alterados, e2e verde.

## 9. Constraints globais

- DS de `../../../shared/ui` (`TextField`, `Select`, `Button`); componentes de
  `components/*`; ícones `lucide-react`; `Link`/`useSearchParams` de
  `react-router-dom`.
- Honest-null: nunca fabricar; sparkline oculto com <2 pontos; link do workspace só
  com `apiAssetId`; params vazios omitidos.
- i18n: nenhuma string de UI hardcoded; chaves nos 4 locales (NÃO há `fr`); ficheiros
  FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não refatorar a lógica de query nem o interior das tabelas; a
  entrada manual da timeline mantém-se.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em
  `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
- Tooling: `npm run test` (não `npx vitest`); gate final `npm run build` (`tsc -b`).
