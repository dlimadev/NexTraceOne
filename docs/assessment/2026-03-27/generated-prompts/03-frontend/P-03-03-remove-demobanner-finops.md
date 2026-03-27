# P-03-03 — Remover DemoBanner das páginas FinOps e conectar a dados reais do backend

## 1. Título

Remover DemoBanner das páginas de FinOps no módulo Governance e conectar a dados reais da API.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

As páginas de FinOps (FinOpsPage, ServiceFinOpsPage, TeamFinOpsPage, DomainFinOpsPage, ExecutiveFinOpsPage) apresentam DemoBanner porque o backend retorna dados simulados. Após P-01-03, o backend deve fornecer dados reais. Este prompt remove o DemoBanner e conecta as páginas às APIs reais.

## 4. Problema atual

- Testes confirmam DemoBanner ativo: `src/frontend/src/__tests__/pages/FinOpsPage.test.tsx`, `ServiceFinOpsPage.test.tsx`, `TeamFinOpsPage.test.tsx`, `ExecutiveFinOpsPage.test.tsx`.
- Páginas FinOps em `src/frontend/src/features/governance/pages/`:
  - `FinOpsPage.tsx`, `ServiceFinOpsPage.tsx`, `TeamFinOpsPage.tsx`, `DomainFinOpsPage.tsx`, `ExecutiveFinOpsPage.tsx`
- O ficheiro de API `src/frontend/src/features/governance/api/finOps.ts` pode ter funções com dados simulados.
- FinOps contextual é pilar do produto — páginas com DemoBanner permanente desvalorizam a proposta.

## 5. Escopo permitido

- `src/frontend/src/features/governance/pages/FinOpsPage.tsx`
- `src/frontend/src/features/governance/pages/ServiceFinOpsPage.tsx`
- `src/frontend/src/features/governance/pages/TeamFinOpsPage.tsx`
- `src/frontend/src/features/governance/pages/DomainFinOpsPage.tsx`
- `src/frontend/src/features/governance/pages/ExecutiveFinOpsPage.tsx`
- `src/frontend/src/features/governance/api/finOps.ts`
- Testes correspondentes em `src/frontend/src/__tests__/pages/`

## 6. Escopo proibido

- Não alterar páginas de Governance que não sejam FinOps.
- Não alterar o backend.
- Não remover o componente DemoBanner globalmente — apenas remover o uso nas páginas FinOps.
- Não alterar o design system.

## 7. Ficheiros principais candidatos a alteração

1. `src/frontend/src/features/governance/pages/FinOpsPage.tsx`
2. `src/frontend/src/features/governance/pages/ServiceFinOpsPage.tsx`
3. `src/frontend/src/features/governance/pages/TeamFinOpsPage.tsx`
4. `src/frontend/src/features/governance/pages/DomainFinOpsPage.tsx`
5. `src/frontend/src/features/governance/pages/ExecutiveFinOpsPage.tsx`
6. `src/frontend/src/features/governance/api/finOps.ts`

## 8. Responsabilidades permitidas

- Remover importação e uso de DemoBanner das 5 páginas FinOps.
- Implementar chamadas reais à API de FinOps usando TanStack Query.
- Adicionar estados de loading, erro e vazio com mensagens i18n.
- Atualizar testes para refletir ausência do DemoBanner e presença de dados reais (ou mocks de API).
- Manter contextualização por serviço, equipa e domínio nas páginas respetivas.

## 9. Responsabilidades proibidas

- Não implementar gráficos novos — usar ECharts existentes.
- Não alterar lógica de autenticação ou autorização.
- Não remover filtros de contexto (serviço, equipa, domínio) existentes.

## 10. Critérios de aceite

- [ ] Nenhuma página FinOps mostra DemoBanner.
- [ ] Todas as 5 páginas consomem API real de FinOps.
- [ ] Estados de loading, erro e vazio implementados.
- [ ] Build frontend sem erros.
- [ ] Testes atualizados e a passar.
- [ ] Textos usam chaves i18n.

## 11. Validações obrigatórias

- Build do frontend sem erros.
- Testes existentes atualizados e a passar.
- Lint sem erros críticos.
- Verificar que DemoBanner não é importado nas 5 páginas FinOps.

## 12. Riscos e cuidados

- Se P-01-03 não estiver concluído, a API pode ainda retornar dados simulados — as páginas devem funcionar com qualquer dado.
- ExecutiveFinOpsPage pode ter requisitos de persona (Executive) — verificar que o contexto está mantido.
- Testes que validam DemoBanner precisam ser reescritos para validar dados reais ou estados de loading.

## 13. Dependências

- **P-01-03** — Backend handlers de FinOps devem retornar dados reais.
- Endpoints REST de FinOps registados no módulo Governance.

## 14. Próximos prompts sugeridos

- **P-03-01** — Substituir mocks em Operations (mesmo padrão).
- **P-XX-XX** — Drill-down de FinOps por ambiente e período temporal.
- **P-XX-XX** — Testes E2E para fluxos de FinOps.
