# Phase 1, Block C — Rate Limiting & API Protection

> **Status:** Complete  
> **Risk Treated:** Authentication endpoints vulnerable to brute-force and credential-stuffing attacks

---

## Problem

While a global rate limiter existed (100 req/min per IP), authentication endpoints
had no dedicated throttling. This left login, token refresh, and federated auth
flows exposed to automated abuse.

## Pre-existing Global Policy

| Parameter | Value |
|-----------|-------|
| Limit | 100 requests/minute per IP |
| Fallback (unresolved IP) | 20 requests/minute |
| Scope | All endpoints |

## New Named Policies

### `auth` Policy

Applied to standard authentication endpoints that are publicly accessible.

| Parameter | Value |
|-----------|-------|
| Limit | 20 requests/minute per IP |
| Applied to | Login, token refresh, federated auth, OIDC callback |
| Response on exceed | HTTP 429 Too Many Requests |

### `auth-sensitive` Policy

Applied to endpoints that initiate authentication flows or create sessions.

| Parameter | Value |
|-----------|-------|
| Limit | 10 requests/minute per IP |
| Applied to | OIDC start, cookie session creation |
| Response on exceed | HTTP 429 Too Many Requests |

## Endpoint Coverage

### AuthEndpoints.cs

All anonymous authentication endpoints annotated with the appropriate rate-limiting
policy:

| Endpoint | Policy |
|----------|--------|
| POST /auth/login | `auth` |
| POST /auth/refresh | `auth` |
| POST /auth/federated | `auth` |
| GET /auth/oidc/callback | `auth` |
| GET /auth/oidc/start | `auth-sensitive` |

### CookieSessionEndpoints.cs

| Endpoint | Policy |
|----------|--------|
| POST /sessions/cookie | `auth-sensitive` |

## Design Decisions

1. **Per-IP partitioning** — Rate limits are keyed by client IP to prevent
   one abusive client from affecting others.
2. **Layered policies** — The global policy acts as a safety net; named policies
   provide tighter control on sensitive surfaces.
3. **No rate limiting on authenticated business endpoints** — Authenticated users
   are already identity-bound and subject to permission checks. Adding rate limits
   there would add complexity without meaningful security benefit at this stage.
4. **HTTP 429 response** — Standard response code with `Retry-After` header
   when available.

## Verification

- Manual testing confirms HTTP 429 is returned after exceeding threshold
- Existing integration tests unaffected (test traffic stays well within limits)
