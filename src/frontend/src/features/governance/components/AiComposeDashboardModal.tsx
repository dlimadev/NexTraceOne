import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Bot, Sparkles, CheckCircle, XCircle, LayoutDashboard, AlertTriangle } from 'lucide-react';
import { notebooksApi, type AiDashboardProposal, type ProposedWidgetDto } from '../api/notebooks';
import { Modal } from '../../../components/Modal';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';

interface Props {
  open: boolean;
  onClose: () => void;
  tenantId: string;
  userId: string;
  persona: string;
  teamId?: string;
  environmentId?: string;
}

export function AiComposeDashboardModal({
  open,
  onClose,
  tenantId,
  userId,
  persona,
  teamId,
  environmentId,
}: Props) {
  const { t } = useTranslation();
  const qc = useQueryClient();

  const [prompt, setPrompt] = React.useState('');
  const [proposal, setProposal] = React.useState<AiDashboardProposal | null>(null);
  const [removedWidgets, setRemovedWidgets] = React.useState<Set<number>>(new Set());

  const composeMutation = useMutation({
    mutationFn: () =>
      notebooksApi.composeAiDashboard({
        prompt,
        tenantId,
        userId,
        persona,
        teamId: teamId ?? null,
        environmentId: environmentId ?? null,
      }),
    onSuccess(data) {
      setProposal(data);
      setRemovedWidgets(new Set());
    },
  });

  const acceptMutation = useMutation({
    mutationFn: () => {
      if (!proposal) throw new Error('No proposal');
      const widgets = proposal.proposedWidgets
        .filter((_, i) => !removedWidgets.has(i))
        .map((w) => ({
          widgetId: crypto.randomUUID(),
          type: w.widgetType,
          position: { x: w.gridX, y: w.gridY, width: w.gridWidth, height: w.gridHeight },
          config: { customTitle: w.title ?? undefined },
        }));

      return fetch('/api/v1/governance/dashboards', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: proposal.proposedTitle,
          description: `AI-composed dashboard (${persona})`,
          layout: proposal.proposedLayout,
          persona,
          widgets,
          tenantId,
          userId,
        }),
      }).then((r) => r.json());
    },
    onSuccess() {
      qc.invalidateQueries({ queryKey: ['dashboards'] });
      onClose();
      setProposal(null);
      setPrompt('');
    },
  });

  const handleClose = () => {
    onClose();
    setProposal(null);
    setPrompt('');
    setRemovedWidgets(new Set());
  };

  const toggleWidget = (idx: number) => {
    setRemovedWidgets((prev) => {
      const next = new Set(prev);
      next.has(idx) ? next.delete(idx) : next.add(idx);
      return next;
    });
  };

  const activeWidgets = proposal?.proposedWidgets.filter((_, i) => !removedWidgets.has(i)) ?? [];

  return (
    <Modal open={open} onClose={handleClose} size="lg">
      <div className="space-y-5">
        {/* Title */}
        <div className="flex items-center gap-2">
          <div className="p-2 bg-indigo-100 dark:bg-indigo-900 rounded-lg">
            <Bot className="h-5 w-5 text-indigo-600 dark:text-indigo-400" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
              {t('aiCompose.title')}
            </h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">{t('aiCompose.subtitle')}</p>
          </div>
        </div>

        {/* Prompt input */}
        {!proposal && (
          <div className="space-y-3">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('aiCompose.promptLabel')}
            </label>
            <textarea
              className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-white placeholder-gray-400 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"
              rows={4}
              placeholder={t('aiCompose.promptPlaceholder')}
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
            />
            <div className="flex justify-end gap-2">
              <Button variant="outline" size="sm" onClick={handleClose}>
                {t('aiCompose.reject')}
              </Button>
              <Button
                size="sm"
                onClick={() => composeMutation.mutate()}
                disabled={!prompt.trim() || composeMutation.isPending}
                className="flex items-center gap-2"
              >
                <Sparkles className="h-4 w-4" />
                {composeMutation.isPending ? t('aiCompose.composing') : t('aiCompose.title')}
              </Button>
            </div>
          </div>
        )}

        {/* Proposal */}
        {proposal && (
          <div className="space-y-4">
            {/* Simulated banner */}
            {proposal.isSimulated && (
              <div className="flex items-start gap-2 p-3 bg-amber-50 dark:bg-amber-900/20 rounded-lg border border-amber-200 dark:border-amber-700">
                <AlertTriangle className="h-4 w-4 text-amber-500 mt-0.5 shrink-0" />
                <div className="text-xs text-amber-700 dark:text-amber-300">
                  <div className="font-medium">{t('aiCompose.simulatedBanner')}</div>
                  <div>{t('aiCompose.simulatedNote')}</div>
                </div>
              </div>
            )}

            {/* Proposed title */}
            <div>
              <div className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">
                {t('aiCompose.proposalTitle')}
              </div>
              <div className="text-base font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                <LayoutDashboard className="h-4 w-4 text-indigo-500" />
                {proposal.proposedTitle}
              </div>
            </div>

            {/* Variables */}
            {proposal.proposedVariables.length > 0 && (
              <div>
                <div className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                  {t('aiCompose.variablesSection')}
                </div>
                <div className="flex flex-wrap gap-2">
                  {proposal.proposedVariables.map((v) => (
                    <Badge key={v.key} variant="purple" size="sm">
                      {v.key}
                      {v.defaultValue ? ` = ${v.defaultValue}` : ''}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            {/* Widgets */}
            <div>
              <div className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
                {t('aiCompose.widgetsSection', { count: activeWidgets.length })}
              </div>
              <div className="space-y-2 max-h-64 overflow-y-auto pr-1">
                {proposal.proposedWidgets.map((w, i) => (
                  <WidgetProposalRow
                    key={i}
                    widget={w}
                    removed={removedWidgets.has(i)}
                    onToggle={() => toggleWidget(i)}
                  />
                ))}
              </div>
            </div>

            {/* Actions */}
            <div className="flex justify-between items-center pt-2 border-t border-gray-200 dark:border-gray-700">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setProposal(null)}
              >
                ← Back
              </Button>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={handleClose}>
                  {t('aiCompose.reject')}
                </Button>
                <Button
                  size="sm"
                  onClick={() => acceptMutation.mutate()}
                  disabled={acceptMutation.isPending || activeWidgets.length === 0}
                  className="flex items-center gap-2"
                >
                  <CheckCircle className="h-4 w-4" />
                  {acceptMutation.isPending ? t('common.creating', 'Creating…') : t('aiCompose.accept')}
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
}

function WidgetProposalRow({
  widget,
  removed,
  onToggle,
}: {
  widget: ProposedWidgetDto;
  removed: boolean;
  onToggle: () => void;
}) {
  return (
    <div
      className={`flex items-center gap-3 p-2 rounded-lg border transition-colors cursor-pointer ${
        removed
          ? 'border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 opacity-50'
          : 'border-indigo-200 dark:border-indigo-700 bg-indigo-50 dark:bg-indigo-900/20'
      }`}
      onClick={onToggle}
    >
      {removed ? (
        <XCircle className="h-4 w-4 text-gray-400 shrink-0" />
      ) : (
        <CheckCircle className="h-4 w-4 text-indigo-500 shrink-0" />
      )}
      <div className="flex-1 min-w-0">
        <div className="text-sm font-medium text-gray-900 dark:text-white truncate">
          {widget.title ?? widget.widgetType}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">
          {widget.widgetType}
          {widget.serviceFilter ? ` · ${widget.serviceFilter}` : ''}
          {` · ${widget.gridWidth}×${widget.gridHeight}`}
        </div>
      </div>
    </div>
  );
}
