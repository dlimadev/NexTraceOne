> **⚠️ ARCHIVED — April 2026**: Este documento foi gerado como análise pontual de gaps. Muitos dos gaps aqui listados já foram resolvidos. Para o estado atual, consultar `docs/CONSOLIDATED-GAP-ANALYSIS-AND-ACTION-PLAN.md` e `docs/IMPLEMENTATION-STATUS.md`.

# Database & Persistence — Gaps, Erros e Pendências

## 1. Estado resumido
22+ DbContexts com migrations confirmadas para todos os 12 módulos activos. 80+ migration files. Modelo extenso e bem estruturado com EF Core 10 + Npgsql + PostgreSQL 16. Gaps residuais.

## 2. Gaps críticos
Nenhum gap crítico.

## 3. Gaps altos
Nenhum.

## 4. Gaps médios

### 4.1 Outbox Tables sem Domain Events em Maioria dos Módulos
- **Severidade:** MEDIUM
- **Classificação:** PARTIAL
- **Descrição:** `NexTraceDbContextBase` cria tabela de outbox para todos os DbContexts. `ModuleOutboxProcessorJob` processa para 21 DbContexts. Porém a maioria dos módulos não publica domain events para o outbox — as tabelas estão vazias.
- **Impacto:** Infraestrutura pronta mas integração entre módulos via eventos de domínio não funciona na maioria dos módulos.
- **Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs`, `src/platform/NexTraceOne.BackgroundWorkers/Jobs/ModuleOutboxProcessorJob.cs`

### 4.2 Múltiplas Connection Strings Necessárias
- **Severidade:** MEDIUM
- **Classificação:** CONFIG_RISK
- **Descrição:** 22+ DbContexts requerem connection strings individuais em `appsettings.json`. Errar ou omitir uma connection string resulta em falha de migração ou runtime error.
- **Impacto:** Complexidade de deployment. Risco de misconfiguration.
- **Evidência:** `src/platform/NexTraceOne.ApiHost/appsettings.json`, `src/platform/NexTraceOne.ApiHost/appsettings.Development.json`

## 5. Itens mock / stub / placeholder
Nenhum na camada de persistência.

## 6. Erros de desenho / implementação incorreta
Nenhum.

## 7-8. N/A

## 9. Gaps de banco/migração
- Outbox tables subutilizadas (ver 4.1)
- Todas as migrations presentes e confirmadas

## 10. Gaps de configuração
- 22+ connection strings necessárias para deployment completo

## 11. Gaps de documentação
- `docs/IMPLEMENTATION-STATUS.md` afirma "sem migrações confirmadas" para vários módulos (Knowledge, Integrations, ProductAnalytics) — **FALSO**, todas têm migrations

## 12. Gaps de seed/bootstrap
- Coberto em `00-seed-strategy-gaps.md`

## 13. Ações corretivas obrigatórias
1. Documentar lista completa de connection strings necessárias para deployment
2. Avaliar consolidação de DbContexts para reduzir complexidade (ex: modules pequenos podem partilhar DB)
3. Actualizar `docs/IMPLEMENTATION-STATUS.md` para reflectir migrations existentes
4. Publicar domain events para outbox nos módulos que necessitam de integração cross-module
