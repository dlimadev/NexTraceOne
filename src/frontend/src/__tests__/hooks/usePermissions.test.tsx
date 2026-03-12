import { describe, it, expect, vi } from 'vitest';
import { renderHook } from '@testing-library/react';
import { usePermissions } from '../../hooks/usePermissions';
import type { UserProfile } from '../../types';

// Mock do AuthContext
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}));

import { useAuth } from '../../contexts/AuthContext';

function mockUser(roles: string[]): UserProfile {
  return {
    id: 'user-1',
    email: 'test@acme.com',
    fullName: 'Test User',
    roles,
    tenantId: 'tenant-1',
  };
}

describe('usePermissions', () => {
  it('Admin pode acessar users:read e users:write', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: mockUser(['Admin']),
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('users:read')).toBe(true);
    expect(result.current.can('users:write')).toBe(true);
    expect(result.current.can('audit:read')).toBe(true);
    expect(result.current.can('workflow:approve')).toBe(true);
  });

  it('Developer não pode acessar users:read', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: mockUser(['Developer']),
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('users:read')).toBe(false);
    expect(result.current.can('releases:read')).toBe(true);
    expect(result.current.can('releases:write')).toBe(true);
  });

  it('Viewer não pode escrever em nenhum módulo', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: mockUser(['Viewer']),
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('releases:write')).toBe(false);
    expect(result.current.can('contracts:write')).toBe(false);
    expect(result.current.can('users:write')).toBe(false);
    expect(result.current.can('releases:read')).toBe(true);
  });

  it('retorna roles do usuário corretamente', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: mockUser(['Admin', 'Auditor']),
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    const { result } = renderHook(() => usePermissions());
    expect(result.current.roles).toEqual(['Admin', 'Auditor']);
  });

  it('usuário sem perfil carregado não tem permissões', () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      accessToken: 'token',
      user: null,
      tenantId: 'tenant-1',
      login: vi.fn(),
      logout: vi.fn(),
    });

    const { result } = renderHook(() => usePermissions());
    expect(result.current.can('releases:read')).toBe(false);
    expect(result.current.roles).toEqual([]);
  });
});
