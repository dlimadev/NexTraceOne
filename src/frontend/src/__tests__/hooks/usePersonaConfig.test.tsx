import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';

vi.mock('../../features/identity-access/api/identity', () => ({
  identityApi: {
    getPersonaConfig: vi.fn(),
  },
}));

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn(),
}));

import { identityApi } from '../../features/identity-access/api/identity';
import { usePersona } from '../../contexts/PersonaContext';
import { usePersonaConfig } from '../../features/identity-access/hooks/usePersonaConfig';

const mockPersonaContext = (persona: string) => ({
  persona,
  config: {
    sectionOrder: ['services', 'operations', 'changes'],
    highlightedSections: ['services'],
    homeSubtitleKey: `persona.${persona}.homeSubtitle`,
    homeWidgets: [],
    quickActions: [
      { id: 'qa-1', labelKey: `persona.${persona}.actions.first`, icon: 'Search', to: '/services' },
    ],
    aiContextScopes: ['services'],
    aiSuggestedPromptKeys: [],
  },
});

function createWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  );
}

describe('usePersonaConfig', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(usePersona).mockReturnValue(mockPersonaContext('Engineer') as ReturnType<typeof usePersona>);
  });

  it('returns server persona config when API call succeeds', async () => {
    const serverConfig = {
      persona: 'Engineer',
      quickActions: [{ id: 'qa-server', labelKey: 'persona.Engineer.actions.server', icon: 'Server', to: '/services' }],
      prioritizedModules: ['services', 'operations'],
    };
    vi.mocked(identityApi.getPersonaConfig).mockResolvedValue(serverConfig);

    const { result } = renderHook(() => usePersonaConfig(), { wrapper: createWrapper() });
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.personaConfig?.persona).toBe('Engineer');
    expect(result.current.personaConfig?.quickActions[0].id).toBe('qa-server');
    expect(result.current.isError).toBe(false);
  });

  it('falls back to local PersonaContext config when API fails', async () => {
    vi.mocked(identityApi.getPersonaConfig).mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => usePersonaConfig(), { wrapper: createWrapper() });
    await waitFor(() => expect(result.current.personaConfig).not.toBeNull());

    expect(result.current.personaConfig?.persona).toBe('Engineer');
    expect(result.current.personaConfig?.quickActions[0].id).toBe('qa-1');
    expect(result.current.isError).toBe(true);
  });

  it('returns correct prioritized modules from local fallback', async () => {
    vi.mocked(identityApi.getPersonaConfig).mockRejectedValue(new Error('fail'));

    const { result } = renderHook(() => usePersonaConfig(), { wrapper: createWrapper() });
    await waitFor(() => expect(result.current.personaConfig).not.toBeNull());

    expect(result.current.personaConfig?.prioritizedModules).toEqual(['services', 'operations', 'changes']);
  });

  it('shows loading state initially while query is pending', () => {
    vi.mocked(identityApi.getPersonaConfig).mockReturnValue(new Promise(() => {}));

    const { result } = renderHook(() => usePersonaConfig(), { wrapper: createWrapper() });
    expect(result.current.isLoading).toBe(true);
    expect(result.current.personaConfig).toBeNull();
  });

  it('uses Executive persona context when usePersona returns Executive', async () => {
    vi.mocked(usePersona).mockReturnValue(mockPersonaContext('Executive') as ReturnType<typeof usePersona>);
    vi.mocked(identityApi.getPersonaConfig).mockRejectedValue(new Error('fail'));

    const { result } = renderHook(() => usePersonaConfig(), { wrapper: createWrapper() });
    await waitFor(() => expect(result.current.personaConfig).not.toBeNull());

    expect(result.current.personaConfig?.persona).toBe('Executive');
  });
});
