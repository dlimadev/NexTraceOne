# Environment Management — Module Scope Finalization

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Escopo funcional oficial do módulo

O módulo Environment Management é responsável pelo **ciclo de vida completo dos ambientes runtime** da plataforma NexTraceOne, incluindo registo, classificação, políticas, controlo de acesso e governança operacional.

Não confundir com `DeploymentEnvironment` do Change Governance, que representa estágios no pipeline de promoção.

---

## 2. Capacidades por área funcional

### 2.1 Registo e lifecycle de ambientes

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Criar ambiente | ✅ Implementado | `CreateEnvironment` handler em `NexTraceOne.IdentityAccess.Application/Features/` | Factory methods `Create()` no domínio |
| Listar ambientes | ✅ Implementado | `ListEnvironments` handler + `GET /api/v1/environments` | Filtrado por TenantId via RLS |
| Editar ambiente | ✅ Implementado | `UpdateEnvironment` handler + `PUT /api/v1/environments/{id}` | `UpdateBasicInfo()`, `UpdateLocationInfo()` |
| Activar/desactivar | ✅ Domínio | `Environment.Activate()`, `Environment.Deactivate()` | ⚠️ Sem endpoint dedicado — acoplado a UpdateEnvironment |
| Eliminar ambiente (soft-delete) | ❌ Ausente | — | Não existe `Delete()` nem `IsDeleted` flag |
| Reordenar ambientes | ✅ Domínio | `Environment.UpdateSortOrder(int)` | ⚠️ Sem endpoint dedicado |

### 2.2 Designação de produção

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Designar primary production | ✅ Implementado | `SetPrimaryProductionEnvironment` handler + endpoint | `DesignateAsPrimaryProduction()` no domínio |
| Revogar primary production | ✅ Domínio | `RevokePrimaryProductionDesignation()` | Invocado internamente pelo handler de designação |
| Consultar primary production | ✅ Implementado | `GetPrimaryProductionEnvironment` + `GET /api/v1/environments/primary-production` | Query dedicada |
| Flag `IsProductionLike` | ✅ Domínio | Propriedade em `Environment.cs` | Ambiente que se comporta como produção sem ser primary |
| Partial unique index | ✅ DB | `(TenantId, IsPrimaryProduction) WHERE IsPrimaryProduction=true AND IsActive=true` | Garante unicidade |

### 2.3 Perfil e criticidade

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Definir perfil (`EnvironmentProfile`) | ✅ Domínio | `UpdateProfile(EnvironmentProfile, EnvironmentCriticality)` | 9 perfis: Development → PerformanceTesting |
| Definir criticidade (`EnvironmentCriticality`) | ✅ Domínio | Actualizado junto com perfil | 4 níveis: Low → Critical |
| UI profile resolution | ✅ Infra | `EnvironmentProfileResolver` → `EnvironmentUiProfile` | Badge color, protection warning, AI flag |
| Código do ambiente (`Code`) | ✅ Domínio | Propriedade `Code` (max 50) | Ex: "DEV", "PROD-BR", "STG-EU" |
| Região | ✅ Domínio | Propriedade `Region` (max 100) | Ex: "eu-west-1" |

### 2.4 Controlo de acesso a ambientes

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Conceder acesso | ✅ Implementado | `GrantEnvironmentAccess` handler + endpoint | Cria `EnvironmentAccess` |
| Revogar acesso | ✅ Domínio | `EnvironmentAccess.Revoke()` | ⚠️ Sem endpoint dedicado |
| Níveis de acesso | ✅ Domínio | `none`, `read`, `write`, `admin` | Enum em `EnvironmentAccess.cs` |
| Expiração de acesso | ✅ Domínio | `ExpiresAt?` + `IsActiveAt(DateTimeOffset)` | Temporal access control |
| Alterar nível | ✅ Domínio | `ChangeAccessLevel()` | ⚠️ Sem endpoint dedicado |
| Listar acessos por ambiente | ❌ Ausente | — | Necessário para gestão administrativa |

### 2.5 Políticas por ambiente (Phase 2)

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Promotion approval policy | ⚠️ Entidade definida | `EnvironmentPolicy.cs` (124 linhas) | ❌ Sem DbSet, sem EF mapping |
| Freeze window | ⚠️ Entidade definida | Tipo `FreezeWindow` em `EnvironmentPolicy` | ❌ Sem persistência |
| Alert escalation | ⚠️ Entidade definida | Tipo `AlertEscalation` em `EnvironmentPolicy` | ❌ Sem persistência |
| Deploy quality gate | ⚠️ Entidade definida | Tipo `DeployQualityGate` em `EnvironmentPolicy` | ❌ Sem persistência |

### 2.6 Políticas de telemetria (Phase 2)

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Retenção por ambiente | ⚠️ Entidade definida | `EnvironmentTelemetryPolicy.cs` (122 linhas) | ❌ Sem DbSet, sem EF mapping |
| Nível de verbosidade | ⚠️ Entidade definida | `VerbosityLevel` | ❌ Sem persistência |
| Comparação cross-environment | ⚠️ Entidade definida | `AllowCrossEnvironmentComparison` | ❌ Sem persistência |

