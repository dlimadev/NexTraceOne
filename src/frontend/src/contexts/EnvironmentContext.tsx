/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  useMemo,
  type ReactNode,
} from 'react';
import { useAuth } from './AuthContext';
import { storeEnvironmentId, getEnvironmentId, clearEnvironmentId } from '../utils/tokenStorage';

// ── Types ───────────────────────────────────────────────────────────────────

/**
 * Perfil do ambiente — determina a UX contextual sem depender de nomes fixos.
 * Mapeado a partir de dados retornados pelo backend.
 */
export type EnvironmentProfile =
  | 'production'
  | 'staging'
  | 'uat'
  | 'qa'
  | 'development'
  | 'sandbox'
  | 'unknown';

/** Descrição de um ambiente disponível para o tenant ativo. */
export interface EnvironmentOption {
  id: string;
  name: string;
  profile: EnvironmentProfile;
  isProductionLike: boolean;
  isDefault?: boolean;
}

interface EnvironmentState {
  /** ID do ambiente atualmente ativo. Null se não selecionado. */
  activeEnvironmentId: string | null;
  /** Dados do ambiente ativo. Null se não selecionado. */
  activeEnvironment: EnvironmentOption | null;
  /** Lista de ambientes disponíveis para o tenant. */
  availableEnvironments: EnvironmentOption[];
  /** Indica se os ambientes estão a ser carregados. */
  isLoadingEnvironments: boolean;
}

interface EnvironmentContextValue extends EnvironmentState {
  /** Seleciona um ambiente ativo. Persiste em sessionStorage. */
  selectEnvironment: (environmentId: string) => void;
  /** Limpa o ambiente ativo (ex: ao trocar de tenant). */
  clearEnvironment: () => void;
}

// ── Context ─────────────────────────────────────────────────────────────────

export const EnvironmentContext = createContext<EnvironmentContextValue | null>(null);

// ── Mock environments loader ─────────────────────────────────────────────────
// TODO Phase 7: Replace with real API call to GET /api/v1/identity/environments?tenantId=X
// The backend already has EnvironmentResolutionMiddleware and X-Environment-Id header support
// from Phase 2. This mock respects the contract: N environments per tenant, no hardcoded enum.
function loadEnvironmentsForTenant(_tenantId: string | null): EnvironmentOption[] {
  if (!_tenantId) return [];
  // Returns a realistic multi-environment set — not 3 hardcoded envs.
  // Real data will come from the backend in Phase 7.
  return [
    {
      id: `${_tenantId}-prod`,
      name: 'Production',
      profile: 'production',
      isProductionLike: true,
      isDefault: false,
    },
    {
      id: `${_tenantId}-staging`,
      name: 'Staging',
      profile: 'staging',
      isProductionLike: true,
    },
    {
      id: `${_tenantId}-uat`,
      name: 'UAT',
      profile: 'uat',
      isProductionLike: false,
    },
    {
      id: `${_tenantId}-qa`,
      name: 'QA',
      profile: 'qa',
      isProductionLike: false,
      isDefault: true,
    },
    {
      id: `${_tenantId}-dev`,
      name: 'Development',
      profile: 'development',
      isProductionLike: false,
    },
  ];
}

// ── Provider ─────────────────────────────────────────────────────────────────

/**
 * Provider do contexto de ambiente ativo.
 *
 * Responsabilidades:
 * - Carregar ambientes disponíveis para o tenant ativo
 * - Manter o ambiente ativo com persistência em sessionStorage
 * - Reagir à troca de tenant (limpar ambiente ao trocar de tenant)
 * - Expor o perfil do ambiente para que a UI adapte a experiência sem hardcode
 *
 * REGRA DE SEGURANÇA: O contexto de ambiente no frontend serve apenas para
 * materializar a experiência. Autorização real e isolamento ocorrem no backend.
 * O header X-Environment-Id é injetado automaticamente pelo API client.
 *
 * @see src/api/client.ts para injeção do X-Environment-Id header
 * @see docs/architecture/phase-6/ para detalhes da arquitetura
 */
export function EnvironmentProvider({ children }: { children: ReactNode }) {
  const { tenantId, isAuthenticated } = useAuth();

  const [availableEnvironments, setAvailableEnvironments] = useState<EnvironmentOption[]>([]);
  const [activeEnvironmentId, setActiveEnvironmentId] = useState<string | null>(null);
  const [isLoadingEnvironments, setIsLoadingEnvironments] = useState(false);

  // Load environments when tenant changes
  useEffect(() => {
    if (!isAuthenticated || !tenantId) {
      setAvailableEnvironments([]);
      setActiveEnvironmentId(null);
      clearEnvironmentId();
      return;
    }

    setIsLoadingEnvironments(true);

    // TODO Phase 7: Replace with real API call
    const environments = loadEnvironmentsForTenant(tenantId);
    setAvailableEnvironments(environments);

    // Restore persisted environment or select the default one
    const persisted = getEnvironmentId();
    const persistedEnv = persisted ? environments.find(e => e.id === persisted) : null;
    const defaultEnv = environments.find(e => e.isDefault) ?? environments[0];

    const resolved = persistedEnv ?? defaultEnv ?? null;
    if (resolved) {
      setActiveEnvironmentId(resolved.id);
      storeEnvironmentId(resolved.id);
    }

    setIsLoadingEnvironments(false);
  }, [tenantId, isAuthenticated]);

  const selectEnvironment = useCallback((environmentId: string) => {
    setActiveEnvironmentId(environmentId);
    storeEnvironmentId(environmentId);
  }, []);

  const clearEnvironment = useCallback(() => {
    setActiveEnvironmentId(null);
    clearEnvironmentId();
  }, []);

  const activeEnvironment = useMemo(
    () => availableEnvironments.find(e => e.id === activeEnvironmentId) ?? null,
    [availableEnvironments, activeEnvironmentId],
  );

  const value = useMemo<EnvironmentContextValue>(
    () => ({
      activeEnvironmentId,
      activeEnvironment,
      availableEnvironments,
      isLoadingEnvironments,
      selectEnvironment,
      clearEnvironment,
    }),
    [activeEnvironmentId, activeEnvironment, availableEnvironments, isLoadingEnvironments, selectEnvironment, clearEnvironment],
  );

  return (
    <EnvironmentContext.Provider value={value}>
      {children}
    </EnvironmentContext.Provider>
  );
}

// ── Hooks ────────────────────────────────────────────────────────────────────

/**
 * Hook principal para acesso ao contexto de ambiente.
 * Lança erro se usado fora de EnvironmentProvider.
 */
export function useEnvironment(): EnvironmentContextValue {
  const ctx = useContext(EnvironmentContext);
  if (!ctx) {
    throw new Error('useEnvironment must be used within EnvironmentProvider');
  }
  return ctx;
}

/**
 * Hook que retorna o perfil do ambiente ativo.
 * Retorna 'unknown' se nenhum ambiente estiver selecionado.
 */
export function useEnvironmentProfile(): EnvironmentProfile {
  const { activeEnvironment } = useEnvironment();
  return activeEnvironment?.profile ?? 'unknown';
}

/**
 * Hook que indica se o ambiente ativo é similar à produção.
 * Usado para adaptar UX sem hardcode de nome de ambiente.
 */
export function useIsProductionLike(): boolean {
  const { activeEnvironment } = useEnvironment();
  return activeEnvironment?.isProductionLike ?? false;
}
