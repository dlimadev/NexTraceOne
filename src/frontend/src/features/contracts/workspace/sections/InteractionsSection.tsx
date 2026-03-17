import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { MessageSquare, Plus, Code, ArrowDown, ArrowUp, Tag } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import type { InteractionExample } from '../../types/domain';

interface InteractionsSectionProps {
  contractId?: string;
  protocol?: string;
  className?: string;
}

const MOCK_INTERACTIONS: InteractionExample[] = [
  {
    id: 'int-1',
    name: 'Create Order — Success',
    description: 'Successful creation of a new order with valid payload.',
    request: JSON.stringify({
      method: 'POST',
      path: '/api/v1/orders',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer {token}' },
      body: { customerId: 'cust-123', items: [{ productId: 'prod-456', quantity: 2 }], currency: 'EUR' },
    }, null, 2),
    response: JSON.stringify({
      status: 201,
      headers: { 'Content-Type': 'application/json' },
      body: { orderId: 'ord-789', status: 'Created', total: 49.98, currency: 'EUR' },
    }, null, 2),
    contentFormat: 'json',
    tags: ['happy-path', 'order'],
  },
  {
    id: 'int-2',
    name: 'Create Order — Validation Error',
    description: 'Attempting to create an order with missing required fields.',
    request: JSON.stringify({
      method: 'POST',
      path: '/api/v1/orders',
      body: { customerId: 'cust-123' },
    }, null, 2),
    response: JSON.stringify({
      status: 400,
      body: { code: 'VALIDATION_ERROR', message: 'Missing required field: items', details: [{ field: 'items', rule: 'required' }] },
    }, null, 2),
    contentFormat: 'json',
    tags: ['error', 'validation'],
  },
  {
    id: 'int-3',
    name: 'Get Order — Not Found',
    description: 'Querying a non-existent order.',
    request: JSON.stringify({
      method: 'GET',
      path: '/api/v1/orders/ord-000',
    }, null, 2),
    response: JSON.stringify({
      status: 404,
      body: { code: 'NOT_FOUND', message: 'Order ord-000 not found' },
    }, null, 2),
    contentFormat: 'json',
    tags: ['error', 'not-found'],
  },
];

/**
 * Secção de Interactions do studio — exemplos de interacção com o contrato.
 * Mostra request/response com formatação, tags e descrições.
 */
export function InteractionsSection({ className = '' }: InteractionsSectionProps) {
  const { t } = useTranslation();
  const [expandedId, setExpandedId] = useState<string | null>(null);

  return (
    <div className={`space-y-4 ${className}`}>
      <div className="flex items-center justify-between flex-wrap gap-2">
        <div className="flex items-center gap-2">
          <MessageSquare size={14} className="text-accent" />
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.studio.interactions.title', 'Interaction Examples')}
          </h3>
          <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
            {MOCK_INTERACTIONS.length}
          </span>
        </div>
        <button className="inline-flex items-center gap-1 px-2.5 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
          <Plus size={10} /> {t('contracts.studio.interactions.add', 'Add Example')}
        </button>
      </div>

      {MOCK_INTERACTIONS.length === 0 ? (
        <EmptyState
          title={t('contracts.studio.interactions.emptyTitle', 'No interaction examples')}
          description={t('contracts.studio.interactions.emptyDescription', 'Add request/response examples to help consumers understand how to use this contract.')}
          icon={<MessageSquare size={24} />}
        />
      ) : (
        <div className="space-y-2">
          {MOCK_INTERACTIONS.map((interaction) => {
            const isExpanded = expandedId === interaction.id;
            return (
              <Card key={interaction.id}>
                <button
                  onClick={() => setExpandedId(isExpanded ? null : interaction.id)}
                  className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors"
                >
                  <Code size={12} className="text-accent flex-shrink-0" />
                  <span className="text-xs font-semibold text-heading flex-1">{interaction.name}</span>
                  {interaction.tags?.map((tag) => (
                    <span key={tag} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated border border-edge text-muted">
                      {tag}
                    </span>
                  ))}
                </button>

                {isExpanded && (
                  <CardBody className="pt-0 px-4 pb-4 border-t border-edge space-y-3">
                    {interaction.description && (
                      <p className="text-xs text-body">{interaction.description}</p>
                    )}

                    {/* Request */}
                    {interaction.request && (
                      <div>
                        <div className="flex items-center gap-1.5 mb-1.5">
                          <ArrowUp size={10} className="text-cyan" />
                          <span className="text-[10px] font-medium text-muted">
                            {t('contracts.studio.interactions.request', 'Request')}
                          </span>
                        </div>
                        <pre className="text-[10px] font-mono text-body bg-elevated rounded p-3 border border-edge overflow-x-auto max-h-48 whitespace-pre-wrap">
                          {interaction.request}
                        </pre>
                      </div>
                    )}

                    {/* Response */}
                    {interaction.response && (
                      <div>
                        <div className="flex items-center gap-1.5 mb-1.5">
                          <ArrowDown size={10} className="text-mint" />
                          <span className="text-[10px] font-medium text-muted">
                            {t('contracts.studio.interactions.response', 'Response')}
                          </span>
                        </div>
                        <pre className="text-[10px] font-mono text-body bg-elevated rounded p-3 border border-edge overflow-x-auto max-h-48 whitespace-pre-wrap">
                          {interaction.response}
                        </pre>
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
