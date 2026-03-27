# P-03-02 — Substituir mocks nas páginas de Product Analytics por chamadas reais à API

## 1. Título

Substituir dados mock nas páginas PersonaUsagePage, ValueTrackingPage e JourneyFunnelPage por chamadas reais ao backend de ProductAnalytics.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

As páginas de Product Analytics usam dados mock locais. Este prompt conecta-as aos handlers GetPersonaUsage, GetValueMilestones e GetJourneys do backend, que após P-02-03 já retornam dados reais da tabela AnalyticsEvent.

## 4. Problema atual

- `src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx` — dados mock frontais (gap D-033).
- `src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx` — dados mock frontais (gap D-034).
- `src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx` — dados mock frontais (gap D-035).
- O ficheiro `src/frontend/src/features/product-analytics/api/productAnalyticsApi.ts` pode ter funções stub.
- Páginas auxiliares como `ProductAnalyticsOverviewPage.tsx` e `ModuleAdoptionPage.tsx` existem no mesmo diretório.

## 5. Escopo permitido

- `src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx`
- `src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx`
- `src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx`
- `src/frontend/src/features/product-analytics/api/productAnalyticsApi.ts`
- Tipos TypeScript associados se necessário.

## 6. Escopo proibido

- Não alterar páginas fora de `src/frontend/src/features/product-analytics/`.
- Não alterar o backend.
- Não alterar ProductAnalyticsOverviewPage ou ModuleAdoptionPage neste prompt.
- Não remover componentes de UI existentes.

## 7. Ficheiros principais candidatos a alteração

1. `src/frontend/src/features/product-analytics/pages/PersonaUsagePage.tsx`
2. `src/frontend/src/features/product-analytics/pages/ValueTrackingPage.tsx`
3. `src/frontend/src/features/product-analytics/pages/JourneyFunnelPage.tsx`
4. `src/frontend/src/features/product-analytics/api/productAnalyticsApi.ts`
5. Ficheiros de tipos TypeScript para DTOs de resposta (novo ou existente).
6. `src/frontend/src/features/product-analytics/pages/ProductAnalyticsOverviewPage.tsx` (apenas se necessário para links)

## 8. Responsabilidades permitidas

- Implementar funções de API para GET /api/v1/productanalytics/persona-usage, /journeys, /value-milestones.
- Criar hooks TanStack Query com useQuery para cada endpoint.
- Substituir dados mock por dados da API.
- Implementar estados de loading (skeleton), erro e vazio com mensagens i18n.
- Criar tipos TypeScript que correspondam aos DTOs do backend.

## 9. Responsabilidades proibidas

- Não criar gráficos novos — usar os componentes ECharts existentes.
- Não implementar filtros avançados neste prompt.
- Não criar testes E2E neste prompt.

## 10. Critérios de aceite

- [ ] PersonaUsagePage mostra dados reais da API (ou estado vazio se sem dados).
- [ ] ValueTrackingPage mostra milestones reais da API.
- [ ] JourneyFunnelPage mostra journeys reais da API.
- [ ] Loading states visíveis durante fetch.
- [ ] Error states com mensagem i18n quando API falha.
- [ ] Build frontend sem erros.

## 11. Validações obrigatórias

- Build do frontend sem erros.
- Lint sem erros críticos.
- Verificar que as páginas não crasham quando a API retorna lista vazia.
- Verificar que tipos TypeScript são consistentes com o contrato da API.

## 12. Riscos e cuidados

- Se P-02-03 não estiver concluído, as APIs retornarão dados mínimos — páginas devem lidar com isso graciosamente.
- Nomes de campos nos DTOs do backend podem diferir do que o frontend espera — verificar mapeamento.
- O AnalyticsEventTracker em `src/frontend/src/features/product-analytics/AnalyticsEventTracker.tsx` não deve ser alterado.

## 13. Dependências

- **P-02-03** — Backend handlers GetPersonaUsage, GetJourneys e GetValueMilestones devem retornar dados reais.
- Endpoints REST de ProductAnalytics registados no `ProductAnalyticsEndpointModule.cs`.

## 14. Próximos prompts sugeridos

- **P-03-01** — Substituir mocks em Operations (mesmo padrão).
- **P-XX-XX** — Filtros de período temporal nas páginas de analytics.
- **P-XX-XX** — Testes E2E para fluxos de Product Analytics.
