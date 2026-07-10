/**
 * Handlers MSW de autenticação e arranque de sessão.
 *
 * Cobrem tudo o que o AuthContext / EnvironmentContext / PersonaContext
 * disparam no boot, para que a app arranque autenticada em modo stub.
 */
import { http, HttpResponse } from 'msw';
import {
  stubCurrentUser,
  stubCookieSessionLogin,
  stubBearerLogin,
  stubTenants,
  stubEnvironments,
  stubPersonaConfig,
} from '../fixtures/auth';

const API = '/api/v1';

export const authHandlers = [
  // ── Login ────────────────────────────────────────────────────────
  http.post(`${API}/identity/auth/cookie-session`, () =>
    HttpResponse.json(stubCookieSessionLogin),
  ),
  http.post(`${API}/identity/auth/login`, () =>
    HttpResponse.json(stubBearerLogin),
  ),

  // ── Sessão / arranque ────────────────────────────────────────────
  http.get(`${API}/identity/auth/me`, () =>
    HttpResponse.json(stubCurrentUser),
  ),
  http.post(`${API}/identity/auth/refresh`, () =>
    HttpResponse.json({ accessToken: 'stub-access-token', refreshToken: 'stub-refresh-token' }),
  ),
  http.get(`${API}/identity/auth/cookie-session/csrf-token`, () =>
    HttpResponse.json({ csrfToken: 'stub-csrf-token' }),
  ),
  http.post(`${API}/identity/auth/logout`, () =>
    new HttpResponse(null, { status: 204 }),
  ),

  // ── Tenants / persona / ambientes ────────────────────────────────
  http.get(`${API}/identity/tenants/mine`, () =>
    HttpResponse.json(stubTenants),
  ),
  http.get(`${API}/identity/me/persona-config`, () =>
    HttpResponse.json(stubPersonaConfig),
  ),
  http.get(`${API}/identity/environments`, () =>
    HttpResponse.json(stubEnvironments),
  ),
  http.get(`${API}/identity/environments/primary-production`, () =>
    HttpResponse.json(stubEnvironments.find((e) => e.isPrimaryProduction) ?? null),
  ),
];
