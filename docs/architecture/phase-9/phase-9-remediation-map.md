# Phase 9 — Remediation Map

**Produto:** NexTraceOne  
**Fase:** 9 — Consolidation, Adherence Audit & 100% Validation  
**Data:** 2026-03-21

**Legenda de Severidade:** 🔴 Critical | 🟠 High | 🟡 Medium | 🔵 Low  
**Legenda de Urgência:** P0 Imediato | P1 Próxima sprint | P2 Backlog prioritário | P3 Backlog

---

## Mapa de Remediação

| # | Finding | Área | Ficheiro(s) | Tipo | Severidade | Impacto | Bloqueia 100%? | Recomendação | Prioridade |
|---|---|---|---|---|---|---|---|---|---|
| F-01 | Migração `AddEnvironmentProfileFields` ausente — campos Profile, Criticality, Code, Description, Region, IsProductionLike existem no domain mas estão explicitamente ignorados no EF Core config e não são persistidos na BD | Data / Domain | `IdentityAccess.Infrastructure/Persistence/Configurations/EnvironmentConfiguration.cs` | Gap de Persistência | 🔴 Critical | Toda funcionalidade que depende de `IsProductionLike` ou `EnvironmentProfile` da BD não funciona. Middleware e IA usam defaults ao invés de dados reais. | ✅ Sim | Criar migração EF Core com: `profile` (int), `criticality` (int), `code` (varchar 50 nullable), `description` (text nullable), `region` (varchar 100 nullable), `is_production_like` (bool default false). Actualizar `EnvironmentConfiguration` para mapear estes campos. | P0 |
| F-02 | `AnalyzeNonProdEnvironment` handler não valida server-side que o ambiente é não-produtivo. Aceita qualquer string em `EnvironmentProfile` incluindo "production" | AI / Backend | `AIKnowledge.Application/Orchestration/Features/AnalyzeNonProdEnvironment/AnalyzeNonProdEnvironment.cs` | Validação ausente | 🔴 Critical | Um utilizador pode solicitar análise de ambiente produtivo, expondo dados de produção a uma análise IA não governada. Risco de segurança e de conformidade. | ✅ Sim | Adicionar validação no Validator: `RuleFor(x => x.EnvironmentProfile).Must(p => !IsProductionProfile(p)).WithMessage(...)`. Definir lista de perfis bloqueados: `"production"`, `"disasterrecovery"`. Ou receber `IsProductionLike` como booleano e rejeitar se true. | P0 |
| F-03 | Frontend `EnvironmentContext.tsx` usa mock `loadEnvironmentsForTenant` em vez de chamada real à API. Todos os utilizadores vêem sempre os mesmos 4 ambientes sintéticos | Frontend | `src/frontend/src/contexts/EnvironmentContext.tsx` | Integração incompleta | 🔴 Critical | Frontend não reflecte ambientes reais do tenant. EnvironmentBanner e WorkspaceSwitcher operam com dados falsos. Toda a segmentação por ambiente no frontend é baseada em dados fictícios. | ✅ Sim | Substituir `loadEnvironmentsForTenant` por chamada real a `GET /api/v1/identity/environments?tenantId=X` usando o `apiClient`. Endpoint já existe no backend. Tratar estado de loading e erro. | P0 |
| F-04 | `AssessPromotionReadiness` handler não valida que source é non-prod e target é prod-like. Validator apenas garante `SourceEnvironmentId != TargetEnvironmentId` | AI / Backend | `AIKnowledge.Application/Orchestration/Features/AssessPromotionReadiness/AssessPromotionReadiness.cs` | Validação ausente | 🟠 High | Um utilizador pode solicitar promoção de produção para development, invertendo o fluxo esperado. Análise IA pode produzir recomendações incorrectas ou enganosas. | ✅ Sim | Adicionar campos `SourceIsProductionLike` (bool) e `TargetIsProductionLike` (bool) ao Command. Validator: `RuleFor(x => x.SourceIsProductionLike).Equal(false)`. `RuleFor(x => x.TargetIsProductionLike).Equal(true)`. | P1 |
| F-05 | `IncidentRecord.EnvironmentId` existe no domain (linha 147) mas não foi confirmado na migração `20260317161138_InitialIncidentsSchema`. Risco de campo não persistido | Data | `OperationalIntelligence.Infrastructure/Incidents/Persistence/Migrations/20260317161138_InitialIncidentsSchema.cs` | Persistência não confirmada | 🟠 High | IncidentRecord pode perder contexto de ambiente ao ser persistido. Filtros por ambiente em incidents queries retornam resultados incorrectos. | ⚠️ Parcial | Verificar se `environment_id` existe na migração. Se ausente, criar migração `AddEnvironmentIdToIncidents`. Verificar `IncidentRecordConfiguration.cs` para mapeamento. | P1 |
| F-06 | `CompareEnvironments` e demais handlers AI não fazem DB lookup para confirmar que `EnvironmentId`s passados pertencem ao `TenantId` indicado. Isolamento cross-tenant é apenas via grounding context | AI | `AIKnowledge.Application/Orchestration/Features/CompareEnvironments/CompareEnvironments.cs` | Segurança / Isolamento | 🟠 High | Um actor malicioso com TenantId válido pode passar EnvironmentIds de outro tenant. IA processa o pedido com contexto de outro tenant incluído no grounding. | ⚠️ Parcial | Injectar `IEnvironmentRepository` ou `ITenantEnvironmentContextResolver` nos handlers AI. Verificar `await resolver.ResolveAsync(tenantId, environmentId)` — se retornar null, rejeitar com `Error.Authorization("AIKnowledge.Environment.NotInTenant")`. | P1 |
| F-07 | Release domain entity usa `Environment` como `string` (campo livre) em vez de `EnvironmentId` strongly typed. Migração `AddTenantContextToReleases` adiciona `environment_id` como coluna separada sem FK ao domain entity | Data / Domain | `ChangeGovernance.Domain/.../Entities/Release.cs` | Inconsistência domain ↔ BD | 🟡 Medium | Consultas por ambiente em releases podem retornar resultados incorrectos ou requerer joins manuais. Dívida técnica que cresce com o produto. | ⚠️ Parcial | Deprecar `Environment` string em favor de `EnvironmentId?` strongly typed. Criar migration para fazer a transição. Actualizar `ReleaseConfiguration.cs`. | P2 |
| F-08 | Testes de integração (`IntegrationTests.dll`): 8 falhas em `CriticalFlowsPostgreSqlTests` relacionadas com PostgreSQL/DB persistence | Tests | `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CriticalFlowsPostgreSqlTests.cs` | Falha de teste | 🟠 High | Fluxos críticos de persistência (ChangeGovernance + Releases) não estão validados por testes verdes. Risco de regressão em produção. | ✅ Sim | Investigar stack traces das 8 falhas. Falha detectada: `BatchExecutor.ExecuteAsync` → provavelmente schema mismatch ou migração não aplicada no test container. Aplicar migrações pendentes no test container setup. | P1 |
| F-09 | Testes E2E (`E2E.Tests.dll`): 8 falhas incluindo `Incidents_GetById_With_Unknown_Id_Should_Return_404` que recebe HTTP 500 em vez de 404 | Tests | `tests/platform/NexTraceOne.E2E.Tests/Flows/CatalogAndIncidentApiFlowTests.cs` | Falha de teste / Bug | 🟠 High | Incident GET endpoint retorna 500 (unhandled exception) quando incidente não existe. Viola contract REST e expõe stack traces ao cliente. | ✅ Sim | Verificar `GetIncidentById` handler — adicionar verificação `if (incident is null) return Error.NotFound("Incidents.NotFound")`. Garantir que `Result<T>` com `NotFound` é mapeado para HTTP 404. | P0 |
| F-10 | Frontend vitest não executa no CI por falta de dependências instaladas (`vitest` não encontrado) | Tests | `src/frontend/` | CI/CD Gap | 🟡 Medium | 42 ficheiros de teste frontend não são executados. Regressões no frontend não são detectadas no CI. | ⚠️ Parcial | Garantir `npm install` antes de `npx vitest run` no pipeline CI. Ou adicionar job de build frontend com `npm ci && npm test`. | P1 |
| F-11 | Ausência de teste específico que valida que `AnalyzeNonProdEnvironment` rejeita ambiente de produção | Tests | `tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/Orchestration/Features/` | Gap de cobertura de testes | 🟡 Medium | Comportamento de segurança crítico (rejeitar análise em prod) não tem teste de regressão. | ⚠️ Parcial | Adicionar `AnalyzeNonProd_WithProductionProfile_ShouldReturnValidationError()` com EnvironmentProfile = "production". Verificar que result.IsFailure == true. | P1 |
| F-12 | FK constraints ausentes para `tenant_id` nas colunas adicionadas por `AddTenantContextToReleases` | Data | `ChangeGovernance.Infrastructure/.../Migrations/20260320220001_AddTenantContextToReleases.cs` | Integridade referencial | 🔵 Low | Sem FK, é possível persistir releases com TenantId inválido ou de tenant inexistente. Dados órfãos possíveis. | ❌ Não | Adicionar FK opcional `tenant_id → identity_tenants(id)`. Dado que é nullable e cross-module, pode ser enforced via application layer em vez de FK. Documentar decisão. | P3 |

