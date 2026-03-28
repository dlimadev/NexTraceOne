# Product Analytics — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
26 .cs files (menor módulo). Backend real com repository, DbContext, migration, 7 features. Porém: nenhum evento é gerado pelo frontend — o repositório existe mas está vazio porque nenhuma página da aplicação emite `RecordAnalyticsEvent`.

## 2. Gaps críticos
Nenhum.

## 3. Gaps altos

### 3.1 Sem event tracking no frontend
- **Severidade:** HIGH
- **Classificação:** INCOMPLETE
- **Descrição:** O módulo depende de eventos de analytics emitidos pela aplicação (page views, journeys, friction signals). Nenhuma página do frontend emite estes eventos via `RecordAnalyticsEvent`.
- **Impacto:** Todos os handlers de leitura (GetAnalyticsSummary, GetFrictionIndicators, GetJourneys, GetModuleAdoption, GetPersonaUsage, GetValueMilestones) retornam dados vazios porque a tabela `AnalyticsEvent` nunca é populada.
- **Evidência:**
  - Backend: `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/RecordAnalyticsEvent/` — handler real
  - Frontend: Zero chamadas a `/api/v1/analytics/events` em qualquer página

## 4. Gaps médios

### 4.1 Frontend duplicado
- **Severidade:** LOW
- **Classificação:** CLEANUP_REQUIRED
- **Descrição:** `ProductAnalyticsOverviewPage.tsx` existe em duplicado (verificar se path diferente).
- **Evidência:** 2 instâncias de `ProductAnalyticsOverviewPage.tsx` no scan de features

## 5. Itens mock / stub / placeholder
Nenhum mock no código — handlers usam repository real. Porém sem dados porque nenhum evento é gerado.

**CORRECÇÃO DE REGISTO:** `docs/IMPLEMENTATION-STATUS.md` afirma "100% mock; depende de event tracking real não implementado" — os **handlers NÃO são mock**. Usam `IAnalyticsEventRepository` real. O problema é que a tabela está vazia.

## 6. Erros de desenho / implementação incorreta
Nenhum erro de design. O módulo é correcto mas incompleto (falta o producer de eventos).

## 7. Gaps de frontend ligados a este módulo
- `ProductAnalyticsOverviewPage.tsx` — sem error handling, sem empty state
- `ModuleAdoptionPage.tsx` — sem empty state
- Nenhuma página emite eventos de analytics

## 8. Gaps de backend ligados a este módulo
- Handlers de leitura retornam vazio (tabela sem dados)

## 9. Gaps de banco/migração ligados a este módulo
Nenhum — ProductAnalyticsDbContext com migration confirmada.

## 10. Gaps de configuração ligados a este módulo
Nenhum.

## 11. Gaps de documentação ligados a este módulo
- `docs/IMPLEMENTATION-STATUS.md` §ProductAnalytics afirma "100% mock" e "Handlers mock" — **FALSO**, handlers usam repository real

## 12. Gaps de seed/bootstrap ligados a este módulo
Nenhum seed referenciado.

## 13. Ações corretivas obrigatórias
1. Implementar tracking service no frontend que emita `RecordAnalyticsEvent` em page views, journeys e friction signals
2. Actualizar `docs/IMPLEMENTATION-STATUS.md` §ProductAnalytics
3. Resolver duplicação de `ProductAnalyticsOverviewPage.tsx`
4. Adicionar error handling e empty states às páginas
