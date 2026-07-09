# Contract Quality & Governance Hub — Design (P2, fatia 1)

**Data:** 2026-07-09
**Módulo:** catalog / contracts (frontend)
**Persona:** owner de contrato (produtor) + integrador/consumidor
**Ciclo:** P2 fatia 1 da jornada de Qualidade & Governança de contratos

---

## 1. Problema

O módulo tem 11 telas de qualidade/governança de contratos, todas modernizadas em
estilo (ciclos 14-15) mas **journey-dead**: existem sem se ligarem numa jornada.

Estado atual (auditoria 2026-07-09):

- **6 de 11 telas estão totalmente órfãs** — alcançáveis apenas escrevendo o URL,
  zero links de entrada e zero entradas de navegação:
  `ContractGovernancePage` (`/contracts/governance`), `ContractPlaygroundPage`
  (`/contracts/playground`), `ContractMigrationPage` (`/contracts/migration`),
  `ContractHealthTimelinePage` (`/contracts/health/timeline`),
  `CanonicalEntityImpactCascadePage` (`/contracts/canonical/impact-cascade`),
  `ContractPortalPage` (`/contracts/portal/:contractVersionId`).
- **5 telas no sidebar** como lista plana sob o sub-grupo
  `sidebar.subGroupContractGovernance`: health (`/contracts/health`), cdct
  (`/contracts/cdct`), spectral (`/contracts/spectral`), canonical
  (`/contracts/canonical`), publication (`/contracts/publication`). Sem hub, sem
  narrativa, sem cross-linking.
- Os deep-links naturais entre pai→filho não existem: a Health dashboard já tem
  `topViolations[].contractVersionId` mas as linhas não ligam a lado nenhum; o
  catálogo canónico não liga ao impact-cascade; o portal do consumidor não é
  alcançável de parte alguma.
- O melhor candidato a hub — `ContractGovernancePage`, que é um dashboard real com
  5 tabs (overview/approvals/compliance/gaps/audit) e já computa insights
  honest-null a partir da lista de contratos — é ele próprio órfão.

## 2. Objetivo (fatia 1)

Dar **espinha à jornada**: um hub de entrada que torna toda a jornada de
qualidade/governança alcançável e narrada, e wiring dos dead-ends pai→filho. Não
redesenhar o interior de cada ferramenta (fatias 2-4).

Análoga à P1 (reboot do hub self-service + wiring de links mortos + postura
honest-null).

Critério de sucesso: depois desta fatia, **nenhuma das 11 telas é órfã** e existe
um único ponto de entrada ("Contract Governance") que apresenta a postura e lança
as 11 ferramentas agrupadas por intenção.

## 3. Não-objetivos (deferidos)

- Redesenho interno de cada ferramenta (Health drill-through, viz do impact-cascade,
  profundidade do portal do consumidor, UX do playground) → fatias 2-4.
- Novos endpoints/backend. Usar apenas dados já disponíveis (honest-null onde faltam).
- Novas contagens/badges fabricadas nos cards de ferramenta.

## 4. Desenho

### 4.1 Hub — promover `ContractGovernancePage`

`ContractGovernancePage` (`/contracts/governance`) passa a ser o hub da jornada.

- **Manter** o dashboard existente com as 5 tabs (overview/approvals/compliance/gaps/
  audit) e o cálculo honest-null de `computeGovernanceInsights`/`computePolicyChecks`
  — é a postura agregada, não se toca na lógica.
- **Adicionar** uma secção nova **"Governance tools"** (abaixo das tabs, no fim da
  página, sempre visível independentemente da tab ativa) — uma grelha de cards
  agrupados por intenção. Cada card: ícone + título i18n + subtítulo curto i18n +
  `Link` para a rota. Sem contagens dinâmicas (ferramentas estáticas).

  Grupos e cards:

  | Grupo (i18n) | Cards → rota |
  |---|---|
  | **Assess** | Health dashboard → `/contracts/health` · Health timeline → `/contracts/health/timeline` |
  | **Enforce** | Spectral rulesets → `/contracts/spectral` · Consumer-driven contracts → `/contracts/cdct` |
  | **Model** | Canonical entities → `/contracts/canonical` · Impact cascade → `/contracts/canonical/impact-cascade` |
  | **Publish** | Publication center → `/contracts/publication` · Migration → `/contracts/migration` |
  | **Test** | Playground → `/contracts/playground` |

  A secção vive num componente próprio, `GovernanceToolsSection`, para manter a
  página-hub focada. Os cards reutilizam o padrão visual do `SelfServicePortalPage`
  (card `rounded-lg border border-edge bg-card p-4` + ícone em caixa accent).

### 4.2 Sidebar — entrada de landing do hub

Adicionar, como **1º item** do sub-grupo `sidebar.subGroupContractGovernance`
(antes de `contractPipeline`), uma entrada nova:

```
{ labelKey: 'sidebar.contractGovernanceHub', to: '/contracts/governance',
  icon: <ShieldCheck size={18} />, permission: 'contracts:read',
  section: 'catalog', subGroup: 'sidebar.subGroupContractGovernance' }
```

