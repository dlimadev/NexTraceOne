import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Target, Plus, ChevronDown, ChevronRight, User, ArrowRight } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import type { UseCase } from '../../types/domain';

interface UseCasesSectionProps {
  contractId?: string;
  className?: string;
}

const MOCK_USE_CASES: UseCase[] = [
  {
    id: 'uc-1',
    title: 'Create a new order',
    description: 'A customer places a new order through the mobile app or web portal.',
    actor: 'Customer',
    preconditions: 'Customer is authenticated and has items in cart.',
    flow: '1. Customer submits order → 2. API validates payload → 3. Order is created → 4. Confirmation returned → 5. Event published to downstream services.',
    postconditions: 'Order record exists in the system. Notification sent to fulfillment.',
  },
  {
    id: 'uc-2',
    title: 'Retrieve order status',
    description: 'An internal service or customer queries the status of an existing order.',
    actor: 'Customer / Internal Service',
    preconditions: 'Valid order ID provided.',
    flow: '1. Caller sends GET /orders/{id} → 2. API looks up order → 3. Returns current status with tracking details.',
    postconditions: 'No state change. Read-only operation.',
  },
  {
    id: 'uc-3',
    title: 'Cancel an order',
    description: 'A customer or admin cancels an order before it enters fulfillment.',
    actor: 'Customer / Admin',
    preconditions: 'Order exists and is in cancellable state.',
    flow: '1. Caller sends DELETE /orders/{id} → 2. API validates cancellation window → 3. Order state updated → 4. Refund initiated → 5. Cancellation event published.',
    postconditions: 'Order is cancelled. Refund processing started.',
  },
];

/**
 * Secção de Use Cases do studio — casos de uso documentados do contrato.
 * Mostra actor, pré-condições, fluxo e pós-condições.
 */
export function UseCasesSection({ className = '' }: UseCasesSectionProps) {
  const { t } = useTranslation();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  return (
    <div className={`space-y-4 ${className}`}>
      <div className="flex items-center justify-between flex-wrap gap-2">
        <div className="flex items-center gap-2">
          <Target size={14} className="text-accent" />
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.studio.useCases.title', 'Use Cases')}
          </h3>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
            {MOCK_USE_CASES.length}
          </span>
        </div>
        <button className="inline-flex items-center gap-1 px-2.5 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
          <Plus size={10} /> {t('contracts.studio.useCases.add', 'Add Use Case')}
        </button>
      </div>

      {MOCK_USE_CASES.length === 0 ? (
        <EmptyState
          title={t('contracts.studio.useCases.emptyTitle', 'No use cases documented')}
          description={t('contracts.studio.useCases.emptyDescription', 'Document use cases to help consumers understand how to interact with this contract.')}
          icon={<Target size={24} />}
        />
      ) : (
        <div className="space-y-2">
          {MOCK_USE_CASES.map((uc) => {
            const isExpanded = expandedId === uc.id;
            return (
              <Card key={uc.id}>
                <button
                  onClick={() => setExpandedId(isExpanded ? null : uc.id)}
                  className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors"
                >
                  {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                  <Target size={12} className="text-accent flex-shrink-0" />
                  <span className="text-xs font-semibold text-heading flex-1">{uc.title}</span>
                  {uc.actor && (
                    <span className="inline-flex items-center gap-1 text-[10px] text-muted">
                      <User size={9} /> {uc.actor}
                    </span>
                  )}
                </button>

                {isExpanded && (
                  <CardBody className="pt-0 px-4 pb-4 border-t border-edge space-y-3">
                    <p className="text-xs text-body leading-relaxed">{uc.description}</p>

                    {uc.preconditions && (
                      <div>
                        <p className="text-[10px] font-medium text-muted mb-0.5">
                          {t('contracts.studio.useCases.preconditions', 'Preconditions')}
                        </p>
                        <p className="text-xs text-body">{uc.preconditions}</p>
                      </div>
                    )}

                    {uc.flow && (
                      <div>
                        <p className="text-[10px] font-medium text-muted mb-0.5">
                          {t('contracts.studio.useCases.flow', 'Flow')}
                        </p>
                        <div className="text-xs text-body bg-elevated rounded p-3 border border-edge">
                          {uc.flow.split('→').map((step, idx) => (
                            <div key={idx} className="flex items-start gap-2 py-0.5">
                              <ArrowRight size={10} className="text-accent mt-0.5 flex-shrink-0" />
                              <span>{step.trim()}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {uc.postconditions && (
                      <div>
                        <p className="text-[10px] font-medium text-muted mb-0.5">
                          {t('contracts.studio.useCases.postconditions', 'Postconditions')}
                        </p>
                        <p className="text-xs text-body">{uc.postconditions}</p>
                      </div>
                    )}
                  </CardBody>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
