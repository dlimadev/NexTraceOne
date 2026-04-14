import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { BrainCircuit, Wand2, Check, X, MessageSquare } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

interface KnowledgeProposal {
  id: string;
  proposalTitle: string;
  proposalType: 'note' | 'runbook';
  confidence: number;
  sourceChannel: string;
  extractedAt: string;
  review: boolean;
}

interface ExtractionResponse {
  proposals: KnowledgeProposal[];
  total: number;
}

const useExtraction = (conversation: string, enabled: boolean) =>
  useQuery({
    queryKey: ['ai-knowledge-extraction', conversation.substring(0, 30)],
    queryFn: () =>
      client
        .post<ExtractionResponse>('/ai/knowledge-extraction', { conversation })
        .then((r) => r.data),
    enabled,
  });

export function AiKnowledgeExtractionPage() {
  const { t } = useTranslation();
  const [conversation, setConversation] = useState('');
  const [extract, setExtract] = useState(false);
  const [dismissed, setDismissed] = useState<Set<string>>(new Set());
  const { data, isLoading, isError, refetch } = useExtraction(conversation, extract && conversation.length > 20);

  if (isLoading && extract) return <PageLoadingState message={t('aiKnowledgeExtraction.extracting')} />;
  if (isError && extract) return <PageErrorState message={t('aiKnowledgeExtraction.error')} onRetry={() => refetch()} />;

  const proposals = (data?.proposals ?? []).filter((p) => !dismissed.has(p.id));

  return (
    <PageContainer>
      <PageHeader
        title={t('aiKnowledgeExtraction.title')}
        subtitle={t('aiKnowledgeExtraction.subtitle')}
      />

      <Card className="mb-4">
        <CardBody className="p-4">
          <label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2 block">
            {t('aiKnowledgeExtraction.pasteConversation')}
          </label>
          <textarea
            value={conversation}
            onChange={(e) => { setConversation(e.target.value); setExtract(false); }}
            rows={6}
            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-sm p-2 resize-y mb-2"
          />
          <div className="flex justify-end">
            <Button
              size="sm"
              onClick={() => setExtract(true)}
              disabled={conversation.length < 20}
            >
              <Wand2 size={14} className="mr-1" />
              {t('aiKnowledgeExtraction.extract')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {proposals.length > 0 && (
        <PageSection title={t('aiKnowledgeExtraction.proposals')}>
          <div className="space-y-3">
            {proposals.map((proposal) => (
              <Card key={proposal.id}>
                <CardBody className="p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <MessageSquare size={14} className="text-indigo-500" />
                        <p className="font-medium text-sm text-gray-900 dark:text-white">
                          {proposal.proposalTitle}
                        </p>
                        <Badge variant={proposal.proposalType === 'runbook' ? 'neutral' : 'success'}>
                          {proposal.proposalType}
                        </Badge>
                      </div>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        {t('aiKnowledgeExtraction.confidence')}: {Math.round(proposal.confidence * 100)}% ·{' '}
                        {t('aiKnowledgeExtraction.sourceChannel')}: {proposal.sourceChannel} ·{' '}
                        {t('aiKnowledgeExtraction.extractedAt')}: {proposal.extractedAt}
                      </p>
                    </div>
                    <div className="flex gap-2">
                      <Button size="sm" variant="ghost">
                        <Check size={12} className="mr-1 text-green-500" />
                        {t('aiKnowledgeExtraction.addToKnowledge')}
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => setDismissed((prev) => new Set([...prev, proposal.id]))}
                      >
                        <X size={12} className="mr-1 text-red-500" />
                        {t('aiKnowledgeExtraction.dismiss')}
                      </Button>
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        </PageSection>
      )}

      {data && proposals.length === 0 && (
        <p className="text-sm text-center text-gray-400 py-8">{t('aiKnowledgeExtraction.noProposals')}</p>
      )}
    </PageContainer>
  );
}
