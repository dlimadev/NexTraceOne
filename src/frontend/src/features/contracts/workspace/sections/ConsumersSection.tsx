import { useTranslation } from 'react-i18next';
import { Users, ArrowUpRight, ArrowDownLeft, ExternalLink, Clock, Tag } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import type { StudioContract, StudioRelationship } from '../studioTypes';

interface ConsumersSectionProps {
  contract: StudioContract;
  className?: string;
}

/**
 * Secção de Consumers / Producers do studio.
 * Mostra quem consome e quem produz para este contrato, com tipos,
 * datas de registo e links contextuais.
 */
export function ConsumersSection({ contract, className = '' }: ConsumersSectionProps) {
  const { t } = useTranslation();

  return (
    <div className={`space-y-6 ${className}`}>
      {/* ── Consumers ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ArrowUpRight size={14} className="text-cyan" />
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.studio.consumers.consumersTitle', 'Consumers')}
              </h3>
              <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
                {contract.consumers.length}
              </span>
            </div>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {contract.consumers.length === 0 ? (
            <div className="p-6">
              <EmptyState
                title={t('contracts.studio.consumers.noConsumers', 'No consumers registered')}
                description={t('contracts.studio.consumers.noConsumersDescription', 'Services and applications consuming this contract will appear here.')}
                icon={<Users size={20} />}
                size="compact"
              />
            </div>
          ) : (
            <div className="divide-y divide-edge">
              {contract.consumers.map((consumer) => (
                <RelationshipRow
                  key={consumer.id}
                  relationship={consumer}
                  direction="consumer"
                />
              ))}
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Producers ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <ArrowDownLeft size={14} className="text-mint" />
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.studio.consumers.producersTitle', 'Producers')}
              </h3>
              <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
                {contract.producers.length}
              </span>
            </div>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {contract.producers.length === 0 ? (
            <div className="p-6">
              <EmptyState
                title={t('contracts.studio.consumers.noProducers', 'No producers registered')}
                description={t('contracts.studio.consumers.noProducersDescription', 'Services producing events or data for this contract will appear here.')}
                icon={<Users size={20} />}
                size="compact"
              />
            </div>
          ) : (
            <div className="divide-y divide-edge">
              {contract.producers.map((producer) => (
                <RelationshipRow
                  key={producer.id}
                  relationship={producer}
                  direction="producer"
                />
              ))}
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Summary ── */}
      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-lg border border-cyan/20 bg-cyan/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.consumers.totalConsumers', 'Total Consumers')}</p>
          <p className="text-lg font-bold text-cyan">{contract.consumers.length}</p>
        </div>
        <div className="rounded-lg border border-mint/20 bg-mint/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.consumers.totalProducers', 'Total Producers')}</p>
          <p className="text-lg font-bold text-mint">{contract.producers.length}</p>
        </div>
      </div>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function RelationshipRow({
  relationship,
  direction,
}: {
  relationship: StudioRelationship;
  direction: 'consumer' | 'producer';
}) {
  return (
    <div className="flex items-center gap-3 px-6 py-3 text-xs hover:bg-elevated/30 transition-colors">
      {direction === 'consumer' ? (
        <ArrowUpRight size={12} className="text-cyan flex-shrink-0" />
      ) : (
        <ArrowDownLeft size={12} className="text-mint flex-shrink-0" />
      )}

      <div className="flex-1 min-w-0">
        <p className="text-xs font-medium text-heading">{relationship.name}</p>
        <div className="flex items-center gap-2 mt-0.5">
          <span className="inline-flex items-center gap-0.5 text-[10px] text-muted">
            <Tag size={8} /> {relationship.type}
          </span>
          <span className="text-[10px] text-muted">·</span>
          <span className="inline-flex items-center gap-0.5 text-[10px] text-muted">
            <Clock size={8} /> {formatDate(relationship.registeredAt)}
          </span>
        </div>
      </div>

      <button className="text-muted hover:text-accent transition-colors">
        <ExternalLink size={12} />
      </button>
    </div>
  );
}

function formatDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  } catch {
    return dateStr;
  }
}
