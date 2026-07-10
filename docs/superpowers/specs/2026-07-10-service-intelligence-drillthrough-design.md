# P3 — Drill-through da inteligência de serviço (design)

**Data:** 2026-07-10
**Autor:** Claude Opus 4.8 (autonomia total concedida pelo owner)
**Persona:** owner / engenheiro de plataforma que parte de um dashboard agregado, quer chegar a um serviço específico e agir sobre ele.

## Problema

Os dashboards de inteligência de serviço do catálogo mostram números mas são
becos-sem-saída: não deixam saltar para um serviço específico, nem oferecem
ponte de ida a partir do detalhe do serviço. Três deles (Scorecard, Dependency,
License) obrigam o utilizador a **escrever o nome do serviço às cegas** num
campo antes de mostrar qualquer coisa — não há lista de onde escolher nem
ligação a partir do serviço que já se está a ver.

## Mapa de identidade (fonte da verdade para o que é honesto ligar)

| Ecrã | Identidade por-serviço disponível | Drill honesto para `/services/:id`? |
|------|-----------------------------------|-------------------------------------|
| `ServiceScorecardPage` | `ServiceScorecardResponse.serviceName` apenas (sem `serviceId`) | ❌ resultado não pode; ✅ pode ser **pré-carregado** por `?serviceName=` |
| `ServiceMaturityPage` (tab *maturity*) | `ServiceMaturityItemDto.serviceId` | ✅ |
| `ServiceMaturityPage` (tab *audit*) | `AuditFindingDto.serviceId` | ✅ |
| `ServiceDiscoveryPage` | `DiscoveredServiceItem.matchedServiceAssetId: string \| null` | ✅ quando não-nulo |
| `ServiceDetailPage` | `useParams().serviceId` **e** `service.name` / `service.displayName` | é o hub — já liga a `/changes?serviceName=` |

**Fora de escopo (honesto, não fabricado):**
- `DeveloperExperienceScorePage` — métrica ao nível de equipa (`teamId`/`teamName`);
  `serviceId` é opcional no formulário e nem aparece na lista. Sem alvo por-serviço.
- `SecurityGateDashboardPage` — não expõe `serviceId` em lado nenhum.
- `DependencyDashboardPage` / `LicenseCompliancePage` — scan por input manual;
  o `serviceId` presente nas linhas de resultado refere-se a nós do grafo de
  dependências / componentes, que **não** são garantidamente serviços do
  catálogo. Ligar seria fabricar uma associação que pode não existir.
- `ContractPipelinePage` — é um gerador de scaffolding, não um dashboard agregado.

## Arquitetura

O **detalhe do serviço (`/services/:id`) é o hub** do loop bidireccional:
- **Ida (detalhe → dashboard):** a partir de um serviço, saltar para o seu
  scorecard já pré-carregado.
- **Volta (dashboard → detalhe):** a partir das linhas dos dashboards agregados
  (maturity, auditoria, discovery), saltar para o serviço específico.

O padrão de pré-carregamento por query-param replica o já validado na P2 F2
(`ContractHealthTimelinePage` com `?apiAssetId=`): ler o parâmetro no primeiro
render, semear o estado local e disparar a query automaticamente, mantendo o
input manual para uso ad-hoc.

## Fatias

### F1 — Scorecard como acção sobre o serviço

**Objetivo:** matar o anti-padrão "escreve o nome às cegas" e ligar o detalhe do
serviço ao seu scorecard.

- `ServiceScorecardPage`:
  - Ler `?serviceName=` via `useSearchParams` no primeiro render; se presente,
    semear `searchInput` + `serviceName` e deixar a `useQuery` existente
    (`enabled: serviceName.length > 0`) correr automaticamente.
  - Manter o input manual + botão Compute intactos para consulta ad-hoc.
- `ServiceDetailPage`:
  - Adicionar um link **"Ver scorecard"** (chave i18n) para
    `/services/scorecards?serviceName=${encodeURIComponent(service.name)}`,
    junto do link existente para `/changes?serviceName=`.
- **Honest-null:** `ServiceScorecardResponse` não tem `serviceId`; **não** se
  cria back-link do resultado do scorecard para `/services/:id`.

**Critério de verificação:** teste que renderiza `ServiceScorecardPage` com
`?serviceName=orders-api` e verifica que a query é chamada com `orders-api` sem
o utilizador escrever nada; teste que o `ServiceDetailPage` renderiza o link com
o `href` correcto.

### F2 — Maturity & auditoria → detalhe do serviço

**Objetivo:** tornar as linhas dos dashboards de maturity e auditoria um ponto
de partida para o serviço específico.

- `ServiceMaturityPage` (tab *maturity*): em cada linha de serviço (que já é um
  `Button` de expand/collapse), adicionar um afford separado **"Abrir serviço"**
  (`Link` para `/services/${svc.serviceId}`) que **não** dispara o
  expand/collapse (parar propagação / elemento irmão do botão de toggle).
- `ServiceMaturityPage` (tab *audit*): cada `finding` (com `serviceId`) ganha um
  `Link` **"Abrir serviço"** para `/services/${f.serviceId}`.

**Critério de verificação:** teste que renderiza a tab maturity com um serviço e
verifica o `href` do link; idem para um finding na tab audit; teste que clicar no
link de abrir-serviço não alterna o expand.

### F3 — Discovery → catálogo + endurecimento do modal

**Objetivo:** ligar serviços descobertos já associados ao seu registo no catálogo
e alinhar o modal de triagem com o Design System.

- `ServiceDiscoveryPage`:
  - Para itens com `matchedServiceAssetId` não-nulo (estados Matched/Registered),
    adicionar link **"Ver no catálogo"** (`Link` para
    `/services/${svc.matchedServiceAssetId}`) na célula de acções, ao lado do
    botão View existente.
  - Migrar o `ActionModal` cru (`<div className="fixed inset-0 ... bg-black/50">`)
    para o DS `Modal` (título/onClose/footer), preservando os três fluxos
    (match/register/ignore) e o comportamento de submissão.

**Critério de verificação:** teste que uma linha Matched com
`matchedServiceAssetId` renderiza o link com o `href` correcto e que uma linha
Pending (sem match) não o renderiza; teste que abrir a acção mostra o conteúdo do
modal e que Cancel/confirmar disparam os callbacks certos.

## i18n

Novas chaves adicionadas aos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`) via script
de deep-merge; `npm run validate:i18n` tem de passar. Chaves previstas:
- `serviceDetail.viewScorecard`
- `serviceMaturity.openService`
- `catalog.discovery.actions.viewInCatalog`

## Testes

Vitest + Testing Library, centralizados em `src/frontend/src/__tests__/**`.
Cada fatia acrescenta testes forward-only que falham antes e passam depois.
Revisão final opus de todo o branch antes do merge direto em `main` (sem PR).

## Não-objetivos

- Não redesenhar o conteúdo dos dashboards nem os seus cálculos.
- Não adicionar lista/picker de serviços a Scorecard/Dependency/License
  (mudança maior; o pré-carregamento por serviço resolve a jornada da persona).
- Não tocar em DX, SecurityGate, Dependency, License, Pipeline (fora de escopo
  honesto acima).