Mantêm-se as 5 entradas de ferramenta existentes (health/cdct/spectral/canonical/
publication) e a `contractPipeline`. `ShieldCheck` já é importado no ficheiro do
sidebar (confirmar; senão adicionar o import).

### 4.3 Wiring de dead-ends pai→filho

Ligar as sub-páginas contextuais a partir das suas páginas-mãe naturais, matando os
becos sem saída:

1. **Health dashboard (`ContractHealthDashboardPage`):**
   - Ação no `PageHeader` "View timeline" → `Link` para `/contracts/health/timeline`.
   - Cada linha de `topViolations` (que tem `contractVersionId` + `semVer`) passa a
     `Link` para `/contracts/portal/${contractVersionId}` (portal do contrato),
     preservando o layout atual da linha.

2. **Canonical catalog (`CanonicalEntityCatalogPage`):**
   - Por entidade, uma ação "Impact cascade" → `/contracts/canonical/impact-cascade?entityId=${entity.id}`.
     O ponto de inserção exato (linha da entidade expandida) fica definido no plano.
   - `CanonicalEntityImpactCascadePage` passa a ler `entityId` de
     `useSearchParams()` para pré-selecionar a entidade quando presente (fallback:
     comportamento atual, seletor manual).

## 5. Componentes / ficheiros

- **Novo:** `features/contracts/governance/GovernanceToolsSection.tsx` —
  apresentacional, honest-null (cards estáticos), agrupamento por intenção.
- **Modificar:** `features/contracts/governance/ContractGovernancePage.tsx` —
  renderizar `<GovernanceToolsSection />` no fim.
- **Modificar:** `components/shell/AppSidebar.tsx` — entrada de landing do hub.
- **Modificar:** `features/contracts/governance/ContractHealthDashboardPage.tsx` —
  ação "View timeline" + linhas de violação como `Link` para o portal.
- **Modificar:** `features/contracts/canonical/CanonicalEntityCatalogPage.tsx` —
  ação "Impact cascade" por entidade.
- **Modificar:** `features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx` —
  ler `entityId` de query param.
- **Modificar:** `locales/{en,es,pt-BR,pt-PT}.json` — chaves novas (ver §7).

## 6. Fluxo de dados

- O hub não introduz novas queries — reutiliza `contracts-summary` +
  `contracts-list-governance` já existentes na página. `GovernanceToolsSection` não
  faz fetch (cards estáticos).
- Wiring usa apenas dados já carregados (`topViolations[].contractVersionId`,
  `entity.id`). Zero fabricação: se um campo não existir, o link não é renderizado.

## 7. i18n (4 locales: en, es, pt-BR, pt-PT)

Novas chaves (com fallback em inglês no código via `t('key','fallback')`):

- `sidebar.contractGovernanceHub`
- `contracts.governance.tools.title` (título da secção)
- `contracts.governance.tools.groups.{assess,enforce,model,publish,test}`
- `contracts.governance.tools.items.{healthDashboard,healthTimeline,spectral,cdct,canonical,impactCascade,publication,migration,playground}.{title,subtitle}`
- `contracts.healthDashboard.viewTimeline`
- `contracts.canonical.catalog.impactCascade` (ação por entidade)

`validate:i18n` tem de passar (as 4 locales completas e em paridade).

## 8. Testes

- **`GovernanceToolsSection`** (novo): renderiza os grupos e um `link` por
  ferramenta com o `href` correto (ex. `/contracts/playground`,
  `/contracts/migration`); todos os 9 cards presentes.
- **`ContractGovernancePage`**: renderiza a secção de ferramentas (smoke — o hub
  monta a secção).
- **`ContractHealthDashboardPage`**: linha de violação renderiza como `link` para
  `/contracts/portal/:id`; ação "View timeline" presente com `href`
  `/contracts/health/timeline`.
- **`CanonicalEntityImpactCascadePage`**: com `?entityId=x` no router, pré-seleciona
  a entidade (o input reflete `x`).
- **e2e (`contract-governance-hub.spec.ts`):** navegar para `/contracts/governance`,
  ver a secção de ferramentas, clicar em "Playground" → URL `/contracts/playground`.

Gates: `npm run test` (suite completa verde), `validate:i18n` PASS, `npm run build`
exit 0, `eslint` 0 erros nos ficheiros alterados, e2e do hub verde.

## 9. Constraints globais

- DS de `../../../shared/ui` (`Button`) e componentes de `components/*`
  (`PageHeader`, `Card`); ícones `lucide-react`; `Link` de `react-router-dom`.
- Honest-null: nunca fabricar contagens/estado; ocultar link quando falta o id.
- i18n: nenhuma string de UI hardcoded; chaves nos 4 locales.
- Mudanças cirúrgicas: não refatorar a lógica de insights nem o interior das
  ferramentas; não tocar em código não relacionado.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em
  `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
