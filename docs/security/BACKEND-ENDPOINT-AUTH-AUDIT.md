# Backend Endpoint Authorization Audit

## Resumo da Auditoria

**Data:** 2026-03-21  
**Fase:** Phase 1 — Security Critical + Production Baseline  
**Âmbito:** Todos os EndpointModules do NexTraceOne backend

### Resultado
| Status | Contagem |
|---|---|
| ✅ Protegidos (RequirePermission / RequireAuthorization) | 291 |
| 🔓 Públicos Legítimos (AllowAnonymous justificado) | 8 |
| 🚫 **Expostos sem proteção (CORRIGIDOS)** | 32 |

---

## Modelo de Autorização

O NexTraceOne usa autorização granular baseada em permissões:
- **`RequirePermission("scope:resource:action")`** — requer um JWT com a claim de permissão específica
- **`RequireAuthorization()`** — requer autenticação (qualquer utilizador autenticado)
- **`AllowAnonymous()`** — endpoint público, sem autenticação necessária (deve ser justificado)

A implementação usa `PermissionPolicyProvider` + `PermissionAuthorizationHandler` em `NexTraceOne.BuildingBlocks.Security`.

---

## Endpoints Públicos Legítimos (AllowAnonymous)

Estes endpoints são públicos por design e estão documentados e justificados:

| Módulo | Endpoint | Método | Justificação |
|---|---|---|---|
| IdentityAccess | `POST /api/v1/auth/login` | POST | Login — deve ser acessível sem token |
| IdentityAccess | `POST /api/v1/auth/federated` | POST | Login federado (SSO/OIDC) |
| IdentityAccess | `POST /api/v1/auth/refresh` | POST | Refresh de token — usa refresh token no body |
| IdentityAccess | `POST /api/v1/auth/oidc/start` | POST | Início de fluxo OIDC — redireciona para provider |
| IdentityAccess | `GET /api/v1/auth/oidc/callback` | GET | Callback OIDC — recebe code do provider |
| IdentityAccess | `POST /api/v1/session` (cookie login) | POST | Login via cookie session — inicia sessão |
| IdentityAccess | `GET /api/v1/session/csrf-token` | GET | Emissão de CSRF token — necessário antes do login |
| ApiHost | `GET /health` | GET | Health check operacional |
| ApiHost | `GET /ready` | GET | Readiness probe |
| ApiHost | `GET /live` | GET | Liveness probe |

---

## Auditoria por Módulo

### AIKnowledge (5 ficheiros — 57 endpoints)

| Ficheiro | Endpoints | Status | Permissões Usadas |
|---|---|---|---|
| `ExternalAiEndpointModule.cs` | 2 | ✅ Todos protegidos | `ai:external:write`, `ai:external:read` |
| `AiGovernanceEndpointModule.cs` | 32 | ✅ Todos protegidos | `ai:governance:*` |
| `AiIdeEndpointModule.cs` | 5 | ✅ Todos protegidos | `ai:ide:*` |
| `AiOrchestrationEndpointModule.cs` | 6 | ✅ Todos protegidos | `ai:orchestration:*` |
| `AiRuntimeEndpointModule.cs` | 13 | ✅ Todos protegidos | `ai:runtime:*` |

### AuditCompliance (1 ficheiro — 6 endpoints)

| Ficheiro | Endpoints | Status | Permissões Usadas |
|---|---|---|---|
| `AuditEndpointModule.cs` | 6 | ✅ Todos protegidos | `audit:events:write`, `audit:trail:read` |

### Catalog (5 ficheiros — 77 endpoints)

| Ficheiro | Endpoints | Status | Permissões Usadas |
|---|---|---|---|
| `ContractStudioEndpointModule.cs` | 11 | ✅ Todos protegidos | `contracts:studio:*` |
| `ContractsEndpointModule.cs` | 24 | ✅ Todos protegidos | `contracts:read`, `contracts:write` |
| `ServiceCatalogEndpointModule.cs` | 22 | ✅ Todos protegidos | `catalog:read`, `catalog:write` |
| `DeveloperPortalEndpointModule.cs` | 15 | ✅ Todos protegidos | `portal:read`, `portal:write` |
| `SourceOfTruthEndpointModule.cs` | 5 | ✅ Todos protegidos | `catalog:read` |

