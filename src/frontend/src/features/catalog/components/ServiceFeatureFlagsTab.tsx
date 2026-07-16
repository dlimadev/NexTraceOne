import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle } from 'lucide-react';
import { serviceFeatureFlagsApi } from '../api/featureFlags';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { TableWrapper } from '../../../components/shell';
import { Toggle } from '../../../components/Toggle';

/**
 * Aba de feature flags de um serviço específico, embutida no detalhe do serviço.
 * Filtra o dashboard global de feature flags por serviceId, reutilizando a query
 * cache da página de portefólio para evitar pedidos duplicados.
 */
export function ServiceFeatureFlagsTab({ serviceId }: { serviceId: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { activeEnvironmentId } = useEnvironment();

  // Reutiliza a mesma queryKey do ServiceFeatureFlagsPage para deduplicar pedidos
  const { data, isLoading, isError } = useQuery({
    queryKey: ['service-feature-flags', activeEnvironmentId],
    queryFn: () => serviceFeatureFlagsApi.getDashboard(),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ flagId, enabled }: { flagId: string; enabled: boolean }) =>
      serviceFeatureFlagsApi.toggle(flagId, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service-feature-flags'] });
    },
  });

  // Filtra as flags pelo serviço atual
  const flags = (data?.flags ?? []).filter((f) => f.serviceId === serviceId);

  return (
    <Card>
      <CardHeader>
        <span className="text-sm font-semibold text-heading flex-1">
          {t('serviceDetail.tabFeatureFlags', 'Feature Flags')}
        </span>
        <Link
          to="/services/feature-flags"
          className="text-xs text-accent hover:underline"
        >
          {t('featureFlags.viewPortfolio', 'View all portfolio flags')} →
        </Link>
      </CardHeader>

      <CardBody className="p-0">
        {/* Estado de carregamento */}
        {isLoading && (
          <div className="flex justify-center py-12">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
          </div>
        )}

        {/* Estado de erro */}
        {isError && (
          <div className="m-4 bg-critical/10 border border-critical/30 rounded-lg p-4 flex items-start gap-3">
            <AlertTriangle size={18} className="text-critical mt-0.5 shrink-0" />
            <div>
              <p className="text-sm font-medium text-critical">
                {t('featureFlags.errorTitle', 'Erro ao carregar feature flags')}
              </p>
              <p className="text-xs text-critical/80 mt-1">
                {t('featureFlags.errorDesc', 'Verifique a conectividade e tente novamente.')}
              </p>
            </div>
          </div>
        )}

        {/* Estado vazio — serviço sem flags */}
        {!isLoading && !isError && flags.length === 0 && (
          <div className="py-10 text-center text-muted px-4">
            <p className="text-sm">
              {t('featureFlags.serviceEmpty', 'This service has no registered feature flags.')}
            </p>
          </div>
        )}

        {/* Tabela de flags */}
        {!isLoading && !isError && flags.length > 0 && (
          <TableWrapper>
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-elevated border-b border-edge">
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                    {t('featureFlags.colFlag', 'Flag')}
                  </th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                    {t('featureFlags.colEnvironment', 'Ambiente')}
                  </th>
                  <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                    {t('featureFlags.colUpdated', 'Atualizado')}
                  </th>
                  <th className="text-center px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                    {t('featureFlags.colStatus', 'Status')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {flags.map((flag) => (
                  <tr key={flag.id} className="hover:bg-elevated/30 transition-colors">
                    <td className="px-4 py-3">
                      <p className="font-medium text-heading">{flag.displayName}</p>
                      <p className="text-xs text-muted font-mono mt-0.5">{flag.flagKey}</p>
                      {flag.description && (
                        <p className="text-xs text-muted mt-0.5">{flag.description}</p>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-elevated text-body font-mono border border-edge">
                        {flag.environment}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-muted text-xs">
                      {new Date(flag.updatedAt).toLocaleDateString('pt-BR', {
                        day: '2-digit',
                        month: '2-digit',
                        year: '2-digit',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                      {flag.updatedBy && (
                        <span className="block text-muted/70">{flag.updatedBy}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <Toggle
                        checked={flag.enabled}
                        onChange={(checked) =>
                          toggleMutation.mutate({ flagId: flag.id, enabled: checked })
                        }
                        disabled={toggleMutation.isPending}
                        label={
                          flag.enabled
                            ? t('featureFlags.disable', 'Desabilitar')
                            : t('featureFlags.enable', 'Habilitar')
                        }
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </TableWrapper>
        )}
      </CardBody>
    </Card>
  );
}
