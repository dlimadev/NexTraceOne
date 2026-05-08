# NexTraceOne — Production Action Plan

**Companion to:** `PRODUCTION-READINESS-REPORT.md`  
**Date:** 2026-05-07  
**Branch:** `claude/analyze-solution-gaps-8Xuj2`

---

## Sprint 1 — P0: Security & Critical Feature Gaps (Week 1)

### Task 1.1 — ActivateAccount Token Infrastructure

**Status:** ✅ IMPLEMENTADO (Maio 2026) — Confirmado por auditoria de código  
**Files to create/modify:**

```
src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/AccountActivationToken.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/IdentityAccessDbContext.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ActivateAccount/ActivateAccount.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/IAccountActivationTokenRepository.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Repositories/AccountActivationTokenRepository.cs
```

**Implementation steps:**
1. Create `AccountActivationToken` entity:
   ```csharp
   public sealed class AccountActivationToken
   {
       public Guid Id { get; private set; }
       public Guid UserId { get; private set; }
       public string TokenHash { get; private set; }   // SHA-256 of raw token
       public DateTime ExpiresAt { get; private set; }
       public DateTime? UsedAt { get; private set; }
       public DateTime CreatedAt { get; private set; }
   }
   ```
2. Add `DbSet<AccountActivationToken>` to `IdentityAccessDbContext`
3. Run `dotnet ef migrations add AddAccountActivationTokens --context IdentityAccessDbContext`
4. Add `IAccountActivationTokenRepository` port
5. Implement `AccountActivationTokenRepository` (EF Core)
6. Update `ActivateAccount.Handler`:
   - Find token by hash (SHA-256 of incoming raw token)
   - Check `ExpiresAt > DateTime.UtcNow` and `UsedAt == null`
   - Set `UsedAt = DateTime.UtcNow`
   - Activate user account (`user.Activate()`)
   - Commit via UoW
7. Create `RequestAccountActivation` command that generates the token and triggers outbox email

**Tests to write:**
- `ActivateAccountTests.cs` — valid token, expired token, already-used token, user not found
- `RequestAccountActivationTests.cs` — generates token, sends email event

---

### Task 1.2 — ResetPassword Token Infrastructure

**Status:** ✅ IMPLEMENTADO (Maio 2026) — Confirmado por auditoria de código  
**Files to create/modify:**

```
src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/PasswordResetToken.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Repositories/PasswordResetTokenRepository.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Abstractions/IPasswordResetTokenRepository.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/ResetPassword/ResetPassword.cs
src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/RequestPasswordReset/RequestPasswordReset.cs  ← may already exist
```

**Implementation steps:**
1. Same token entity pattern as activation token
2. `ResetPassword.Handler`:
   - Verify token hash + expiry
   - Hash new password with BCrypt (rounds ≥ 12)
   - Set `user.PasswordHash = newHash`
   - Mark token as used
   - Optionally: revoke all existing refresh tokens for user
3. `RequestPasswordReset.Handler`:
   - Look up user by email
   - Delete any existing unexpired reset tokens for user
   - Generate `PasswordResetToken` (store hash, return raw to email only)
   - Publish outbox event → Notifications module

**Tests to write:**
- `ResetPasswordTests.cs` — valid, expired, replayed, wrong user
- `RequestPasswordResetTests.cs` — unknown email (should not reveal existence), duplicate request

---

## Sprint 2 — P1: Configuration & Deployment Readiness (Week 1–2)

### Task 2.1 — Startup Configuration Validation

**File:** `src/platform/NexTraceOne.ApiHost/Program.cs`

Add validation so the app fails fast at startup if required secrets are missing:

```csharp
// After builder.Services configuration, before builder.Build():
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .Validate(o => !string.IsNullOrEmpty(o.Secret) && o.Secret != "REPLACE_VIA_ENV",
              "Jwt:Secret must be set via environment variable")
    .ValidateOnStart();

builder.Services.AddOptions<ConnectionStringsOptions>()
    .BindConfiguration("ConnectionStrings")
    .Validate(o => !o.Default.Contains("REPLACE_VIA_ENV"),
              "ConnectionStrings:Default must be set via environment variable")
    .ValidateOnStart();
```

