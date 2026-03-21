# Phase 9 — Remediation Map

**Produto:** NexTraceOne  
**Fase:** 9 — Consolidation, Adherence Audit & 100% Validation  
**Data:** 2026-03-21  
**Actualizado:** 2026-03-21 (pós-remediação P0 implementada durante Fase 9)

**Legenda de Severidade:** 🔴 Critical | 🟠 High | 🟡 Medium | 🔵 Low  
**Legenda de Urgência:** P0 Imediato | P1 Próxima sprint | P2 Backlog prioritário | P3 Backlog  
**Legenda de Status:** ✅ Resolvido | ⏳ Pendente

---

## Mapa de Remediação

| # | Finding | Área | Tipo | Severidade | Bloqueia 100%? | Status | Prioridade |
|---|---|---|---|---|---|---|---|
| F-01 | Migração `AddEnvironmentProfileFields` ausente — campos Profile, Criticality, Code, Description, Region, IsProductionLike existem no domain mas estavam Ignorados no EF Core | Data / Domain | Gap de Persistência | 🔴 Critical | ✅ Sim | ✅ **Resolvido** — Migration `20260321105409_AddEnvironmentProfileFields` criada. `EnvironmentConfiguration.cs` actualizado. `ListEnvironments.EnvironmentResponse` enriquecido com Profile, IsProductionLike, Code. | P0 |
| F-02 | `AnalyzeNonProdEnvironment` handler não validava server-side que o ambiente é não-produtivo. Aceitava `EnvironmentProfile = "production"` | AI / Backend | Validação ausente | 🔴 Critical | ✅ Sim | ✅ **Resolvido** — Validator rejeita profiles `production`, `disasterrecovery`, `dr` (case-insensitive). 10 novos testes adicionados. | P0 |
| F-03 | Frontend `EnvironmentContext.tsx` usava mock `loadEnvironmentsForTenant` em vez de chamada real à API | Frontend | Integração incompleta | 🔴 Critical | ✅ Sim | ✅ **Resolvido** — `EnvironmentContext.tsx` chama `GET /api/v1/identity/environments` via apiClient. Inferência de profile por slug/name como fallback. 7 novos testes. | P0 |
| F-04 | `AssessPromotionReadiness` handler não validava que source é non-prod e target é prod-like | AI / Backend | Validação ausente | 🟠 High | ✅ Sim | ✅ **Resolvido** — Command enriquecido com `SourceIsProductionLike`/`TargetIsProductionLike`. Validator: source must be false, target must be true. Testes existentes actualizados + 2 novos testes. | P0→P1 |
| F-05 | `IncidentRecord.EnvironmentId` existe no domain mas não confirmado na migração `InitialIncidentsSchema` | Data | Persistência não confirmada | 🟠 High | ⚠️ Parcial | ⏳ **Pendente** — Verificar se `environment_id` existe na migração. Se ausente, criar migração `AddEnvironmentIdToIncidents`. | P1 |
| F-06 | Handlers AI não fazem DB lookup para confirmar que `EnvironmentId`s pertencem ao `TenantId` | AI | Segurança / Isolamento | 🟠 High | ⚠️ Parcial | ⏳ **Pendente** — Injectar `ITenantEnvironmentContextResolver` nos handlers AI e rejeitar com `Error.Authorization` se ownership não confirmado. | P1 |
| F-07 | `Release.Environment` é `string` livre em vez de `EnvironmentId?` strongly typed | Data / Domain | Inconsistência domain | 🟡 Medium | ⚠️ Parcial | ⏳ **Pendente** — Deprecar string, adicionar `EnvironmentId?`, criar migration. | P2 |
| F-08 | Integration Tests: falhas em `CriticalFlowsPostgreSqlTests` relacionadas com DB persistence | Tests | Falha de teste | 🟠 High | ✅ Sim | ⏳ **Pendente** — Investigar stack traces. Provável schema mismatch ou migração não aplicada no test container. | P1 |
| F-09 | E2E Tests: `Incidents_GetById_With_Unknown_Id_Should_Return_404` recebe HTTP 500 | Tests | Bug | 🟠 High | ✅ Sim | ⏳ **Pendente** — Handler code está correcto. Investigar causa do 500 no ambiente E2E (possível exception em EfIncidentStore ou middleware). | P1 |
| F-10 | Frontend tests: `EnvironmentContext.test.tsx` falhava por mock mal configurado | Tests | Falha de teste | 🟡 Medium | ⚠️ Parcial | ✅ **Resolvido** — 7 novos testes com api mock correcto. 373/394 passam. 21 falhas pre-existentes. | P0 |
| F-11 | Ausência de teste para validar rejeição de análise em ambiente produtivo | Tests | Gap de cobertura | 🟡 Medium | ⚠️ Parcial | ✅ **Resolvido** — `Validate_ShouldFail_WhenEnvironmentProfileIsProductionLike` + `Validate_ShouldPass_WhenEnvironmentProfileIsNonProduction` adicionados. | P0 |
| F-12 | FK constraints ausentes para `tenant_id` nas colunas de `AddTenantContextToReleases` | Data | Integridade referencial | 🔵 Low | ❌ Não | ⏳ **Pendente** — Adicionar FK opcional ou documentar decisão de enforcement via application layer. | P3 |

---

## Plano de Remediação por Prioridade

### P0 — CONCLUÍDOS durante Fase 9

| # | Finding | Implementação |
|---|---|---|
| F-01 | Criar migração `AddEnvironmentProfileFields` | Migration criada, EnvironmentConfiguration actualizada, ListEnvironments enriquecida |
| F-02 | Adicionar validação server-side em `AnalyzeNonProdEnvironment` | HashSet de profiles bloqueados no Validator + 10 novos testes |
| F-03 | Substituir mock por API real em `EnvironmentContext.tsx` | Chamada real + profile inference + 7 novos testes |
| F-04 | Validar perfis source/target em `AssessPromotionReadiness` | Novos campos + Validator + testes actualizados + 2 novos testes |
| F-10/F-11 | Testes de regressão adicionados | 19 novos testes no total |

### P1 — Próxima Sprint

| # | Finding | Esforço estimado |
|---|---|---|
| F-05 | Confirmar/criar migração para `IncidentRecord.EnvironmentId` | 2h |
| F-06 | DB lookup de tenant ownership nos handlers AI | 3h |
| F-08 | Corrigir falhas nos Integration Tests | 4h |
| F-09 | Investigar e corrigir HTTP 500 em Incidents GET-by-unknown-id | 2h |

### P2 — Backlog Prioritário

| # | Finding | Esforço estimado |
|---|---|---|
| F-07 | Migrar `Release.Environment` (string) para `EnvironmentId?` strongly typed | 4h + migration |

### P3 — Backlog

| # | Finding | Esforço estimado |
|---|---|---|
| F-12 | Documentar/adicionar FK para tenant_id em releases | 1h |

---

## Sumário de Impacto (pós-remediação P0)

| Severidade | Total | Resolvidos | Pendentes |
|---|---|---|---|
| 🔴 Critical | 3 | 3 | 0 |
| 🟠 High | 4 | 1 | 3 |
| 🟡 Medium | 3 | 2 | 1 |
| 🔵 Low | 1 | 0 | 1 |
| **Total** | **11** | **6** | **5** |
