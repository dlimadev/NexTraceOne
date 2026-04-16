import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  ClipboardCheck,
  CheckCircle2,
  Circle,
  AlertCircle,
  Save,
  User,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { workflowApi } from '../api/workflow';
import type { ChecklistItemInput, ChecklistEvidenceResponse } from '../api/workflow';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

interface ChecklistItem extends ChecklistItemInput {
  id: string;
}

function generateId() {
  return Math.random().toString(36).slice(2, 10);
}

const DEFAULT_ITEMS: ChecklistItem[] = [
  { id: generateId(), name: '', completed: false, notes: null },
  { id: generateId(), name: '', completed: false, notes: null },
  { id: generateId(), name: '', completed: false, notes: null },
];

/**
 * Página de execução de checklist de release vinculado ao Evidence Pack.
 * Permite registar o estado de conclusão de cada item do checklist
 * como evidência formal da release no workflow.
 * Gap 11: Checklist linked to Release Evidence Pack (4.9).
 */
export function ReleaseChecklistExecutionPage() {
  const { t } = useTranslation();

  const [instanceId, setInstanceId] = useState('');
  const [checklistName, setChecklistName] = useState('');
  const [executedBy, setExecutedBy] = useState('');
  const [items, setItems] = useState<ChecklistItem[]>(DEFAULT_ITEMS);
  const [result, setResult] = useState<ChecklistEvidenceResponse | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const { mutate: submitChecklist, isPending } = useMutation({
    mutationFn: () =>
      workflowApi.recordChecklistEvidence(instanceId, {
        workflowInstanceId: instanceId,
        checklistName,
        executedBy,
        items: items.filter((i) => i.name.trim()),
      }),
    onSuccess: (data) => {
      setResult(data);
      setFormError(null);
    },
    onError: () => {
      setFormError(t('checklist.submitError'));
    },
  });

  function addItem() {
    setItems((prev) => [
      ...prev,
      { id: generateId(), name: '', completed: false, notes: null },
    ]);
  }

  function removeItem(id: string) {
    setItems((prev) => prev.filter((i) => i.id !== id));
  }

  function toggleItem(id: string) {
    setItems((prev) =>
      prev.map((i) => (i.id === id ? { ...i, completed: !i.completed } : i)),
    );
  }

  function updateItemName(id: string, name: string) {
    setItems((prev) => prev.map((i) => (i.id === id ? { ...i, name } : i)));
  }

  function updateItemNotes(id: string, notes: string) {
    setItems((prev) =>
      prev.map((i) => (i.id === id ? { ...i, notes: notes || null } : i)),
    );
  }

  function handleSubmit() {
    const uuidPattern =
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (!uuidPattern.test(instanceId.trim())) {
      setFormError(t('checklist.invalidInstanceId'));
      return;
    }
    if (!checklistName.trim()) {
      setFormError(t('checklist.nameRequired'));
      return;
    }
    if (!executedBy.trim()) {
      setFormError(t('checklist.executedByRequired'));
      return;
    }
    const validItems = items.filter((i) => i.name.trim());
    if (validItems.length === 0) {
      setFormError(t('checklist.noItems'));
      return;
    }
    setFormError(null);
    submitChecklist();
  }

  const completedCount = items.filter((i) => i.completed && i.name.trim()).length;
  const totalCount = items.filter((i) => i.name.trim()).length;
  const completionRate = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

  return (
    <PageContainer>
      <PageHeader
        title={t('checklist.title')}
        subtitle={t('checklist.subtitle')}
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main checklist form */}
        <div className="lg:col-span-2 space-y-6">
          {/* Workflow context */}
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('checklist.contextTitle')}</h3>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                <div>
                  <label className="block text-xs font-medium text-muted mb-1">
                    {t('checklist.instanceIdLabel')}
                  </label>
                  <input
                    className={INPUT_CLS}
                    placeholder={t('checklist.instanceIdPlaceholder')}
                    value={instanceId}
                    onChange={(e) => setInstanceId(e.target.value)}
                  />
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-muted mb-1">
                      {t('checklist.nameLabel')}
                    </label>
                    <input
                      className={INPUT_CLS}
                      placeholder={t('checklist.namePlaceholder')}
                      value={checklistName}
                      onChange={(e) => setChecklistName(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-muted mb-1">
                      {t('checklist.executedByLabel')}
                    </label>
                    <div className="relative">
                      <User size={14} className="absolute left-3 top-2.5 text-muted" />
                      <input
                        className={`${INPUT_CLS} pl-8`}
                        placeholder={t('checklist.executedByPlaceholder')}
                        value={executedBy}
                        onChange={(e) => setExecutedBy(e.target.value)}
                      />
                    </div>
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Checklist items */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-heading">{t('checklist.itemsTitle')}</h3>
                <Button variant="ghost" size="sm" onClick={addItem}>
                  + {t('checklist.addItem')}
                </Button>
              </div>
            </CardHeader>
            <CardBody>
              <div className="space-y-3">
                {items.map((item, idx) => (
                  <div key={item.id} className="border border-edge rounded-md p-3">
                    <div className="flex items-start gap-3">
                      <button
                        onClick={() => toggleItem(item.id)}
                        className="mt-2 shrink-0"
                        aria-label={item.completed ? t('checklist.markIncomplete') : t('checklist.markComplete')}
                      >
                        {item.completed ? (
                          <CheckCircle2 size={18} className="text-success" />
                        ) : (
                          <Circle size={18} className="text-muted" />
                        )}
                      </button>
                      <div className="flex-1 space-y-2">
                        <input
                          className={INPUT_CLS}
                          placeholder={t('checklist.itemNamePlaceholder', { num: idx + 1 })}
                          value={item.name}
                          onChange={(e) => updateItemName(item.id, e.target.value)}
                        />
                        <input
                          className={INPUT_CLS}
                          placeholder={t('checklist.itemNotesPlaceholder')}
                          value={item.notes ?? ''}
                          onChange={(e) => updateItemNotes(item.id, e.target.value)}
                        />
                      </div>
                      <button
                        onClick={() => removeItem(item.id)}
                        className="mt-2 shrink-0 text-muted hover:text-critical transition-colors"
                        aria-label={t('checklist.removeItem')}
                      >
                        ×
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {formError && (
                <div className="mt-4 flex items-start gap-2 p-3 bg-critical/10 rounded-md border border-critical/20">
                  <AlertCircle size={14} className="text-critical mt-0.5 shrink-0" />
                  <p className="text-xs text-critical">{formError}</p>
                </div>
              )}

              <div className="mt-4 flex items-center justify-between">
                <p className="text-xs text-muted">
                  {t('checklist.progress', { completed: completedCount, total: totalCount, rate: completionRate })}
                </p>
                <Button
                  variant="primary"
                  onClick={handleSubmit}
                  disabled={isPending}
                >
                  <Save size={16} />
                  {isPending ? t('common.saving') : t('checklist.submitButton')}
                </Button>
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Progress sidebar */}
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('checklist.progressTitle')}</h3>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                {/* Progress bar */}
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-xs text-muted">{t('checklist.completion')}</span>
                    <span className="text-xs font-medium text-heading">{completionRate}%</span>
                  </div>
                  <div className="w-full bg-surface rounded-full h-2">
                    <div
                      className="bg-accent rounded-full h-2 transition-all"
                      style={{ width: `${completionRate}%` }}
                    />
                  </div>
                </div>
                <div className="text-center py-2">
                  <p className="text-2xl font-bold text-heading">{completedCount}</p>
                  <p className="text-xs text-muted">
                    {t('checklist.completedOf', { total: totalCount })}
                  </p>
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Result */}
          {result && (
            <Card className="border border-success/30">
              <CardBody>
                <div className="flex items-start gap-2">
                  <CheckCircle2 size={16} className="text-success mt-0.5 shrink-0" />
                  <div className="space-y-2">
                    <p className="text-sm font-semibold text-success">
                      {t('checklist.recordedTitle')}
                    </p>
                    <div className="space-y-1 text-xs text-muted">
                      <p>
                        {t('checklist.evidenceCompleteness')}: {' '}
                        <span className="font-medium text-heading">
                          {result.evidenceCompletenessPercentage.toFixed(0)}%
                        </span>
                      </p>
                      <p>
                        {t('checklist.checklistCompletion')}: {' '}
                        <span className="font-medium text-heading">
                          {result.completionRate.toFixed(0)}%
                        </span>
                      </p>
                      <p className="font-mono text-xs break-all text-muted">
                        {t('checklist.evidencePackId')}: {result.evidencePackId}
                      </p>
                    </div>
                    <Badge variant={result.completionRate >= 100 ? 'success' : result.completionRate >= 75 ? 'warning' : 'danger'}>
                      {result.completedItems}/{result.totalItems} {t('checklist.itemsCompleted')}
                    </Badge>
                  </div>
                </div>
              </CardBody>
            </Card>
          )}
        </div>
      </div>
    </PageContainer>
  );
}
