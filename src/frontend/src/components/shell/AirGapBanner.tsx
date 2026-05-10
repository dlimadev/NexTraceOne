import { ShieldOff } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import client from '../../api/client';

interface NetworkPolicyResponse {
  mode: string;
  activeCalls: number;
  blockedCalls: number;
}

function useNetworkPolicy() {
  return useQuery({
    queryKey: ['platform', 'network-policy'],
    queryFn: () =>
      client
        .get<NetworkPolicyResponse>('/api/v1/platform/network-policy')
        .then((r) => r.data),
    staleTime: 5 * 60 * 1_000,
    retry: false,
  });
}

/**
 * Banner persistente que aparece quando a plataforma está em modo AirGap.
 *
 * Visível apenas a utilizadores com permissão platform:admin:read — outros
 * utilizadores recebem 403 silenciosamente e o banner não aparece.
 * Não é dispensável: o modo AirGap é uma restrição de segurança permanente.
 */
export function AirGapBanner() {
  const { t } = useTranslation();
  const { data } = useNetworkPolicy();

  if (data?.mode !== 'AirGap') {
    return null;
  }

  return (
    <div
      role="alert"
      aria-live="assertive"
      aria-atomic="true"
      className="flex items-center gap-2 px-4 py-2 text-xs bg-destructive/15 border-b border-destructive/25 text-destructive"
    >
      <ShieldOff size={13} className="shrink-0" aria-hidden="true" />
      <span>
        {t(
          'platform.airGapBanner',
          'Air-Gap mode is active — all outbound network calls are blocked. External integrations are unavailable.',
        )}
      </span>
      <span className="ml-auto font-mono opacity-60">
        {t('platform.airGapMode', 'NetworkIsolation: AirGap')}
      </span>
    </div>
  );
}
