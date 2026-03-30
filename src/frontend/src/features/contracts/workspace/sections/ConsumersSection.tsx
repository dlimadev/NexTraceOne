import React from 'react';
import { useTranslation } from 'react-i18next';
import { Users, ArrowUpRight, ArrowDownLeft, ExternalLink, Clock, Tag, Bell, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { useContractSubscribers } from '../../hooks';
import type { StudioContract, StudioRelationship } from '../studioTypes';
import type { ContractSubscriber } from '../../types';

interface ConsumersSectionProps {
  contract: StudioContract;
  className?: string;
}

/**
 * Secção de Consumers / Producers / Portal Subscribers do studio.
 * - Topology consumers: observados via Graph (OpenTelemetry, trace analysis, etc.)
 * - Producers: relevante para contratos de eventos; REST APIs não têm produtores
 * - Portal Subscribers: subscrições formais via Developer Portal
 */
export function ConsumersSection({ contract, className = '' }: ConsumersSectionProps) {
  const { t } = useTranslation();
  const subscribersQuery = useContractSubscribers(contract.apiAssetId);
  const subscribers = subscribersQuery.data?.consumers ?? [];

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

      {/* ── Portal Subscribers ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Bell size={14} className="text-amber-400" />
              <h3 className="text-xs font-semibold text-heading">
                {t('contracts.studio.consumers.subscribersTitle', 'Portal Subscribers')}
              </h3>
              <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
                {subscribersQuery.isLoading ? '…' : subscribers.length}
              </span>
            </div>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {subscribersQuery.isLoading ? (
            <div className="p-4 text-xs text-muted animate-pulse">
              {t('common.loading', 'Loading...')}
            </div>
          ) : subscribers.length === 0 ? (
            <div className="p-6">
              <EmptyState
                title={t('contracts.studio.consumers.noSubscribers', 'No subscribers')}
                description={t('contracts.studio.consumers.noSubscribersDescription', 'Developers who subscribed to change notifications via the Developer Portal will appear here.')}
                icon={<Bell size={20} />}
                size="compact"
              />
            </div>
          ) : (
            <div className="divide-y divide-edge">
              {subscribers.map((subscriber) => (
                <SubscriberRow key={subscriber.subscriberId} subscriber={subscriber} />
              ))}
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Summary ── */}
      <div className="grid grid-cols-3 gap-3">
        <div className="rounded-lg border border-cyan/20 bg-cyan/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.consumers.totalConsumers', 'Total Consumers')}</p>
          <p className="text-lg font-bold text-cyan">{contract.consumers.length}</p>
        </div>
        <div className="rounded-lg border border-mint/20 bg-mint/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.consumers.totalProducers', 'Total Producers')}</p>
          <p className="text-lg font-bold text-mint">{contract.producers.length}</p>
        </div>
        <div className="rounded-lg border border-amber-400/20 bg-amber-400/5 px-4 py-3">
          <p className="text-[10px] text-muted mb-0.5">{t('contracts.studio.consumers.totalSubscribers', 'Total Subscribers')}</p>
          <p className="text-lg font-bold text-amber-400">{subscribers.length}</p>
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
          {relationship.confidenceScore !== undefined && (
            <>
              <span className="text-[10px] text-muted">·</span>
              <ConfidenceBadge score={relationship.confidenceScore} />
            </>
          )}
        </div>
      </div>

      <button className="text-muted hover:text-accent transition-colors">
        <ExternalLink size={12} />
      </button>
    </div>
  );
}

function SubscriberRow({ subscriber }: { subscriber: ContractSubscriber }) {
  return (
    <div className="flex items-center gap-3 px-6 py-3 text-xs hover:bg-elevated/30 transition-colors">
      <Bell size={12} className="text-amber-400 flex-shrink-0" />

      <div className="flex-1 min-w-0">
        <p className="text-xs font-medium text-heading truncate">{subscriber.consumerServiceName}</p>
        <div className="flex items-center gap-2 mt-0.5">
          <span className="text-[10px] text-muted truncate max-w-[140px]">{subscriber.subscriberEmail}</span>
          <span className="text-[10px] text-muted">·</span>
          <span className="inline-flex items-center gap-0.5 text-[10px] text-muted">
            <Tag size={8} /> {subscriber.subscriptionLevel}
          </span>
          <span className="text-[10px] text-muted">·</span>
          <span className="inline-flex items-center gap-0.5 text-[10px] text-muted">
            <Clock size={8} /> {formatDate(subscriber.subscribedAt)}
          </span>
        </div>
      </div>

      {subscriber.isActive ? (
        <CheckCircle size={12} className="text-mint flex-shrink-0" />
      ) : (
        <XCircle size={12} className="text-red-400 flex-shrink-0" />
      )}
    </div>
  );
}

function ConfidenceBadge({ score }: { score: number }) {
  const pct = Math.round(score * 100);
  const colorClass = score >= 0.8 ? 'text-mint' : score >= 0.5 ? 'text-amber-400' : 'text-red-400';
  return <span className={`text-[10px] font-medium ${colorClass}`}>{pct}%</span>;
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
