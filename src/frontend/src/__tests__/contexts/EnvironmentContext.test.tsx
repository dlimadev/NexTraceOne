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

// vi.mock factory is hoisted — use vi.hoisted() to define mocks used inside it
const { mockGet } = vi.hoisted(() => ({ mockGet: vi.fn() }));

// Mock the API client — EnvironmentProvider now calls the real API
vi.mock('../../api/client', () => ({
  default: {
    get: mockGet,
    post: vi.fn(),
    patch: vi.fn(),
  },
}));

const mockEnvironments = [
  { id: 'env-prod-001', name: 'Production', slug: 'prod', sortOrder: 0, isActive: true, profile: 'production', isProductionLike: true },
  { id: 'env-staging-001', name: 'Staging', slug: 'staging', sortOrder: 1, isActive: true, profile: 'staging', isProductionLike: true },
  { id: 'env-qa-001', name: 'QA', slug: 'qa', sortOrder: 2, isActive: true, profile: 'qa', isProductionLike: false, isDefault: true },
  { id: 'env-dev-001', name: 'Development', slug: 'dev', sortOrder: 3, isActive: true, profile: 'development', isProductionLike: false },
];

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
    // Default mock: return environments successfully
    mockGet.mockResolvedValue({ data: mockEnvironments });
  });

  it('should load environments from API when tenant is authenticated', async () => {
    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      const count = parseInt(screen.getByTestId('env-count').textContent ?? '0');
      expect(count).toBeGreaterThan(0);
    });

    expect(mockGet).toHaveBeenCalledWith('/identity/environments');
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

    // Should not call API when not authenticated
    expect(mockGet).not.toHaveBeenCalled();
  });

  it('should auto-select default environment (isDefault=true)', async () => {
    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      // The QA environment has isDefault: true and sortOrder: 2
      const activeEnv = screen.getByTestId('active-env').textContent;
      expect(activeEnv).toBe('QA');
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

  it('should clear environments on logout (isAuthenticated false)', async () => {
    const { rerender } = render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(parseInt(screen.getByTestId('env-count').textContent ?? '0')).toBeGreaterThan(0);
    });

    // Simulate logout: isAuthenticated = false
    rerender(
      <Wrapper tenantId={null} isAuthenticated={false}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(screen.getByTestId('env-count').textContent).toBe('0');
    });
  });

  it('should gracefully handle API errors and leave environments empty', async () => {
    mockGet.mockRejectedValueOnce(new Error('Network error'));

    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(screen.getByTestId('env-count').textContent).toBe('0');
      expect(screen.getByTestId('active-env').textContent).toBe('none');
    });
  });

  it('should infer production profile when backend does not return profile field', async () => {
    // Simulate backend without profile fields (pre-migration state)
    mockGet.mockResolvedValueOnce({
      data: [
        { id: 'env-1', name: 'Production', slug: 'prod', sortOrder: 0, isActive: true },
        { id: 'env-2', name: 'QA', slug: 'qa', sortOrder: 1, isActive: true },
      ],
    });

    render(
      <Wrapper tenantId="tenant-123" isAuthenticated={true}>
        <TestConsumer />
      </Wrapper>
    );

    await waitFor(() => {
      expect(parseInt(screen.getByTestId('env-count').textContent ?? '0')).toBe(2);
    });
  });
});