### ChangeGovernance (10 ficheiros — 50+ endpoints)

#### CORRIGIDOS nesta fase (Bloco D)

Os ficheiros abaixo tinham endpoints sem `RequirePermission`. A falha estava na arquitectura interna:  
o `ChangeIntelligenceEndpointModule.cs` e `WorkflowEndpointModule.cs` delegavam para sub-ficheiros  
estáticos (AnalysisEndpoints, DeploymentEndpoints, etc.) que não tinham a using directive de segurança.

| Ficheiro | Endpoints | Status Anterior | Status Atual | Permissões Atribuídas |
|---|---|---|---|---|
| `AnalysisEndpoints.cs` | 6 | 🚫 EXPOSTO | ✅ CORRIGIDO | `change-intelligence:write/read` |
| `DeploymentEndpoints.cs` | 3 | 🚫 EXPOSTO | ✅ CORRIGIDO | `change-intelligence:write` |
| `FreezeEndpoints.cs` | 2 | 🚫 EXPOSTO | ✅ CORRIGIDO | `change-intelligence:write/read` |
| `IntelligenceEndpoints.cs` | 6 | 🚫 EXPOSTO | ✅ CORRIGIDO | `change-intelligence:write/read` |
| `ReleaseQueryEndpoints.cs` | 3 | 🚫 EXPOSTO | ✅ CORRIGIDO | `change-intelligence:read` |
| `ApprovalEndpoints.cs` | 5 | 🚫 EXPOSTO | ✅ CORRIGIDO | `workflow:instances:write` |
| `StatusEndpoints.cs` | 3 | 🚫 EXPOSTO | ✅ CORRIGIDO | `workflow:instances:read/write` |
| `EvidencePackEndpoints.cs` | 3 | 🚫 EXPOSTO | ✅ CORRIGIDO | `workflow:instances:write/read` |
| `TemplateEndpoints.cs` | 1 | 🚫 EXPOSTO | ✅ CORRIGIDO | `workflow:templates:write` |

#### Já protegidos

| Ficheiro | Endpoints | Status | Permissões Usadas |
|---|---|---|---|
| `ChangeConfidenceEndpoints.cs` | 9 | ✅ Protegido | `change-intelligence:read/write` |
| `PromotionEndpointModule.cs` | 9 | ✅ Protegido | `promotion:environments:write`, `promotion:requests:*`, `promotion:gates:override` |
| `RulesetGovernanceEndpointModule.cs` | 9 | ✅ Protegido | `rulesets:read/write/execute` |
| `WorkflowEndpointModule.cs` | — (orquestrador) | ✅ Protegido (via sub-ficheiros) | — |

### Governance (19 ficheiros — 61 endpoints)

| Ficheiro | Endpoints | Status |
|---|---|---|
| `ComplianceChecksEndpointModule.cs` | 2 | ✅ Todos protegidos |
| `DelegatedAdminEndpointModule.cs` | 2 | ✅ Todos protegidos |
| `DomainEndpointModule.cs` | 6 | ✅ Todos protegidos |
| `EnterpriseControlsEndpointModule.cs` | 1 | ✅ Todos protegidos |
| `EvidencePackagesEndpointModule.cs` | 2 | ✅ Todos protegidos |
| `ExecutiveOverviewEndpointModule.cs` | 6 | ✅ Todos protegidos |
| `GovernanceComplianceEndpointModule.cs` | 1 | ✅ Todos protegidos |
| `GovernanceFinOpsEndpointModule.cs` | 7 | ✅ Todos protegidos |
| `GovernancePacksEndpointModule.cs` | 9 | ✅ Todos protegidos |
| `GovernanceReportsEndpointModule.cs` | 1 | ✅ Todos protegidos |
| `GovernanceRiskEndpointModule.cs` | 1 | ✅ Todos protegidos |
| `GovernanceWaiversEndpointModule.cs` | 4 | ✅ Todos protegidos |
| `IntegrationHubEndpointModule.cs` | 8 | ✅ Todos protegidos |
| `OnboardingEndpointModule.cs` | 3 | ✅ Todos protegidos |
| `PlatformStatusEndpointModule.cs` | 6 | ✅ Todos protegidos |
| `PolicyCatalogEndpointModule.cs` | 2 | ✅ Todos protegidos |
| `ProductAnalyticsEndpointModule.cs` | 7 | ✅ Todos protegidos |
| `ScopedContextEndpointModule.cs` | 1 | ✅ Todos protegidos |
| `TeamEndpointModule.cs` | 6 | ✅ Todos protegidos |

