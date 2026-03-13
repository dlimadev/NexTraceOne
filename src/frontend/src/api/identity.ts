import client from './client';
import type {
  LoginRequest,
  LoginResponse,
  CurrentUserProfile,
  UserProfile,
  TenantUser,
  TenantInfo,
  SelectTenantResponse,
  RoleInfo,
  PermissionInfo,
  ActiveSession,
  PagedList,
  BreakGlassRequest,
  BreakGlassActivationResponse,
  JitAccessRequest,
  JitAccessCreatedResponse,
  DelegationInfo,
  DelegationCreatedResponse,
  AccessReviewCampaign,
  AccessReviewCampaignDetail,
} from '../types';

/**
 * Cliente de API do módulo Identity.
 * Cobre autenticação, gestão de usuários, papéis, permissões, sessões,
 * seleção de tenant e funcionalidades enterprise (Break Glass, JIT Access, Delegação).
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

  // ── Tenants ──────────────────────────────────────────────────
  listMyTenants: () =>
    client.get<TenantInfo[]>('/identity/tenants/mine').then((r) => r.data),

  selectTenant: (tenantId: string) =>
    client
      .post<SelectTenantResponse>('/identity/auth/select-tenant', { tenantId })
      .then((r) => r.data),

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

  // ── Enterprise: Break Glass ──────────────────────────────────
  requestBreakGlass: (justification: string) =>
    client
      .post<BreakGlassActivationResponse>('/identity/break-glass', { justification })
      .then((r) => r.data),

  revokeBreakGlass: (requestId: string) =>
    client.post(`/identity/break-glass/${requestId}/revoke`),

  listBreakGlassRequests: () =>
    client.get<BreakGlassRequest[]>('/identity/break-glass').then((r) => r.data),

  // ── Enterprise: JIT Access ───────────────────────────────────
  requestJitAccess: (permissionCode: string, scope: string, justification: string) =>
    client
      .post<JitAccessCreatedResponse>('/identity/jit-access', { permissionCode, scope, justification })
      .then((r) => r.data),

  decideJitAccess: (requestId: string, approve: boolean, rejectionReason?: string) =>
    client.post(`/identity/jit-access/${requestId}/decide`, { approve, rejectionReason }),

  listPendingJitRequests: () =>
    client.get<JitAccessRequest[]>('/identity/jit-access/pending').then((r) => r.data),

  // ── Enterprise: Delegação ────────────────────────────────────
  createDelegation: (data: {
    delegateeId: string;
    permissions: string[];
    reason: string;
    validFrom: string;
    validUntil: string;
  }) =>
    client.post<DelegationCreatedResponse>('/identity/delegations', data).then((r) => r.data),

  revokeDelegation: (delegationId: string) =>
    client.post(`/identity/delegations/${delegationId}/revoke`),

  listDelegations: () =>
    client.get<DelegationInfo[]>('/identity/delegations').then((r) => r.data),

  // ── Enterprise: Access Review ─────────────────────────────────
  listAccessReviewCampaigns: () =>
    client.get<AccessReviewCampaign[]>('/identity/access-reviews').then((r) => r.data),

  startAccessReviewCampaign: (data: { name: string; scope: string; reviewerIds: string[] }) =>
    client.post<{ campaignId: string }>('/identity/access-reviews', data).then((r) => r.data),

  getAccessReviewCampaign: (campaignId: string) =>
    client.get<AccessReviewCampaignDetail>(`/identity/access-reviews/${encodeURIComponent(campaignId)}`).then((r) => r.data),

  decideAccessReviewItem: (campaignId: string, itemId: string, approve: boolean, comment?: string) =>
    client.post(`/identity/access-reviews/${encodeURIComponent(campaignId)}/items/${encodeURIComponent(itemId)}/decide`, { approve, comment }),
};
