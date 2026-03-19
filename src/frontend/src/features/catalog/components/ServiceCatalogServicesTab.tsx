import { useTranslation } from 'react-i18next';
import { Server, ChevronRight } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { ServiceNode } from '../../../types';

interface ServiceCatalogServicesTabProps {
  filteredServices: ServiceNode[];
  onSelectNode: (nodeId: string) => void;
}

/**
 * Conteúdo da aba "Serviços" do Service Catalog.
 * Exibe a lista filtrada de serviços registrados no asset graph.
 */
export function ServiceCatalogServicesTab({ filteredServices, onSelectNode }: ServiceCatalogServicesTabProps) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardBody className="p-0">
        {!filteredServices.length ? (
          <p className="px-6 py-12 text-sm text-muted text-center">{t('serviceCatalog.noServices')}</p>
        ) : (
          <ul className="divide-y divide-edge">
            {filteredServices.map((svc) => (
              <li
                key={svc.serviceAssetId}
                className="px-6 py-4 flex items-center gap-4 hover:bg-hover transition-colors cursor-pointer"
                onClick={() => onSelectNode(svc.serviceAssetId)}
              >
                <div className="w-10 h-10 rounded-lg bg-accent/15 flex items-center justify-center text-accent">
                  <Server size={18} />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-heading">{svc.name}</p>
                  <p className="text-xs text-muted">{svc.teamName} · {svc.domain}</p>
                </div>
                <Badge variant="info">{svc.domain}</Badge>
                <ChevronRight size={16} className="text-muted" />
              </li>
            ))}
          </ul>
        )}
      </CardBody>
    </Card>
  );
}
