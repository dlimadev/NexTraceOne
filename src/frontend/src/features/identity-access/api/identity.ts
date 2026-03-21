import axios from 'axios';
import client from '../../../api/client';
import type {
  LoginRequest,
  LoginResponse,
  CookieSessionLoginResponse,
  AuthLoginResponse,
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
} from '../../../types';

/** Ambiente retornado pela API de gestão de ambientes. */
export interface EnvironmentItem {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
  isActive: boolean;
  profile: string;
  criticality?: string;
  isProductionLike: boolean;
  isPrimaryProduction: boolean;
  code?: string;
  region?: string;
  description?: string;
}

/** Payload para criação de ambiente. */
export interface CreateEnvironmentRequest {
  name: string;
  slug: string;
  sortOrder: number;
  profile: string;
  criticality: string;
  code?: string;
  description?: string;
  region?: string;
  isProductionLike?: boolean;
  isPrimaryProduction: boolean;
}

/** Payload para atualização de ambiente. */
export interface UpdateEnvironmentRequest {
  environmentId?: string;
  name: string;
  sortOrder: number;
  profile: string;
  criticality: string;
  code?: string;
  description?: string;
  region?: string;
  isProductionLike?: boolean;
}

/**
 * Cliente de API do módulo Identity.
 * Cobre autenticação, gestão de usuários, papéis, permissões, sessões,
 * seleção de tenant e funcionalidades enterprise (Break Glass, JIT Access, Delegação).
 */
export const identityApi = {
  // ── Autenticação ─────────────────────────────────────────────
  login: async (data: LoginRequest): Promise<AuthLoginResponse> => {
    try {
      const response = await client.post<CookieSessionLoginResponse>('/identity/auth/cookie-session', data);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return client.post<LoginResponse>('/identity/auth/login', data).then((r) => r.data);
      }

      throw error;
    }
  },

  refresh: (refreshToken: string) =>
    client
      .post<LoginResponse>('/identity/auth/refresh', { refreshToken })
      .then((r) => r.data),

  logout: async () => {
    try {
      return await client.delete('/identity/auth/cookie-session');
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return client.post('/identity/auth/logout');
      }

      throw error;
    }
  },

  getCurrentUser: () =>
    client.get<CurrentUserProfile>('/identity/auth/me').then((r) => r.data),

  getCsrfToken: async () => {
    try {
      return await client
        .get<{ csrfToken: string }>('/identity/auth/cookie-session/csrf-token')
        .then((r) => r.data);
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  changePassword: (currentPassword: string, newPassword: string) =>
    client.put('/identity/auth/password', { currentPassword, newPassword }),

  // ── Password Recovery & Account Activation ─────────────────────
  forgotPassword: (email: string) =>
    client.post('/identity/auth/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string) =>
    client.post('/identity/auth/reset-password', { token, newPassword }),

  activateAccount: (token: string, password: string) =>
    client.post('/identity/auth/activate', { token, password }),

  // ── MFA ────────────────────────────────────────────────────────
  verifyMfa: (code: string, sessionId: string) =>
    client
      .post<{ accessToken: string; refreshToken: string }>('/identity/auth/mfa/verify', { code, sessionId })
      .then((r) => r.data),

  resendMfaCode: (sessionId: string) =>
    client.post('/identity/auth/mfa/resend', { sessionId }),

  // ── Invitation ────────────────────────────────────────────────
  getInvitationDetails: (token: string) =>
    client
      .get<{ email: string; organizationName: string; roleName: string; expiresAt: string }>(
        `/identity/invitations/${encodeURIComponent(token)}`,
      )
      .then((r) => r.data),

  acceptInvitation: (token: string, password: string) =>
    client.post('/identity/invitations/accept', { token, password }),

  // ── OIDC / SSO ────────────────────────────────────────────────
  startOidcLogin: (provider = 'default', returnTo?: string) =>
    client
      .post<{ authorizationUrl: string }>('/identity/auth/oidc/start', { provider, returnTo })
      .then((r) => r.data),

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

  createUser: (data: { email: string; firstName: string; lastName: string; tenantId: string; roleId: string }) =>
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

  // ── Environments ─────────────────────────────────────────────
  listEnvironments: () =>
    client.get<EnvironmentItem[]>('/identity/environments').then((r) => r.data),

  getPrimaryProductionEnvironment: () =>
    client.get<EnvironmentItem | null>('/identity/environments/primary-production').then((r) => r.data),

  createEnvironment: (data: CreateEnvironmentRequest) =>
    client.post<{ environmentId: string; name: string; slug: string }>('/identity/environments', data).then((r) => r.data),

  updateEnvironment: (environmentId: string, data: UpdateEnvironmentRequest) =>
    client.put<{ environmentId: string; name: string; slug: string; profile: string; isProductionLike: boolean }>(
      `/identity/environments/${encodeURIComponent(environmentId)}`, data).then((r) => r.data),

  setPrimaryProductionEnvironment: (environmentId: string) =>
    client.patch(`/identity/environments/${encodeURIComponent(environmentId)}/primary-production`).then((r) => r.data),
};
