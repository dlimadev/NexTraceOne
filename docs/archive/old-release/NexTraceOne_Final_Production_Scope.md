# NexTraceOne — Final Production Scope

## 1. Resumo executivo

O release produtivo final foi reduzido para refletir apenas a superfície com evidência suficiente de estabilidade, backend real e navegação coerente.

## 2. Princípio de escopo adotado

- entrar no release apenas o que está pronto ou aceitavelmente quase pronto
- retirar da navegação produtiva tudo o que permanece parcial, preview, mockado ou experimental
- manter rotas excluídas apenas sob controlo explícito, com mensagem clara de fora do release

## 3. Módulos incluídos no release

- Login / Auth / Tenant selection
- Shell / Navigation
- Dashboard
- Service Catalog
- Source of Truth
- Change Governance core
- Identity Admin core
- Audit

## 4. Módulos excluídos do release

- Developer Portal
- Contracts
- Operations
- AI Hub
- Governance
- Integrations
- Product Analytics
- Platform Operations

## 5. Rotas incluídas

- `/`
- `/login`
- `/forgot-password`
- `/reset-password`
- `/activate`
- `/mfa`
- `/invitation`
- `/select-tenant`
- `/search`
- `/source-of-truth`
- `/source-of-truth/services/:serviceId`
- `/source-of-truth/contracts/:contractVersionId`
- `/services`
- `/services/graph`
- `/services/:serviceId`
- `/graph`
- `/changes`
- `/changes/:changeId`
- `/releases`
- `/workflow`
- `/promotion`
- `/users`
- `/audit`
- `/break-glass`
- `/jit-access`
- `/delegations`
- `/access-reviews`
- `/my-sessions`
- `/unauthorized`

## 6. Rotas excluídas

- `/portal`
- `/contracts/*`
- `/operations/*`
- `/ai/*`
- `/governance/*`
- `/integrations/*`
- `/analytics/*`
- `/platform/operations`

## 7. Features experimentais mantidas fora do release

- reliability
- automation
- integrations
- analytics sub-areas
- governance executive/compliance/risk/finops/policies/evidence/controls
- developer portal
- contracts authoring/workspace advanced flows
- AI Hub surfaces ainda sem prova end-to-end suficiente

## 8. Ações aplicadas na navegação

- sidebar filtrada pelo escopo final
- command palette filtrada pelo escopo final
- quick actions e persona quickstart filtrados pelo escopo final
- dashboard deixou de expor links de atenção e widgets para áreas fora do release
- global search deixou de expor resultados para rotas fora do release
- rotas excluídas continuam existentes apenas com gate explícito de fora do release

## 9. Rationale por decisão importante

### Contracts
Excluído porque o documento de convergência final ainda marca o módulo como parcial, com fluxo create/edit/workspace inconsistente e mock residual.

### Operations
Excluído porque incidents ainda estava classificado como parcial e runbooks standalone permaneciam incompletos; reliability e automation já estavam preview.

### AI Hub
Excluído porque assistant e superfícies administrativas ainda não tinham prova end-to-end suficientemente forte para release honesto.

### Governance / Integrations / Analytics
Excluídos porque o código e os relatórios convergem para estado parcial ou preview, incompatível com navegação produtiva principal.

## 10. Conclusão final de escopo

O produto final ficou menor, mas semanticamente mais honesto. O release passa a expor apenas capacidades com melhor sustentação real, enquanto áreas fora do release permanecem controladas e claramente sinalizadas.