### Task 2.2 — Production Secrets Checklist

The following values must be provided by DevOps via environment variables or secrets manager before deploying to production. None should ever be committed to source control.

```
# PostgreSQL (all 26 contexts — can use same credentials with different DB names)
ConnectionStrings__Default="Host=...;Port=5432;Database=nextraceone;Username=nextraceone;Password=<STRONG>"

# JWT (minimum 256 bits / 32 bytes, base64 encoded)
Jwt__Secret="<BASE64_RANDOM_256BIT>"
Jwt__Issuer="https://api.yourdomain.com"
Jwt__Audience="https://app.yourdomain.com"

# AI
AI__OpenAI__ApiKey="sk-..."

# Observability
Telemetry__ObservabilityProvider__Provider="clickhouse"  # or "elasticsearch"
Telemetry__ClickHouse__ConnectionString="Host=...;Port=8123;..."
# OR
Elastic__Uri="https://..."
Elastic__ApiKey="..."

# OIDC (if using Azure SSO)
Authentication__Azure__ClientId="<GUID>"
Authentication__Azure__ClientSecret="<SECRET>"

# CORS
Cors__AllowedOrigins__0="https://app.yourdomain.com"

# OpenTelemetry
OpenTelemetry__OtlpGrpcEndpoint="http://otel-collector:4317"
```

### Task 2.3 — CORS Configuration

**File:** `src/platform/NexTraceOne.ApiHost/appsettings.json`

The `AllowedOrigins` array is empty in production appsettings. Override via environment in Kubernetes/Docker:
```yaml
# kubernetes deployment.yaml
env:
  - name: Cors__AllowedOrigins__0
    value: "https://app.yourdomain.com"
```

---

## Sprint 3 — P2: Contract Pipeline Enhancement (Week 2–3)

### Task 3.1 — Contract Pipeline: Use Database as Source of Truth

Currently `GenerateServerFromContract`, `GenerateMockServer`, `GenerateContractTests`, `GeneratePostmanCollection`, and `GenerateClientSdkFromContract` accept `ContractJson` as a raw string in the command rather than loading it from the database.

**Pattern to apply across all five features:**

```csharp
// Step 1: Remove ContractJson from Commands that have ContractVersionId
// Before:
public sealed record Command(Guid ContractVersionId, string ContractJson, ...) : ICommand<Response>;

// After:
public sealed record Command(Guid ContractVersionId, ...) : ICommand<Response>;

// Step 2: Inject repository into Handler
public sealed class Handler(IContractVersionRepository contractVersionRepository) : ICommandHandler<Command, Response>
{
    public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
    {
        var spec = await contractVersionRepository.GetSpecificationAsync(request.ContractVersionId, ct);
        if (spec is null)
            return Result<Response>.Failure(Error.NotFound("contract.version.not_found", "Contract version not found"));

        // use spec.OpenApiJson instead of request.ContractJson
    }
}
```

**Status por feature (auditoria Maio 2026):**
- `GeneratePostmanCollection` — ❌ ainda usa `ContractJson` no Command
- `GenerateMockServer` — ❌ ainda usa `ContractJson` no Command
- `GenerateContractTests` — ❌ ainda usa `ContractJson` no Command
- `GenerateServerFromContract` — ✅ já carrega spec da DB
- `GenerateClientSdkFromContract` — ✅ já carrega spec da DB

**Files to modify (apenas os 3 pendentes):**
- `GeneratePostmanCollection/GeneratePostmanCollection.cs`
- `GenerateMockServer/GenerateMockServer.cs`
- `GenerateContractTests/GenerateContractTests.cs`

**API contracts to update:**
- Remove `contractJson` from HTTP request bodies for pipeline endpoints
- Endpoint becomes: `POST /api/v1/catalog/pipeline/{contractVersionId}/postman-collection`