### IdentityAccess (11 ficheiros — 40 endpoints)

| Ficheiro | Endpoints | Status |
|---|---|---|
| `AccessReviewEndpoints.cs` | 4 | ✅ Todos protegidos |
| `AuthEndpoints.cs` | 9 | ✅ 4 AllowAnonymous (legítimo), 5 RequireAuthorization |
| `BreakGlassEndpoints.cs` | 3 | ✅ Todos protegidos |
| `CookieSessionEndpoints.cs` | 3 | ✅ 2 AllowAnonymous (legítimo), 1 RequireAuthorization |
| `DelegationEndpoints.cs` | 3 | ✅ Todos protegidos |
| `EnvironmentEndpoints.cs` | 6 | ✅ Todos protegidos |
| `JitAccessEndpoints.cs` | 3 | ✅ Todos protegidos |
| `RolePermissionEndpoints.cs` | 2 | ✅ Todos protegidos |
| `RuntimeContextEndpoints.cs` | 1 | ✅ Todos protegidos |
| `TenantEndpoints.cs` | 2 | ✅ Todos protegidos |
| `UserEndpoints.cs` | 7 | ✅ Todos protegidos |

### OperationalIntelligence (7 ficheiros — 57 endpoints)

| Ficheiro | Endpoints | Status |
|---|---|---|
| `AutomationEndpointModule.cs` | 15 | ✅ Todos protegidos |
| `CostIntelligenceEndpointModule.cs` | 8 | ✅ Todos protegidos |
| `IncidentEndpointModule.cs` | 10 | ✅ Todos protegidos |
| `MitigationEndpointModule.cs` | 7 | ✅ Todos protegidos |
| `RunbookEndpointModule.cs` | 2 | ✅ Todos protegidos |
| `ReliabilityEndpointModule.cs` | 7 | ✅ Todos protegidos |
| `RuntimeIntelligenceEndpointModule.cs` | 8 | ✅ Todos protegidos |

---

## Permissões Introduzidas Nesta Fase

| Permissão | Âmbito | Endpoints Abrangidos |
|---|---|---|
| `change-intelligence:read` | Leitura de releases, scores, blast radius, histórico | 9 endpoints |
| `change-intelligence:write` | Criação de releases, análise, markers, baseline, freeze | 23 endpoints |
| `workflow:instances:read` | Consulta de status e pendências de workflow | 3 endpoints |
| `workflow:instances:write` | Criação, aprovação, rejeição, escalação | 9 endpoints |
| `workflow:templates:write` | Criação de templates de workflow | 1 endpoint |

---

## Política de Fallback Global

O NexTraceOne **não** configura uma fallback policy global que requeira autenticação por defeito.  
Cada endpoint é responsável por declarar explicitamente a sua política.  
Endpoints sem política explícita ficam acessíveis (comportamento padrão ASP.NET Core).

**Esta abordagem exige que cada endpoint seja auditado individualmente**, o que foi feito neste documento.  
A auditoria deve ser repetida a cada adição de novos endpoints.

---

## Recomendação para Fase 2

Considerar configurar uma fallback authorization policy global que requeira autenticação:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

Isso garantiria que qualquer novo endpoint sem política explícita fosse automaticamente protegido,  
seguindo o princípio de "deny by default, allow explicitly".

Endpoints públicos teriam de declarar explicitamente `.AllowAnonymous()`.

---

## Referências

- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Extensions/EndpointAuthorizationExtensions.cs`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authorization/PermissionPolicyProvider.cs`
- [PHASE-1-SECRETS-BASELINE.md](PHASE-1-SECRETS-BASELINE.md)
- [PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md](PHASE-1-PRODUCTION-BASELINE-CHECKLIST.md)
