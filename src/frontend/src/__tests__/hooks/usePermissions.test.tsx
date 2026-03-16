import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { usePermissions } from '../../hooks/usePermissions';
import type { CurrentUserProfile } from '../../types';
import { getPermissionsForRoles } from '../../auth/permissions';

// Mock do AuthContext
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}));

import { useAuth } from '../../contexts/AuthContext';

function mockUser(roles: string[]): CurrentUserProfile {
  const permissions = [...getPermissionsForRoles(roles)];
  return {
    id: 'user-1',
    email: 'test@acme.com',
    firstName: 'Test',
    lastName: 'User',
    fullName: 'Test User',
    isActive: true,
    lastLoginAt: null,
    tenantId: 'tenant-1',
    roleName: roles[0] || '',
    permissions,
  };
}

function mockAuthValue(roles: string[]) {
  return {
    isAuthenticated: true,
    accessToken: 'token',
    user: mockUser(roles),
    tenantId: 'tenant-1',
    requiresTenantSelection: false,
    availableTenants: [],
    login: vi.fn(),
    selectTenant: vi.fn(),
    logout: vi.fn(),
  };
}

describe('usePermissions', () => {
  it('Admin pode acessar users:read e users:write', () => {
    vi.mocked(useAuth).mockReturnValue(mockAuthValue(['Admin']));

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('users:read')).toBe(true);
    expect(result.current.can('users:write')).toBe(true);
    expect(result.current.can('audit:read')).toBe(true);
    expect(result.current.can('workflow:approve')).toBe(true);
  });

  it('Developer não pode acessar users:read', () => {
    vi.mocked(useAuth).mockReturnValue(mockAuthValue(['Developer']));

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('users:read')).toBe(false);
    expect(result.current.can('releases:read')).toBe(true);
    expect(result.current.can('releases:write')).toBe(true);
  });

  it('Viewer não pode escrever em nenhum módulo', () => {
    vi.mocked(useAuth).mockReturnValue(mockAuthValue(['Viewer']));

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('releases:write')).toBe(false);
    expect(result.current.can('contracts:write')).toBe(false);
    expect(result.current.can('users:write')).toBe(false);
    expect(result.current.can('releases:read')).toBe(true);
  });

  it('retorna roleName do usuário corretamente', () => {
    vi.mocked(useAuth).mockReturnValue(mockAuthValue(['Admin']));

    const { result } = renderHook(() => usePermissions());
    expect(result.current.roleName).toBe('Admin');
  });

  it('usuário sem perfil carregado não tem permissões', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: null,
      tenantId: 'tenant-1',
      requiresTenantSelection: false,
      availableTenants: [],
      login: vi.fn(),
      selectTenant: vi.fn(),
      logout: vi.fn(),
    });

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('releases:read')).toBe(false);
    expect(result.current.roleName).toBe('');
  });
});
