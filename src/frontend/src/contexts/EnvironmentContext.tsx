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
import apiClient from '../api/client';

// ── Types ───────────────────────────────────────────────────────────────────

/**
 * Perfil do ambiente — classificação operacional armazenada no banco de dados.
 * Determina a UX contextual (ícone, cor, comportamento) sem depender do nome livre
 * definido pelo utilizador.
 *
 * Mapeado a partir do enum EnvironmentProfile do backend (serializado como lowercase):
 *   Development=1, Validation=2, Staging=3, Production=4, Sandbox=5,
 *   DisasterRecovery=6, Training=7, UserAcceptanceTesting=8, PerformanceTesting=9
 */
export type EnvironmentProfile =
  | 'production'
  | 'staging'
  | 'useracceptancetesting'
  | 'validation'
  | 'development'
  | 'sandbox'
  | 'disasterrecovery'
  | 'training'
  | 'performancetesting'
  | 'unknown';

/** Descrição de um ambiente disponível para o tenant ativo. */
export interface EnvironmentOption {
  id: string;
  name: string;
  profile: EnvironmentProfile;
  isProductionLike: boolean;
  isPrimaryProduction?: boolean;
  isDefault?: boolean;
}

/** Shape da resposta da API GET /api/v1/identity/environments */
interface ApiEnvironmentResponse {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
  isActive: boolean;
  /** Presente quando a migração AddEnvironmentProfileFields estiver aplicada. */
  profile?: string;
  /** Presente quando a migração AddEnvironmentProfileFields estiver aplicada. */
  isProductionLike?: boolean;
  /** Indica se este é o ambiente produtivo principal do tenant. */
  isPrimaryProduction?: boolean;
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

// ── Profile inference ────────────────────────────────────────────────────────

/**
 * Infere o perfil do ambiente a partir do slug ou nome quando o backend
 * ainda não retorna o campo `profile` (fallback de compatibilidade anterior à
 * migração AddEnvironmentProfileFields).
 *
 * Importante: o perfil correto vem sempre do banco de dados. Esta inferência é
 * apenas um fallback para ambientes criados antes do campo profile ser persistido.
 * O nome do ambiente é livre (ex: "Dev Teste", "QA-EUROPA") e NÃO determina o perfil.
 */
function inferProfile(slug: string, name: string): EnvironmentProfile {
  const text = `${slug} ${name}`.toLowerCase();
  if (/\bprod(?:uction)?\b/.test(text)) return 'production';
  if (/\bdr\b|\bdisaster\b/.test(text)) return 'disasterrecovery';
  if (/\bstag(?:ing|e)?\b|\bhomolog/.test(text)) return 'staging';
  if (/\buat\b/.test(text)) return 'useracceptancetesting';
  if (/\bperf\b|\bperformance\b|\bload\b/.test(text)) return 'performancetesting';
  if (/\bqa\b|\bvalid/.test(text)) return 'validation';
  if (/\bdev(?:elopment)?\b/.test(text)) return 'development';
  if (/\bsandbox\b|\bdemo\b/.test(text)) return 'sandbox';
  if (/\btrain(?:ing)?\b/.test(text)) return 'training';
  return 'unknown';
}

/**
 * Infere isProductionLike a partir do perfil quando o backend não retorna o campo.
 * Espelha a lógica do domínio: Production, DisasterRecovery, Staging, UAT são production-like.
 */
function inferIsProductionLike(profile: EnvironmentProfile): boolean {
  return (
    profile === 'production' ||
    profile === 'disasterrecovery' ||
    profile === 'staging' ||
    profile === 'useracceptancetesting'
  );
}

/**
 * Mapeia a resposta da API para EnvironmentOption normalizado.
 * Usa os campos do backend quando disponíveis; infere a partir do slug/name como fallback.
 */
function mapApiEnvironment(env: ApiEnvironmentResponse): EnvironmentOption {
  const profile = (env.profile as EnvironmentProfile | undefined) ?? inferProfile(env.slug, env.name);
  const isProductionLike = env.isProductionLike ?? inferIsProductionLike(profile);
  return {
    id: env.id,
    name: env.name,
    profile,
    isProductionLike,
    isPrimaryProduction: env.isPrimaryProduction === true,
    isDefault: env.isDefault === true,
  };
}

// ── Provider ─────────────────────────────────────────────────────────────────

/**
 * Provider do contexto de ambiente ativo.
 *
 * Responsabilidades:
 * - Carregar ambientes reais do tenant via GET /api/v1/identity/environments
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
  const { isAuthenticated } = useAuth();

  const [availableEnvironments, setAvailableEnvironments] = useState<EnvironmentOption[]>([]);
  const [activeEnvironmentId, setActiveEnvironmentId] = useState<string | null>(null);
  const [isLoadingEnvironments, setIsLoadingEnvironments] = useState(false);

  // Load environments from the real API when authentication state changes
  useEffect(() => {
    if (!isAuthenticated) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- reset state on auth change is intentional
      setAvailableEnvironments([]);
      setActiveEnvironmentId(null);
      clearEnvironmentId();
      return;
    }

    let cancelled = false;
    setIsLoadingEnvironments(true);

    apiClient
      .get<ApiEnvironmentResponse[]>('/identity/environments')
      .then((response) => {
        if (cancelled) return;

        const environments = (response.data ?? [])
          .filter((e) => e.isActive)
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map(mapApiEnvironment);

        setAvailableEnvironments(environments);

        // Restore persisted environment or select the default one
        const persisted = getEnvironmentId();
        const persistedEnv = persisted ? environments.find((e) => e.id === persisted) : null;
        const defaultEnv = environments.find((e) => e.isDefault) ?? environments[0];
        const resolved = persistedEnv ?? defaultEnv ?? null;

        if (resolved) {
          setActiveEnvironmentId(resolved.id);
          storeEnvironmentId(resolved.id);
        }
      })
      .catch(() => {
        if (cancelled) return;
        // Non-blocking: leave environments empty on error; user can retry or use without context
        setAvailableEnvironments([]);
      })
      .finally(() => {
        if (!cancelled) setIsLoadingEnvironments(false);
      });

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated]);

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