### 2.7 Bindings de integração (Phase 2)

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Binding observability | ⚠️ Entidade definida | `EnvironmentIntegrationBinding.cs` (90+ linhas) | ❌ Sem DbSet, sem EF mapping |
| Binding alerting | ⚠️ Entidade definida | Tipo `alerting` | ❌ Sem persistência |
| Binding CI/CD | ⚠️ Entidade definida | Tipo `ci_cd` | ❌ Sem persistência |
| Binding event broker | ⚠️ Entidade definida | Tipo `event_broker` | ❌ Sem persistência |

### 2.8 Caminhos de promoção

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Definir promotion path | ❌ Ausente | — | Ex: DEV → STG → PROD |
| Validar promoção | ❌ Ausente | — | Necessário para Change Confidence |
| Visualizar promotion flow | ❌ Ausente | — | Diagrama interactivo |

### 2.9 Baseline e drift

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Definir baseline | ❌ Ausente | — | Snapshot de configuração esperada |
| Detectar drift | ❌ Ausente | — | Comparação baseline vs actual |
| Remediar drift | ❌ Ausente | — | Automação ou sugestão |

### 2.10 Readiness e comparação

| Capacidade | Estado | Artefacto actual | Notas |
|-----------|--------|-------------------|-------|
| Readiness score | ❌ Ausente | — | Ambiente pronto para deploy? |
| Comparação entre ambientes | ⚠️ Frontend only | `EnvironmentComparisonPage.tsx` (623 linhas) em `operations` | Sem backend dedicado |
| Grouping de ambientes | ❌ Ausente | — | Clusters, regiões, domínios |

---

## 3. Sumário de escopo — estado actual vs alvo

| Área funcional | Capacidades identificadas | ✅ Implementadas | ⚠️ Parciais | ❌ Ausentes |
|---------------|--------------------------|-------------------|------------|-------------|
| Registo e lifecycle | 6 | 4 | 0 | 2 |
| Designação de produção | 5 | 4 | 0 | 1 |
| Perfil e criticidade | 5 | 5 | 0 | 0 |
| Controlo de acesso | 6 | 2 | 3 | 1 |
| Políticas por ambiente | 4 | 0 | 4 | 0 |
| Políticas de telemetria | 3 | 0 | 3 | 0 |
| Bindings de integração | 4 | 0 | 4 | 0 |
| Caminhos de promoção | 3 | 0 | 0 | 3 |
| Baseline e drift | 3 | 0 | 0 | 3 |
| Readiness e comparação | 3 | 0 | 1 | 2 |
| **TOTAL** | **42** | **15** (36%) | **15** (36%) | **12** (28%) |

---

## 4. Escopo Phase 1 (módulo funcional mínimo)

Capacidades que devem existir antes do módulo ser considerado operacional:

1. ✅ CRUD completo de ambientes (criar, listar, editar, activar/desactivar)
2. ❌ Soft-delete de ambientes
3. ✅ Designação de primary production
4. ✅ Perfil e criticidade
5. ❌ Permissões dedicadas `env:environments:read`, `env:environments:write`, `env:environments:admin`
6. ❌ Endpoint dedicado para revogar acesso
7. ❌ Endpoint dedicado para listar acessos
8. ❌ Sidebar entry na navegação principal
9. ❌ Página de detalhe do ambiente

---

## 5. Escopo Phase 2 (governança e políticas)

1. Persistência de `EnvironmentPolicy` (4 tipos)
2. Persistência de `EnvironmentTelemetryPolicy`
3. Persistência de `EnvironmentIntegrationBinding`
4. Promotion paths (definição e visualização)
5. Baseline management

---

## 6. Escopo Phase 3 (intelligence e automação)

1. Drift detection
2. Readiness scoring
3. Environment grouping (clusters, regiões)
4. Cross-environment comparison backend
5. AI-assisted environment analysis

---

## 7. Fora de escopo (pertence a outros módulos)

| Capacidade | Módulo dono | Justificação |
|-----------|-------------|--------------|
| `DeploymentEnvironment` | Change Governance | Estágios de pipeline, não ambientes runtime |
| `PromotionGate` | Change Governance | Gates de qualidade no promotion flow |
| `IncidentRecord.EnvironmentId` | Operational Intelligence | Referência por ID, não gestão |
| `EnvironmentResolutionMiddleware` | Identity & Access | Cross-cutting concern do request pipeline |
| `EnvironmentAccessAuthorizationHandler` | Identity & Access | Policy-based auth do ASP.NET |
| Configurações por ambiente | Configuration | Scope resolution usa `EnvironmentId` |

---

## 8. Dependências de escopo com outros módulos

```
Environment Management ──expõe──▶ ICurrentEnvironment (via BuildingBlocks)
Environment Management ──expõe──▶ Environment aggregate (consulta)
Environment Management ──expõe──▶ env:* permissions

Identity & Access ──consome──▶ EnvironmentAccess (para auth decisions)
Change Governance ──consome──▶ IsPrimaryProduction, Criticality (para risk scoring)
Operational Intelligence ──consome──▶ EnvironmentId (para contextualizar incidentes)
AI & Knowledge ──consome──▶ EnvironmentComparisonContext, PromotionRiskAnalysisContext
Configuration ──consome──▶ EnvironmentId como ScopeReferenceId
```