---

## Sprint 4 — P3: Quality & Housekeeping (Week 3–4)

### Task 4.1 — Selenium Tests: Add Assertions

**Status:** ✅ DIAGNÓSTICO REVISTO — Os testes Selenium têm asserções reais.

**Nota de revisão (Maio 2026):** Os testes em `tests/platform/NexTraceOne.Selenium.Tests/Modules/` (não em `tests/modules/catalog/`) usam `AssertPageLoadsSuccessfully()` definido em `SeleniumTestBase`, que verifica:
- `AssertNoErrorBoundary()` — ausência de React error boundary
- `AssertNotUnauthorized()` — sem redirect para página de auth
- `AssertNoJavaScriptErrors()` — sem erros JS críticos

Nenhuma acção necessária.

### Task 4.2 — SyncModelSnapshot Migration Cleanup

**Files:**
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Migrations/20260410202600_SyncModelSnapshot.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Migrations/20260410202934_SyncModelSnapshot.cs`

Both have empty `Up()` and `Down()` methods. These are harmless no-ops but should be:
1. Removed if they were placeholders (and a proper initial migration added)
2. Or documented as intentional no-ops if the model was already in sync

### Task 4.3 — Multi-Context Migration Runbook

**File to create:** `docs/runbooks/database-migrations.md`

Document the EF Core commands for each sub-context:

```bash
# ChangeGovernance - 4 sub-contexts
dotnet ef migrations add <MigrationName> \
  --project src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context WorkflowDbContext \
  --output-dir ChangeGovernance/Workflow/Persistence/Migrations

# Repeat for PromotionDbContext, ChangeIntelligenceDbContext, RulesetGovernanceDbContext
```

### Task 4.4 — Add knowledge:read / knowledge:write to Role Definitions

**Status:** ✅ IMPLEMENTADO (Maio 2026) — `RolePermissionCatalog.cs` confirma `knowledge:read` e `knowledge:write` em todos os papéis relevantes.

After adding `RequirePermission` to Knowledge endpoints, ensure the default roles include the new permissions:

**Files to check/update:**
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Seed/` (role seed data)
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermission.cs`

Add to relevant roles:
- `admin` → `knowledge:read`, `knowledge:write`
- `developer` → `knowledge:read`, `knowledge:write`
- `viewer` → `knowledge:read`

---

---

## Sprint 5 — Gaps Identificados na Auditoria de Maio 2026

> Novos items identificados durante validação da documentação (Maio 2026). Não bloqueiam v1.0.0 mas devem ser endereçados.

### Task 5.1 — Startup Configuration Validation (GAP-M02)

**Prioridade:** 🔴 Alta — app pode arrancar em produção sem secrets obrigatórios  
**Ficheiro:** `src/platform/NexTraceOne.ApiHost/Program.cs`

Adicionar após `builder.Services` e antes de `builder.Build()`:

```csharp
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .Validate(o => !string.IsNullOrEmpty(o.Secret) && o.Secret != "REPLACE_VIA_ENV",
              "Jwt:Secret must be set via environment variable — cannot be empty or placeholder")
    .ValidateOnStart();

builder.Services.AddOptions<ConnectionStringsOptions>()
    .BindConfiguration("ConnectionStrings")
    .Validate(o => !o.Default.Contains("REPLACE_VIA_ENV"),
              "ConnectionStrings:Default must be set via environment variable")
    .ValidateOnStart();
```

**Esforço:** 2–4h  
**Critério de aceite:** App falha imediatamente no arranque se JWT_SECRET ou ConnectionString for placeholder.

---

### Task 5.2 — GetDashboardAnnotations: Ligar a Dados Reais (GAP-M01)

**Prioridade:** 🟡 Média  
**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDashboardAnnotations/GetDashboardAnnotations.cs`  
**Plano detalhado:** [PLAN-02-CORE-COMPLETIONS.md CC-09](./docs/plans/PLAN-02-CORE-COMPLETIONS.md)