---

## Plano de Remediação por Prioridade

### P0 — Imediato (antes de qualquer release)

| # | Finding | Responsável sugerido | Esforço estimado |
|---|---|---|---|
| F-01 | Criar migração `AddEnvironmentProfileFields` | Backend Team | 2h |
| F-02 | Adicionar validação server-side em `AnalyzeNonProdEnvironment` | AI Team | 1h |
| F-03 | Substituir mock por API real em `EnvironmentContext.tsx` | Frontend Team | 3h |
| F-09 | Corrigir HTTP 500 em Incidents GET-by-unknown-id | Backend Team | 1h |

### P1 — Próxima Sprint

| # | Finding | Responsável sugerido | Esforço estimado |
|---|---|---|---|
| F-04 | Validar perfis source/target em `AssessPromotionReadiness` | AI Team | 2h |
| F-05 | Confirmar/criar migração para `IncidentRecord.EnvironmentId` | Backend Team | 2h |
| F-06 | DB lookup de tenant ownership nos handlers AI | AI Team | 3h |
| F-08 | Corrigir falhas nos Integration Tests | Backend Team | 4h |
| F-10 | Garantir execução de frontend tests no CI | DevOps | 1h |
| F-11 | Adicionar teste de rejeição de análise em ambiente produtivo | AI Team | 1h |

### P2 — Backlog Prioritário

| # | Finding | Esforço estimado |
|---|---|---|
| F-07 | Migrar `Release.Environment` (string) para `EnvironmentId?` strongly typed | 4h + migration |

### P3 — Backlog

| # | Finding | Esforço estimado |
|---|---|---|
| F-12 | Documentar/adicionar FK para tenant_id em releases | 1h |

---

## Sumário de Impacto

| Severidade | Contagem |
|---|---|
| 🔴 Critical | 3 |
| 🟠 High | 4 |
| 🟡 Medium | 3 |
| 🔵 Low | 1 |
| **Total** | **11** |

| Bloqueia 100%? | Contagem |
|---|---|
| ✅ Sim (bloqueador) | 6 |
| ⚠️ Parcial | 4 |
| ❌ Não | 1 |
