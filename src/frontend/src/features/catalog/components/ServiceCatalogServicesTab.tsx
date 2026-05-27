import { useTranslation } from 'react-i18next';
import { ChevronRight } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { ServiceNode } from '../../../types';
import { EmptyState } from '../../../components/EmptyState';

interface ServiceCatalogServicesTabProps {
  filteredServices: ServiceNode[];
  onSelectNode: (nodeId: string) => void;
}

/** Mapeia serviceType → label curto para o badge de tipo. */
const serviceTypeLabel: Record<string, string> = {
  RestApi: 'REST API',
  SoapService: 'SOAP',
  KafkaProducer: 'Kafka',
  KafkaConsumer: 'Kafka',
  BackgroundService: 'Worker',
  ScheduledProcess: 'Scheduled',
  IntegrationComponent: 'Integration',
  SharedPlatformService: 'Platform',
  GraphqlApi: 'GraphQL',
  Frontend: 'Frontend',
};

/** Variante semântica do badge por tipo de serviço. */
const serviceTypeBadgeVariant = (t: string): 'info' | 'neutral' | 'warning' => {
  if (t === 'RestApi' || t === 'GraphqlApi') return 'info';
  if (t === 'KafkaProducer' || t === 'KafkaConsumer') return 'warning';
  return 'neutral';
};

/** Variante semântica do badge de criticidade. */
const criticalityBadgeVariant = (c: string): 'danger' | 'warning' | 'info' | 'neutral' => {
  const lower = c.toLowerCase();
  if (lower === 'critical' || lower === 'high') return 'danger';
  if (lower === 'medium') return 'warning';
  if (lower === 'low') return 'info';
  return 'neutral';
};

/**
 * Conteúdo da aba "Serviços" do Service Catalog.
 *
 * Avatar: inicial do serviço com fundo tonal (rgba(27,127,232,.1)).
 * Segunda linha: team · stack.
 * Badges: tipo de serviço (à esquerda) + criticidade.
 * Hover de linha com transition-colors.
 */
export function ServiceCatalogServicesTab({ filteredServices, onSelectNode }: ServiceCatalogServicesTabProps) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardBody className="p-0">
        {!filteredServices.length ? (
          <EmptyState
            title={t('serviceCatalog.noServices')}
            description={t('serviceCatalog.noServicesDescription', 'Register services to see them here')}
            size="compact"
          />
        ) : (
          <ul className="divide-y divide-edge">
            {filteredServices.map((svc) => {
              const initial = svc.name?.[0]?.toUpperCase() ?? '?';
              const typeLabel = serviceTypeLabel[svc.serviceType] ?? svc.serviceType;
              const typeVariant = serviceTypeBadgeVariant(svc.serviceType);
              const critVariant = criticalityBadgeVariant(svc.criticality);

              return (
                <li
                  key={svc.serviceAssetId}
                  role="button"
                  tabIndex={0}
                  className="px-5 py-3 flex items-center gap-3.5 hover:bg-hover transition-colors cursor-pointer"
                  onClick={() => onSelectNode(svc.serviceAssetId)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onSelectNode(svc.serviceAssetId);
                    }
                  }}
                >
                  {/* Avatar: inicial com fundo tonal */}
                  <div
                    className="shrink-0 w-9 h-9 rounded-lg flex items-center justify-center font-semibold text-sm text-accent"
                    style={{ background: 'rgba(27,127,232,.1)' }}
                    aria-hidden="true"
                  >
                    {initial}
                  </div>

                  {/* Nome + team/stack */}
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-heading truncate leading-tight">{svc.name}</p>
                    <p className="text-xs text-muted truncate leading-tight mt-0.5">
                      {svc.teamName}
                      {svc.domain ? ` · ${svc.domain}` : ''}
                    </p>
                  </div>

                  {/* Badge de tipo de serviço */}
                  <Badge variant={typeVariant} size="sm" className="shrink-0 hidden sm:inline-flex">
                    {typeLabel}
                  </Badge>

                  {/* Badge de criticidade */}
                  <Badge variant={critVariant} size="sm" dot className="shrink-0">
                    {svc.criticality}
                  </Badge>

                  <ChevronRight size={15} className="text-muted shrink-0" />
                </li>
              );
            })}
          </ul>
        )}
      </CardBody>
    </Card>
  );
}
