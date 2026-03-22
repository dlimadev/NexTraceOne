# 08 — Security Audit

**Date:** 2026-03-22

---

## Authentication

### Implementation

| Feature | Status | Evidence |
|---------|--------|----------|
| JWT Authentication | ✅ Implemented | `BuildingBlocks.Security/Authentication/` — JWT bearer scheme |
| Cookie Session | ✅ Implemented | `CookieSessionEndpoints.cs` — cookie-based auth with CSRF |
| MFA (Multi-Factor Auth) | ✅ Implemented | `MfaPage.tsx`, MFA endpoints in IdentityAccess |
| Account Activation | ✅ Implemented | `ActivationPage.tsx`, activation endpoints |
| Password Reset | ✅ Implemented | `ForgotPasswordPage.tsx`, `ResetPasswordPage.tsx` |
| Invitation Flow | ✅ Implemented | `InvitationPage.tsx`, invitation endpoints |
| Tenant Selection | ✅ Implemented | `TenantSelectionPage.tsx`, multi-tenant auth flow |
| Session Management | ✅ Implemented | `MySessionsPage.tsx`, session tracking |
| OIDC/Federated | ⚠️ Partial | Endpoints exist in `AuthEndpoints.cs` (AllowAnonymous), but no provider configuration |

### Token Storage (Frontend)

| Token | Storage | Risk Level | Justification |
|-------|---------|-----------|---------------|
| Access Token | sessionStorage | Low | Tab-scoped, cleared on browser close |
| Refresh Token | HTTP-only cookie | Very Low | Not accessible to JS |
| Tenant ID | sessionStorage | Low | Re-hydration only |
| User ID | sessionStorage | Low | Fallback for profile |
| Environment ID | sessionStorage | Low | UI preference |

**Assessment:** ✅ Token storage strategy follows security best practices. Detailed comments in `tokenStorage.ts` explain the rationale.

---

## Authorization

### Permission Model

- Fine-grained permission strings (e.g., `catalog:services:read`, `governance:packs:write`, `audit:trail:read`)
- `RequirePermission()` extension method on endpoint builders
- `ProtectedRoute` wrapper on frontend routes with permission checks
- Role-based with permission assignment

### Coverage

| Metric | Value |
|--------|-------|
| Total `RequirePermission` usages | 391 |
| Endpoints with `AllowAnonymous` | ~18 (health, auth, cookie login/logout) |
| Unprotected business endpoints | 0 (verified) |

### Frontend Authorization

- `ProtectedRoute` wraps all protected routes in `App.tsx`
- `usePermissions` hook for component-level permission checks
- Sidebar items filtered by permission
- `PermissionsContext` in AuthContext

**Assessment:** ✅ Authorization is comprehensive and consistently applied.

---

## Advanced Access Controls

| Feature | Status | Evidence |
|---------|--------|----------|
| Break Glass | ✅ Implemented | `BreakGlassPage.tsx`, `BreakGlassEndpoints.cs`, expiration handler |
| JIT (Just-in-Time) Access | ✅ Implemented | `JitAccessPage.tsx`, `JitAccessEndpoints.cs`, expiration handler |
| Delegation | ✅ Implemented | `DelegationPage.tsx`, `DelegationEndpoints.cs`, expiration handler |
| Access Reviews | ✅ Implemented | `AccessReviewPage.tsx`, `AccessReviewEndpoints.cs`, expiration handler |
| Environment-scoped Access | ✅ Implemented | `EnvironmentAccessRequirement.cs`, environment-aware authorization |
| Delegated Administration | ✅ Implemented | `DelegatedAdminPage.tsx`, `DelegatedAdminEndpointModule.cs` |

**Assessment:** ✅ Enterprise-grade access control. Background jobs handle expiration. All advanced access features have backend + frontend + expiration handling.

---

## Credential & Secret Management

### Configuration Security

| Check | Status | Evidence |
|-------|--------|----------|
| No credentials in `appsettings.json` | ✅ | Empty `Password=;` in connection strings |
| Dev credentials in `appsettings.Development.json` | ✅ | Appropriate for local development only |
| JWT secret validation | ✅ | `StartupValidation.cs` — minimum 32 chars, hard fail in production |
| Connection string validation | ✅ | Startup validation for non-dev environments |
| `.env.example` present | ✅ | Template without actual secrets |
| No secrets in Docker compose | ✅ | `docker-compose.override.yml` for dev overrides |

### Findings

