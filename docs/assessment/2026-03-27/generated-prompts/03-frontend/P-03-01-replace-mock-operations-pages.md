# P-03-01 — Substituir mocks nas páginas de Operations por chamadas reais à API

## 1. Título

Substituir dados mock nas páginas TeamReliabilityPage, ServiceReliabilityDetailPage e PlatformOperationsPage por chamadas reais ao backend.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

As páginas de Operations no frontend usam dados mock locais em vez de consumir a API real do backend. Este prompt substitui esses mocks por chamadas HTTP reais usando TanStack Query, conectando o frontend aos handlers de reliability e platform operations já existentes no backend.

## 4. Problema atual

- `src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx` — usa dados mock frontais (gap D-030).
- `src/frontend/src/features/operations/pages/ServiceReliabilityDetailPage.tsx` — usa dados mock frontais (gap D-031).
- `src/frontend/src/features/operations/pages/PlatformOperationsPage.tsx` — usa dados mock frontais (gap D-032).
- Os ficheiros de API em `src/frontend/src/features/operations/api/reliability.ts` e `src/frontend/src/features/operations/api/platformOps.ts` podem já ter funções definidas mas sem implementação real.
- Os testes em `src/__tests__/pages/TeamReliabilityPage.test.tsx` e `ServiceReliabilityDetailPage.test.tsx` validam a presença de DemoBanner, confirmando uso de mocks.

## 5. Escopo permitido

- `src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx`
- `src/frontend/src/features/operations/pages/ServiceReliabilityDetailPage.tsx`
- `src/frontend/src/features/operations/pages/PlatformOperationsPage.tsx`
- `src/frontend/src/features/operations/api/reliability.ts`
- `src/frontend/src/features/operations/api/platformOps.ts`
- `src/frontend/src/features/operations/api/runtimeIntelligence.ts` (se necessário)

## 6. Escopo proibido

- Não alterar páginas fora de `src/frontend/src/features/operations/`.
- Não alterar o backend — assumir que os endpoints já existem e retornam dados reais.
- Não remover componentes de UI — apenas substituir a fonte de dados.
- Não alterar o design system ou layout das páginas.

## 7. Ficheiros principais candidatos a alteração

1. `src/frontend/src/features/operations/pages/TeamReliabilityPage.tsx`
2. `src/frontend/src/features/operations/pages/ServiceReliabilityDetailPage.tsx`
3. `src/frontend/src/features/operations/pages/PlatformOperationsPage.tsx`
4. `src/frontend/src/features/operations/api/reliability.ts`
5. `src/frontend/src/features/operations/api/platformOps.ts`
6. `src/frontend/src/features/operations/api/runtimeIntelligence.ts`

## 8. Responsabilidades permitidas

- Implementar funções de API com fetch/axios para endpoints reais de reliability e platform operations.
- Criar hooks TanStack Query (useQuery) para cada página com loading, error e empty states.
- Substituir arrays mock por dados retornados pelos hooks.
- Manter DemoBanner condicional — mostrar apenas se o backend retornar flag de dados simulados.
- Adicionar tratamento de erro com mensagens i18n.
- Preservar layout e componentes visuais existentes.

## 9. Responsabilidades proibidas

- Não implementar cache avançado — usar defaults do TanStack Query.
- Não criar novos componentes de UI.
- Não alterar rotas ou navegação.
- Não implementar websockets ou polling — usar fetch simples.

## 10. Critérios de aceite

- [ ] TeamReliabilityPage consome dados reais da API de reliability.
- [ ] ServiceReliabilityDetailPage consome dados reais da API.
- [ ] PlatformOperationsPage consome dados reais da API de platform operations.
- [ ] Estados de loading, erro e vazio estão implementados nas 3 páginas.
- [ ] DemoBanner removido ou condicional (não fixo).
- [ ] Compilação frontend sem erros (`npm run build` ou equivalente).
- [ ] Textos de erro e estados usam chaves i18n.

## 11. Validações obrigatórias

- Build do frontend sem erros.
- Lint sem erros críticos.
- Testes existentes em `src/__tests__/pages/TeamReliabilityPage.test.tsx` e `ServiceReliabilityDetailPage.test.tsx` devem ser atualizados ou continuar a passar.

## 12. Riscos e cuidados

- Se o backend ainda retornar dados simulados, as páginas mostrarão esses dados como reais — manter flag de aviso.
- Tipos TypeScript devem corresponder ao contrato da API — verificar DTOs do backend.
- Endpoints de reliability podem não existir para todos os cenários — tratar 404 com estado vazio.
- Rate limiting de chamadas API deve ser considerado (staleTime no TanStack Query).

## 13. Dependências

- **P-01-01** — Backend handlers de reliability devem retornar dados reais (não mocks).
- Endpoints REST de reliability e platform operations devem estar registados e acessíveis.

## 14. Próximos prompts sugeridos

- **P-03-03** — Remover DemoBanner das páginas de FinOps (mesmo padrão).
- **P-XX-XX** — Testes E2E com Playwright para fluxos de Operations.
