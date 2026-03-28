# Catalog — Gaps, Erros e Pendências

## 1. Estado resumido do módulo
317 .cs files, 84 features, 91.7% real. 3 DbContexts com migrations confirmadas. Módulo com a maior cobertura funcional do projecto. Gaps residuais e específicos.

## 2. Gaps críticos
Nenhum gap crítico.

## 3. Gaps altos

### 3.1 `SearchCatalog` é stub intencional
- **Severidade:** HIGH
- **Classificação:** STUB
- **Descrição:** O endpoint `SearchCatalog` no Developer Portal é um stub intencional aguardando integração cross-module com Knowledge e outros módulos.
- **Impacto:** Global Search funciona via PostgreSQL FTS directo mas o catálogo do Developer Portal não tem pesquisa unificada.
- **Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Catalog — "SearchCatalog é stub intencional aguardando integração cross-module"

## 4. Gaps médios

### 4.1 Developer Portal parcial
- **Severidade:** MEDIUM
- **Classificação:** PARTIAL
- **Descrição:** 7 stubs no backend do Developer Portal. `RecordAnalyticsEvent`, `CreateSubscription`, `ExecutePlayground` são reais mas funcionalidades avançadas dependem de módulos externos.
- **Evidência:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Portal/`

## 5. Itens mock / stub / placeholder
- `SearchCatalog` — stub intencional (cross-module integration pending)

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7. Gaps de frontend ligados a este módulo
- `DeveloperPortalPage.tsx` — parcialmente funcional (7 stubs backend)
- `GlobalSearchPage.tsx` — funcional via PostgreSQL FTS; SearchCatalog stub

## 8. Gaps de backend ligados a este módulo
- `SearchCatalog` stub

## 9. Gaps de banco/migração ligados a este módulo
Nenhum — 3 DbContexts com migrations confirmadas.

## 10. Gaps de configuração ligados a este módulo
Nenhum.

## 11. Gaps de documentação ligados a este módulo
- `docs/IMPLEMENTATION-STATUS.md` está correcto para Catalog, mas a secção §CrossModule afirma `IContractsModule = PLAN` quando `ContractsModuleService.cs` **existe e está registado em DI**.

## 12. Gaps de seed/bootstrap ligados a este módulo
- `seed-catalog.sql` referenciado mas **NÃO EXISTE** no disco.

## 13. Ações corretivas obrigatórias
1. Criar `seed-catalog.sql` ou remover referência
2. Actualizar `docs/IMPLEMENTATION-STATUS.md` §CrossModule para reflectir `IContractsModule = IMPLEMENTED`
