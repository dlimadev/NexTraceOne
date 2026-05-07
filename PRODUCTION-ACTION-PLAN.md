# NexTraceOne — Production Action Plan

**Companion to:** `PRODUCTION-READINESS-REPORT.md`  
**Date:** 2026-05-07  
**Branch:** `claude/analyze-solution-gaps-8Xuj2`

---

## Sprint 1 — P0: Security & Critical Feature Gaps (Week 1)

### Task 1.1 — ActivateAccount Token Infrastructure

**Status:** Handler returns hardcoded error — feature non-functional  
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

**Status:** Handler returns hardcoded error — feature non-functional  
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

**Files to modify:**
- `GeneratePostmanCollection/GeneratePostmanCollection.cs`
- `GenerateMockServer/GenerateMockServer.cs`
- `GenerateContractTests/GenerateContractTests.cs`
- `GenerateServerFromContract/GenerateServerFromContract.cs`
- `GenerateClientSdkFromContract/GenerateClientSdkFromContract.cs` (already lacks ContractJson; verify it loads spec)

**API contracts to update:**
- Remove `contractJson` from HTTP request bodies for pipeline endpoints
- Endpoint becomes: `POST /api/v1/catalog/pipeline/{contractVersionId}/postman-collection`

---

## Sprint 4 — P3: Quality & Housekeeping (Week 3–4)

### Task 4.1 — Selenium Tests: Add Assertions

**File:** `tests/modules/catalog/NexTraceOne.Catalog.Tests/AdminNavigationTests.cs`

30 test methods navigate to pages but make no assertions. Either:
- Add `Assert.True(driver.Url.Contains("/admin/..."), "Expected navigation to admin page")` and element visibility checks
- Or mark tests with `[Skip("Navigation scaffold — pending UI assertion implementation")]`

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

After adding `RequirePermission` to Knowledge endpoints, ensure the default roles include the new permissions:

**Files to check/update:**
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Persistence/Seed/` (role seed data)
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/RolePermission.cs`

Add to relevant roles:
- `admin` → `knowledge:read`, `knowledge:write`
- `developer` → `knowledge:read`, `knowledge:write`
- `viewer` → `knowledge:read`

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Production deploy without JWT_SECRET set | Medium | Critical (auth bypass) | Startup validation (Task 2.1) |
| Account activation never works in prod | High | High (user onboarding blocked) | Task 1.1 (P0) |
| Knowledge data exposed before fix deployed | Low (already fixed) | High | Fix already applied in this session |
| Contract Pipeline generates wrong artifacts | Medium | Medium | Document limitation; add preview note |
| CORS blocks frontend in production | High (if not configured) | High | Task 2.3 (P1) |
| Telemetry silent in production | High (if not configured) | Medium | Task 2.2 (P1) |

---

## Effort Summary

| Sprint | Items | Estimated Effort |
|--------|-------|-----------------|
| Sprint 1 (P0) | ActivateAccount + ResetPassword tokens | 3–5 days |
| Sprint 2 (P1) | Startup validation + secrets + CORS | 1–2 days (mostly config) |
| Sprint 3 (P2) | Contract Pipeline DB integration | 3–5 days |
| Sprint 4 (P3) | Housekeeping | 2–3 days |
| **Total** | | **~10–15 days** |

---

## Definition of Done

The system is **fully production-ready** when:
- [ ] ActivateAccount sends email with token and validates correctly
- [ ] ResetPassword sends email with token, validates, and updates password hash
- [ ] All production environment variables are set and validated at startup
- [ ] CORS is configured for production domains
- [ ] Telemetry collector endpoint is reachable
- [ ] Contract Pipeline features load specs from database
- [ ] `knowledge:read` / `knowledge:write` permissions exist in role seed data
- [ ] Integration tests pass against production-equivalent database
- [ ] Selenium tests have assertions or are clearly marked as skipped
