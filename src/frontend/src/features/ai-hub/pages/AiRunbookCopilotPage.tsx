import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Bot, Send, BookOpen, MessageSquare, Info } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

interface RunbookSummary {
  id: string;
  name: string;
  currentStep: number;
  totalSteps: number;
}

interface CopilotAnswer {
  question: string;
  answer: string;
  timestamp: string;
}

interface RunbookListResponse {
  runbooks: RunbookSummary[];
}

const useRunbooks = () =>
  useQuery({
    queryKey: ['ai-runbook-list'],
    queryFn: () =>
      client
        .get<RunbookListResponse>('/operations/runbooks/active')
        .then((r) => r.data),
  });

export function AiRunbookCopilotPage() {
  const { t } = useTranslation();
  const [selectedRunbook, setSelectedRunbook] = useState('');
  const [question, setQuestion] = useState('');
  const [answers, setAnswers] = useState<CopilotAnswer[]>([]);
  const { data, isLoading, isError, refetch } = useRunbooks();

  if (isLoading) return <PageLoadingState message={t('aiRunbookCopilot.loading')} />;
  if (isError) return <PageErrorState message={t('aiRunbookCopilot.error')} onRetry={() => refetch()} />;

  const runbooks = data?.runbooks ?? [];
  const activeRunbook = runbooks.find((r) => r.id === selectedRunbook);

  const handleAsk = () => {
    if (!question || !selectedRunbook) return;
    setAnswers((prev) => [
      { question, answer: '...', timestamp: new Date().toISOString() },
      ...prev,
    ]);
    setQuestion('');
  };

  const suggestedQuestions = [
    t('aiRunbookCopilot.askAboutDependencies'),
    t('aiRunbookCopilot.askAboutHistory'),
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('aiRunbookCopilot.title')}
        subtitle={t('aiRunbookCopilot.subtitle')}
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <Card>
          <CardBody className="p-4">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              {t('aiRunbookCopilot.selectRunbook')}
            </p>
            <div className="space-y-2">
              {runbooks.length === 0 ? (
                <p className="text-xs text-gray-400">{t('aiRunbookCopilot.noRunbook')}</p>
              ) : (
                runbooks.map((rb) => (
                  <button
                    key={rb.id}
                    onClick={() => setSelectedRunbook(rb.id)}
                    className={`w-full text-left p-2 rounded text-sm transition-colors ${
                      selectedRunbook === rb.id
                        ? 'bg-indigo-100 dark:bg-indigo-900 text-indigo-700 dark:text-indigo-300'
                        : 'hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300'
                    }`}
                  >
                    <div className="flex items-center gap-2">
                      <BookOpen size={12} />
                      <span className="truncate">{rb.name}</span>
                    </div>
                    <div className="text-xs text-gray-400 mt-0.5">
                      {t('aiRunbookCopilot.currentStep')}: {rb.currentStep}/{rb.totalSteps}
                    </div>
                  </button>
                ))
              )}
            </div>
          </CardBody>
        </Card>

        <div className="lg:col-span-2 space-y-3">
          {activeRunbook && (
            <Card>
              <CardBody className="p-3 flex items-center gap-2">
                <Badge variant="success">{t('aiRunbookCopilot.activeRunbook')}</Badge>
                <span className="text-sm font-medium text-gray-900 dark:text-white">{activeRunbook.name}</span>
              </CardBody>
            </Card>
          )}

          <Card>
            <CardBody className="p-3">
              <div className="flex gap-2 mb-2">
                <input
                  type="text"
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  placeholder={t('aiRunbookCopilot.questionPlaceholder')}
                  className="flex-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
                  onKeyDown={(e) => e.key === 'Enter' && handleAsk()}
                />
                <Button size="sm" onClick={handleAsk} disabled={!question || !selectedRunbook}>
                  <Send size={14} />
                </Button>
              </div>
              <div className="flex flex-wrap gap-1">
                {suggestedQuestions.map((sq, i) => (
                  <button
                    key={i}
                    onClick={() => setQuestion(sq)}
                    className="text-xs px-2 py-0.5 rounded-full bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 hover:bg-indigo-100 dark:hover:bg-indigo-900"
                  >
                    {sq}
                  </button>
                ))}
              </div>
            </CardBody>
          </Card>

          {!selectedRunbook && (
            <p className="text-sm text-center text-gray-400 py-4">{t('aiRunbookCopilot.noRunbook')}</p>
          )}

          {answers.length > 0 && (
            <PageSection title={t('aiRunbookCopilot.recentAnswers')}>
              <div className="space-y-2">
                {answers.map((a, i) => (
                  <Card key={i}>
                    <CardBody className="p-3">
                      <div className="flex items-start gap-2">
                        <MessageSquare size={13} className="text-indigo-500 mt-0.5 flex-shrink-0" />
                        <div>
                          <p className="text-xs font-medium text-gray-700 dark:text-gray-300">{a.question}</p>
                          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{a.answer}</p>
                        </div>
                      </div>
                    </CardBody>
                  </Card>
                ))}
              </div>
            </PageSection>
          )}

          <div className="flex items-start gap-2 text-xs text-gray-400 p-2">
            <Info size={12} className="mt-0.5 flex-shrink-0" />
            <span>{t('aiRunbookCopilot.auditNote')}</span>
          </div>
        </div>
      </div>
    </PageContainer>
  );
}
