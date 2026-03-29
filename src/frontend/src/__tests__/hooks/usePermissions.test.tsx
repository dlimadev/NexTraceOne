import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { usePermissions } from '../../hooks/usePermissions';
import type { CurrentUserProfile } from '../../types';

// Mock do AuthContext
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}));

import { useAuth } from '../../contexts/AuthContext';

function mockUser(permissions: string[], roleName = 'Developer'): CurrentUserProfile {
  return {
    id: 'user-1',
    email: 'test@acme.com',
    firstName: 'Test',
    lastName: 'User',
    fullName: 'Test User',
    isActive: true,
    lastLoginAt: null,
    tenantId: 'tenant-1',
    roleName,
    permissions,
  };
}

function mockAuthValue(permissions: string[], roleName = 'Developer') {
  return {
    isAuthenticated: true,
    accessToken: 'token',
    user: mockUser(permissions, roleName),
    tenantId: 'tenant-1',
    requiresTenantSelection: false,
    availableTenants: [],
    login: vi.fn(),
    selectTenant: vi.fn(),
    logout: vi.fn(),
  };
}

describe('usePermissions', () => {
  it('PlatformAdmin pode acessar identity:users:read e identity:users:write', () => {
    vi.mocked(useAuth).mockReturnValue(
      mockAuthValue(['identity:users:read', 'identity:users:write', 'audit:trail:read', 'workflow:instances:read'], 'PlatformAdmin'),
    );

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('identity:users:read')).toBe(true);
    expect(result.current.can('identity:users:write')).toBe(true);
    expect(result.current.can('audit:trail:read')).toBe(true);
    expect(result.current.can('workflow:instances:read')).toBe(true);
  });

  it('Developer não pode acessar identity:users:write', () => {
    vi.mocked(useAuth).mockReturnValue(
      mockAuthValue(['contracts:read', 'contracts:write', 'change-intelligence:read']),
    );

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('identity:users:write')).toBe(false);
    expect(result.current.can('contracts:read')).toBe(true);
    expect(result.current.can('contracts:write')).toBe(true);
  });

  it('Viewer não pode escrever em nenhum módulo', () => {
    vi.mocked(useAuth).mockReturnValue(
      mockAuthValue(['contracts:read', 'change-intelligence:read', 'catalog:assets:read'], 'Viewer'),
    );

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('contracts:write')).toBe(false);
    expect(result.current.can('identity:users:write')).toBe(false);
    expect(result.current.can('contracts:read')).toBe(true);
  });

  it('retorna roleName do usuário corretamente', () => {
    vi.mocked(useAuth).mockReturnValue(
      mockAuthValue(['identity:users:read'], 'PlatformAdmin'),
    );

    const { result } = renderHook(() => usePermissions());
    expect(result.current.roleName).toBe('PlatformAdmin');
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
    expect(result.current.can('contracts:read')).toBe(false);
    expect(result.current.roleName).toBe('');
  });
});
