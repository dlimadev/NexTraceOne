import client from './client';
import type {
  LoginRequest,
  LoginResponse,
  CurrentUserProfile,
  UserProfile,
  TenantUser,
  RoleInfo,
  PermissionInfo,
  ActiveSession,
  PagedList,
} from '../types';

/**
 * Cliente de API do módulo Identity.
 * Cobre autenticação, gestão de usuários, papéis, permissões e sessões.
 */
export const identityApi = {
  // ── Autenticação ─────────────────────────────────────────────
  login: (data: LoginRequest) =>
    client.post<LoginResponse>('/identity/auth/login', data).then((r) => r.data),

  refresh: (refreshToken: string) =>
    client
      .post<LoginResponse>('/identity/auth/refresh', { refreshToken })
      .then((r) => r.data),

  logout: () =>
    client.post('/identity/auth/logout'),

  getCurrentUser: () =>
    client.get<CurrentUserProfile>('/identity/auth/me').then((r) => r.data),

  changePassword: (currentPassword: string, newPassword: string) =>
    client.put('/identity/auth/password', { currentPassword, newPassword }),

  // ── Sessões ──────────────────────────────────────────────────
  revoke: (sessionId: string) =>
    client.post('/identity/auth/revoke', { sessionId }),

  listActiveSessions: (userId: string) =>
    client.get<ActiveSession[]>(`/identity/users/${userId}/sessions`).then((r) => r.data),

  // ── Gestão de Usuários ───────────────────────────────────────
  getUserProfile: (id: string) =>
    client.get<UserProfile>(`/identity/users/${id}`).then((r) => r.data),

  listTenantUsers: (tenantId: string, page = 1, pageSize = 20, search?: string) =>
    client
      .get<PagedList<TenantUser>>(`/identity/tenants/${tenantId}/users`, {
        params: { page, pageSize, search },
      })
      .then((r) => r.data),

  createUser: (data: { email: string; firstName: string; lastName: string; tenantId: string }) =>
    client.post<{ id: string }>('/identity/users', data).then((r) => r.data),

  assignRole: (userId: string, tenantId: string, roleId: string) =>
    client.post(`/identity/users/${userId}/roles`, { tenantId, roleId }),

  deactivateUser: (userId: string) =>
    client.put(`/identity/users/${userId}/deactivate`),

  activateUser: (userId: string) =>
    client.put(`/identity/users/${userId}/activate`),

  // ── Papéis e Permissões ──────────────────────────────────────
  listRoles: () =>
    client.get<RoleInfo[]>('/identity/roles').then((r) => r.data),

  listPermissions: () =>
    client.get<PermissionInfo[]>('/identity/permissions').then((r) => r.data),
};
