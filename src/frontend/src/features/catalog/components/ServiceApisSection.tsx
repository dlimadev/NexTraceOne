import { Globe, Eye } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { TableWrapper } from '../../../components/shell';
import type { ServiceApiSummary } from '../../../types';

/**
 * Secção que apresenta as APIs de um serviço em tabela.
 * Inclui colunas: nome, padrão de rota, versão, visibilidade,
 * contagem de consumidores e estado (ativo/desativado).
 * Mostra estado vazio quando não há APIs.
 */
export function ServiceApisSection({ apis }: { apis: ServiceApiSummary[] }) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Globe size={16} className="text-accent" />
          <h2 className="text-base font-semibold text-heading">{t('catalog.detail.apis')}</h2>
        </div>
      </CardHeader>
      <CardBody className="p-0">
        {apis.length === 0 ? (
          <div className="py-10 text-center">
            <p className="text-sm text-muted">{t('catalog.detail.noApis')}</p>
          </div>
        ) : (
          <TableWrapper>
            <table className="w-full text-sm">
              <thead className="sticky top-0 z-10 bg-panel">
                <tr className="border-b border-edge text-left">
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.columns.name')}</th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.routePattern')}</th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.version')}</th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.visibility')}</th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.consumers')}</th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.status')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {apis.map((api: ServiceApiSummary) => (
                  <tr key={api.apiId} className="hover:bg-elevated/50 transition-colors">
                    <td className="px-4 py-3 font-medium text-heading">{api.name}</td>
                    <td className="px-4 py-3 text-muted font-mono text-xs">{api.routePattern}</td>
                    <td className="px-4 py-3 text-muted">{api.version}</td>
                    <td className="px-4 py-3">
                      <span className="inline-flex items-center gap-1 text-xs text-muted">
                        <Eye size={12} />
                        {api.visibility}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-muted">{api.consumerCount}</td>
                    <td className="px-4 py-3">
                      {api.isDecommissioned
                        ? <Badge variant="danger" size="sm">{t('catalog.detail.decommissioned')}</Badge>
                        : <Badge variant="success" size="sm">{t('catalog.detail.active')}</Badge>}
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