| # | Finding | Severity | Evidence | Status |
|---|---------|----------|----------|--------|
| SEC-01 | Development JWT secret in `appsettings.Development.json` | Info | Expected for local dev | ✅ Acceptable |
| SEC-02 | Empty passwords in base `appsettings.json` | Info | Forces injection in non-dev environments | ✅ Good pattern |
| SEC-03 | No hardcoded credentials found in .cs source | ✅ | grep verified | ✅ Clean |
| SEC-04 | No credentials in frontend code | ✅ | grep verified | ✅ Clean |

---

## Security Headers

### ApiHost (verified in Program.cs)

| Header | Status |
|--------|--------|
| Content-Security-Policy | ✅ Configured |
| X-Frame-Options | ✅ DENY |
| X-Content-Type-Options | ✅ nosniff |
| X-XSS-Protection | ✅ 1; mode=block |
| Strict-Transport-Security (HSTS) | ✅ Non-dev only |
| Referrer-Policy | ✅ Configured |
| Permissions-Policy | ✅ Configured |

### Ingestion API

| Header | Status |
|--------|--------|
| Same headers as ApiHost | ✅ Verified |
| HSTS in non-dev | ✅ |

### Frontend (nginx)

| Header | Status |
|--------|--------|
| X-Frame-Options | ✅ DENY |
| X-Content-Type-Options | ✅ nosniff |
| X-XSS-Protection | ✅ |
| Cache-Control for static assets | ✅ 1y immutable |

**Assessment:** ✅ Comprehensive security headers across all entry points.

---

## CORS Configuration

- CORS configured in ApiHost `Program.cs`
- Development: permissive for localhost origins
- Production: should be restricted to known frontend domains
- **Recommendation:** Verify CORS policy for production deployment is correctly restrictive

---

## Input Validation

| Layer | Approach | Status |
|-------|----------|--------|
| Backend | FluentValidation in MediatR pipeline | ✅ All commands/queries validated |
| Frontend | Form validation in components | ✅ Consistent validation patterns |
| API | Model binding + validation behavior | ✅ |

---

## Potential Vulnerabilities

| # | Vulnerability | Severity | Evidence | Recommendation |
|---|--------------|----------|----------|---------------|
| SEC-05 | No rate limiting on business API endpoints | Medium | No rate-limit middleware in ApiHost | Add rate limiting for auth endpoints at minimum |
| SEC-06 | OIDC/federated auth endpoints are AllowAnonymous but non-functional | Low | `AuthEndpoints.cs` — endpoints exist but no provider | Either complete or remove to reduce attack surface |
| SEC-07 | Swagger/OpenAPI exposed in development | Info | `Program.cs` — development-only | ✅ Acceptable (env-gated) |
| SEC-08 | AI TenantId inconsistency could allow cross-tenant data access | Medium | `string` vs `Guid` TenantId in AIKnowledge entities | Standardize and verify global filter coverage |
| SEC-09 | Outbox processor processes only one context — events may leak | Low | `OutboxProcessorJob.cs` | Extend to all contexts |

---

## Security Testing Coverage

| Area | Tests | Status |
|------|-------|--------|
| `BuildingBlocks.Security.Tests` | 0 | ❌ No security component tests |
| Auth flow integration tests | Present in IntegrationTests | ⚠️ Limited |
| Permission tests | `usePermissions.test.tsx` (frontend) | ⚠️ Single file |
| Token storage tests | `tokenStorage.test.tsx`, `tokenStorageEnvironment.test.tsx` | ✅ |

**Critical Gap:** `BuildingBlocks.Security.Tests` project exists but has 0 tests. This means JWT validation logic, permission requirements, cookie session handling, encryption utilities, and multi-tenancy middleware have no unit test coverage.

---

## Security Summary

| Category | Rating | Notes |
|----------|--------|-------|
| Authentication | ✅ Strong | JWT + Cookie + MFA + advanced access controls |
| Authorization | ✅ Strong | 391 permission checks, consistent across all modules |
| Credential Management | ✅ Strong | No hardcoded secrets, startup validation, env-based injection |
| Security Headers | ✅ Strong | Comprehensive across all entry points |
| Input Validation | ✅ Strong | FluentValidation pipeline, frontend validation |
| Token Storage | ✅ Strong | sessionStorage with documented rationale |
| Rate Limiting | ⚠️ Missing | No rate limiting on API endpoints |
| Security Tests | ❌ Poor | 0 tests in security building blocks |
| Overall | **B+** | Strong foundation, needs rate limiting and test coverage |