Handler retorna 4 anotações hardcoded para serviços fictícios com `IsSimulated:true`. Ligar a `IIncidentModule`, `IChangeIntelligenceModule` e `IRulesetGovernanceModule`.

**Esforço:** 4–8h

---

### Task 5.3 — IIdentityNotifier: Ligar a Email Real (GAP-M06)

**Prioridade:** 🟡 Média  
**Ficheiro:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/NullIdentityNotifier.cs`

`NullIdentityNotifier` emite `LogWarning` em vez de enviar email real. Tokens de activação/reset são gerados mas não chegam ao utilizador em produção.

**Implementação:**
1. Criar `NotificationsIdentityNotifier` que injeta `INotificationModule`
2. Registar condicionalmente: quando SMTP configurado → `NotificationsIdentityNotifier`; caso contrário → `NullIdentityNotifier`
3. Adicionar evento de integração `AccountActivationTokenRequested` no outbox

**Esforço:** 1–2 dias

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation | Estado |
|------|-----------|--------|-----------|--------|
| Production deploy without JWT_SECRET set | Medium | Critical (auth bypass) | Startup validation (Task 5.1 / GAP-M02) | 🔴 Aberto |
| Account activation tokens never reach users in prod | High | High (onboarding blocked) | NullIdentityNotifier → ligar email real (Task 5.3 / GAP-M06) | 🔴 Aberto |
| Knowledge data exposed | Resolved | High | RequirePermission aplicado (Task 1.1) | ✅ Resolvido |
| Contract Pipeline generates artifacts from stale JSON | Medium | Medium | 3 features pendentes (Task 3.1 / GAP-M03) | 🟡 Parcial |
| CORS blocks frontend in production | High (if not configured) | High | Configurar via env vars (Task 2.3) | ⚙️ Config/Ops |
| Telemetry silent in production | High (if not configured) | Medium | Configurar OTLP endpoint (Task 2.2) | ⚙️ Config/Ops |
| Dashboard shows fake annotations to users | Low | Low | GetDashboardAnnotations fix (Task 5.2 / GAP-M01) | 🟡 Aberto |

---

## Effort Summary

| Sprint | Items | Estimated Effort | Estado |
|--------|-------|-----------------|--------|
| Sprint 1 (P0) | ActivateAccount + ResetPassword tokens | 3–5 days | ✅ Concluído |
| Sprint 2 (P1) | Startup validation + secrets + CORS | 1–2 days (mostly config) | 🔴 Startup validation pendente |
| Sprint 3 (P2) | Contract Pipeline DB integration (3 features) | 2–3 days | 🔴 Pendente |
| Sprint 4 (P3) | Housekeeping (migrations runbook, SyncModelSnapshot) | 1–2 days | 🟡 Runbook criado; migrations pendentes |
| Sprint 5 (Maio 2026) | Startup validation, Dashboard Annotations, Email notifier | 3–5 days | 🔴 Novos items |
| **Total restante** | | **~6–10 days** | |

---

## Definition of Done

The system is **fully production-ready** when:
- [x] ActivateAccount sends email with token and validates correctly *(token infra implementada; email via IIdentityNotifier pendente — GAP-M06)*
- [x] ResetPassword sends email with token, validates, and updates password hash *(token infra implementada; email via IIdentityNotifier pendente — GAP-M06)*
- [ ] All production environment variables are set and validated at startup *(ValidateOnStart não implementado — GAP-M02)*
- [ ] CORS is configured for production domains *(ops — configurar via env vars)*
- [ ] Telemetry collector endpoint is reachable *(ops — configurar OTLP endpoint)*
- [ ] Contract Pipeline features load specs from database *(3 de 5 features pendentes — GAP-M03)*
- [x] `knowledge:read` / `knowledge:write` permissions exist in role seed data *(confirmado em RolePermissionCatalog.cs)*
- [ ] Integration tests pass against production-equivalent database *(verificar em CI)*
- [x] Selenium tests have assertions *(AssertPageLoadsSuccessfully() confirmado em SeleniumTestBase)*
