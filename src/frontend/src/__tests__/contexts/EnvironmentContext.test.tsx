import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { EnvironmentProvider, useEnvironment } from '../../contexts/EnvironmentContext';
import { AuthContext } from '../../contexts/AuthContext';
import type { ReactNode } from 'react';

// Mock tokenStorage
vi.mock('../../utils/tokenStorage', () => ({
  storeEnvironmentId: vi.fn(),
  getEnvironmentId: vi.fn(() => null),
  clearEnvironmentId: vi.fn(),
  storeTokens: vi.fn(),
  getAccessToken: vi.fn(() => null),
  getTenantId: vi.fn(() => null),
  storeTenantId: vi.fn(),
  storeUserId: vi.fn(),
  getUserId: vi.fn(() => null),
  clearAllTokens: vi.fn(),
  migrateFromLocalStorage: vi.fn(),
  hasActiveSession: vi.fn(() => false),
  getCsrfToken: vi.fn(() => null),
  storeCsrfToken: vi.fn(),
}));

// Helper: mock AuthContext value
function createAuthContextValue(tenantId: string | null, isAuthenticated: boolean) {
  return {
    isAuthenticated,
    isLoadingUser: false,
    accessToken: isAuthenticated ? 'mock-token' : null,
    user: isAuthenticated ? {
      id: 'user-1', email: 'test@test.com', fullName: 'Test User',
      tenantId: tenantId ?? 'none', roleName: 'Developer', permissions: [],
    } : null,
    tenantId,
    requiresTenantSelection: false,
    availableTenants: [],
    login: vi.fn(),
    selectTenant: vi.fn(),
    logout: vi.fn(),
  };
}

function TestConsumer() {
  const { activeEnvironment, availableEnvironments, selectEnvironment } = useEnvironment();
  return (
    <div>
      <span data-testid="active-env">{activeEnvironment?.name ?? 'none'}</span>
      <span data-testid="env-count">{availableEnvironments.length}</span>
      <button
        onClick={() => availableEnvironments[0] && selectEnvironment(availableEnvironments[0].id)}
        data-testid="select-first"
      >
        Select First
      </button>
    </div>
  );
}

function Wrapper({ tenantId, isAuthenticated, children }: { tenantId: string | null; isAuthenticated: boolean; children: ReactNode }) {
  return (
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    <AuthContext.Provider value={createAuthContextValue(tenantId, isAuthenticated) as any}>
      <EnvironmentProvider>
        {children}
      </EnvironmentProvider>
    </AuthContext.Provider>
  );
}

describe('EnvironmentContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should load environments when tenant is authenticated', async () => {
    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      const count = parseInt(screen.getByTestId('env-count').textContent ?? '0');
      expect(count).toBeGreaterThan(0);
    });
  });

  it('should have no environments when not authenticated', async () => {
    render(
      <Wrapper tenantId={null} isAuthenticated={false}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(screen.getByTestId('env-count').textContent).toBe('0');
      expect(screen.getByTestId('active-env').textContent).toBe('none');
    });
  });

  it('should auto-select default environment', async () => {
    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      const activeEnv = screen.getByTestId('active-env').textContent;
      expect(activeEnv).not.toBe('none');
    });
  });

  it('should allow environment selection', async () => {
    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(parseInt(screen.getByTestId('env-count').textContent ?? '0')).toBeGreaterThan(0);
    });

    await act(async () => {
      await userEvent.click(screen.getByTestId('select-first'));
    });

    await waitFor(() => {
      expect(screen.getByTestId('active-env').textContent).not.toBe('none');
    });
  });

  it('should clear environments on logout (tenantId null)', async () => {
    const { rerender } = render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(parseInt(screen.getByTestId('env-count').textContent ?? '0')).toBeGreaterThan(0);
    });

    // Simulate logout: tenantId = null, isAuthenticated = false
    rerender(
      <Wrapper tenantId={null} isAuthenticated={false}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(screen.getByTestId('env-count').textContent).toBe('0');
    });
  });
});
