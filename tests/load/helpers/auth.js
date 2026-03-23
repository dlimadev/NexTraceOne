import http from 'k6/http';
import { getEnvironment } from '../config/environments.js';

/**
 * Authenticates against the NexTraceOne API and returns a bearer token.
 * Credentials are read from environment variables or defaults.
 */
export function login() {
  const env = getEnvironment();
  const payload = JSON.stringify({
    username: env.username,
    password: env.password,
    tenantId: env.tenantId,
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'login' },
  };

  const res = http.post(`${env.baseUrl}/api/v1/identity/auth/login`, payload, params);

  if (res.status === 200) {
    try {
      const body = res.json();
      return body.token || body.accessToken || '';
    } catch (_) {
      return '';
    }
  }
  return '';
}

/**
 * Returns HTTP params with Authorization header.
 */
export function authHeaders(token) {
  return {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  };
}

/**
 * Refreshes an existing token.
 */
export function refreshToken(token) {
  const env = getEnvironment();
  const payload = JSON.stringify({ token });
  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'refresh' },
  };

  const res = http.post(`${env.baseUrl}/api/v1/identity/auth/refresh`, payload, params);

  if (res.status === 200) {
    try {
      const body = res.json();
      return body.token || body.accessToken || token;
    } catch (_) {
      return token;
    }
  }
  return token;
}
