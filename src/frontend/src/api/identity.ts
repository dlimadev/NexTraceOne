import client from './client';
import type {
  LoginRequest,
  LoginResponse,
  UserProfile,
  TenantUser,
  PagedList,
} from '../types';

export const identityApi = {
  login: (data: LoginRequest) =>
    client.post<LoginResponse>('/identity/auth/login', data).then((r) => r.data),

  refresh: (refreshToken: string) =>
    client
      .post<LoginResponse>('/identity/auth/refresh', { refreshToken })
      .then((r) => r.data),

  revoke: (sessionId: string) =>
    client.post('/identity/auth/revoke', { sessionId }),

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
};
