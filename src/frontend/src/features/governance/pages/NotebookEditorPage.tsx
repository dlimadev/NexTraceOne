import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Save,
  Plus,
  Trash2,
  ChevronUp,
  ChevronDown,
  Play,
  BookOpen,
  ArrowLeft,
  FileText,
  Code2,
  LayoutDashboard,
  Zap,
  Bot,
} from 'lucide-react';
import { notebooksApi, type NotebookCellDto, type UpdateNotebookRequest } from '../api/notebooks';
import { useAuth } from '../../../contexts/AuthContext';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { Card } from '../../../components/Card';

const CELL_ICONS: Record<string, React.ReactNode> = {
  Markdown: <FileText className="h-4 w-4" />,
  Query: <Code2 className="h-4 w-4" />,
  Widget: <LayoutDashboard className="h-4 w-4" />,
  Action: <Zap className="h-4 w-4" />,
  Ai: <Bot className="h-4 w-4" />,
};

interface LocalCell {
  localId: string;
  cellId?: string;
  cellType: NotebookCellDto['cellType'];
  sortOrder: number;
  content: string;
  outputJson?: string | null;
  isCollapsed: boolean;
}

function makeCellId() {
  return Math.random().toString(36).slice(2);
}

export function NotebookEditorPage() {
  const { t } = useTranslation();
  const { notebookId } = useParams<{ notebookId: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { user } = useAuth();
  const tenantId = user?.tenantId ?? '';
  const isNew = !notebookId || notebookId === 'new';

  const [title, setTitle] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [cells, setCells] = React.useState<LocalCell[]>([]);
  const [dirty, setDirty] = React.useState(false);
  const [savedAt, setSavedAt] = React.useState<Date | null>(null);

  const { isLoading, isError, data: notebookData } = useQuery({
    queryKey: ['notebook', notebookId, tenantId],
    queryFn: () => notebooksApi.get(notebookId!, tenantId),
    enabled: !isNew && !!tenantId,
  });

  React.useEffect(() => {
    if (notebookData) {
      const nb = notebookData;
      setTitle(nb.title);
      setDescription(nb.description ?? '');
      setCells(
        nb.cells.map((c: { cellId?: string; cellType: LocalCell['cellType']; sortOrder: number; content: string; outputJson?: string | null; isCollapsed: boolean }) => ({
          localId: makeCellId(),
          cellId: c.cellId,
          cellType: c.cellType,
          sortOrder: c.sortOrder,
          content: c.content,
          outputJson: c.outputJson,
          isCollapsed: c.isCollapsed,
        })),
      );
    }
  }, [notebookData]);

  const createMutation = useMutation({
    mutationFn: () =>
      notebooksApi.create({
        title,
        description: description || null,
        tenantId,
        userId: user?.id ?? '',
        persona: user?.persona ?? 'Engineer',
        initialCells: cells.map((c) => ({ cellType: c.cellType, sortOrder: c.sortOrder, content: c.content })),
      }),
    onSuccess(res) {
      setDirty(false);
      setSavedAt(new Date());
      qc.invalidateQueries({ queryKey: ['notebooks'] });
      navigate(`/governance/notebooks/${res.notebookId}`, { replace: true });
    },
  });

  const updateMutation = useMutation({
    mutationFn: () => {
      const req: UpdateNotebookRequest = {
        tenantId,
        userId: user?.id ?? '',
        title,
        description: description || null,
        teamId: null,
        cells: cells.map((c) => ({
          cellId: c.cellId,
          cellType: c.cellType,
          sortOrder: c.sortOrder,
          content: c.content,
        })),
      };
      return notebooksApi.update(notebookId!, req);
    },
    onSuccess() {
      setDirty(false);
      setSavedAt(new Date());
      qc.invalidateQueries({ queryKey: ['notebook', notebookId] });
    },
  });

  const handleSave = () => {
    if (isNew) createMutation.mutate();
    else updateMutation.mutate();
  };

  const addCell = (type: LocalCell['cellType']) => {
    const nextOrder = cells.length > 0 ? Math.max(...cells.map((c) => c.sortOrder)) + 1 : 1;
    setCells((prev) => [
      ...prev,
      { localId: makeCellId(), cellType: type, sortOrder: nextOrder, content: '', isCollapsed: false },
    ]);
    setDirty(true);
  };

  const updateCell = (localId: string, content: string) => {
    setCells((prev) => prev.map((c) => (c.localId === localId ? { ...c, content } : c)));
    setDirty(true);
  };

  const removeCell = (localId: string) => {
    setCells((prev) => prev.filter((c) => c.localId !== localId));
    setDirty(true);
  };

  const toggleCollapse = (localId: string) => {
    setCells((prev) => prev.map((c) => (c.localId === localId ? { ...c, isCollapsed: !c.isCollapsed } : c)));
  };

  const moveCell = (localId: string, dir: 'up' | 'down') => {
    setCells((prev) => {
      const idx = prev.findIndex((c) => c.localId === localId);
      if (idx < 0) return prev;
      const target = dir === 'up' ? idx - 1 : idx + 1;
      if (target < 0 || target >= prev.length) return prev;
      const copy = [...prev];
      [copy[idx], copy[target]] = [copy[target], copy[idx]];
      return copy.map((c, i) => ({ ...c, sortOrder: i + 1 }));
    });
    setDirty(true);
  };

  const isSaving = createMutation.isPending || updateMutation.isPending;

  if (!isNew && isLoading) return <PageLoadingState />;
  if (!isNew && isError) return <PageErrorState />;

  return (
    <div className="max-w-4xl mx-auto space-y-4 pb-16">
      {/* Toolbar */}
      <div className="flex items-center gap-3 sticky top-0 z-20 bg-white dark:bg-gray-900 py-3 border-b border-gray-200 dark:border-gray-700">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)} className="gap-1">
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <BookOpen className="h-5 w-5 text-indigo-500" />
        <input
          className="flex-1 text-lg font-semibold bg-transparent border-none outline-none text-gray-900 dark:text-white placeholder-gray-400"
          placeholder={t('notebook.edit')}
          value={title}
          onChange={(e) => { setTitle(e.target.value); setDirty(true); }}
        />
        {dirty && (
          <Badge variant="yellow" size="sm">{t('common.unsaved', 'Unsaved')}</Badge>
        )}
        {savedAt && !dirty && (
          <span className="text-xs text-gray-400">
            {t('notebook.savedAt', { time: savedAt.toLocaleTimeString() })}
          </span>
        )}
        <Button
          size="sm"
          onClick={handleSave}
          disabled={isSaving || !title.trim()}
          className="flex items-center gap-2"
        >
          <Save className="h-4 w-4" />
          {isSaving ? t('common.saving', 'Saving…') : t('common.save', 'Save')}
        </Button>
      </div>

      {/* Description */}
      <textarea
        className="w-full rounded-lg border border-gray-200 dark:border-gray-700 bg-transparent px-3 py-2 text-sm text-gray-700 dark:text-gray-300 resize-none"
        rows={2}
        placeholder={t('notebook.edit')}
        value={description}
        onChange={(e) => { setDescription(e.target.value); setDirty(true); }}
      />

      {/* Cells */}
      <div className="space-y-3">
        {cells.map((cell, idx) => (
          <Card key={cell.localId} className="overflow-hidden">
            {/* Cell header */}
            <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
              <span className="text-gray-500">{CELL_ICONS[cell.cellType]}</span>
              <span className="text-xs font-medium text-gray-600 dark:text-gray-400 flex-1">
                {t(`notebook.cellType${cell.cellType}`)}
              </span>
              <div className="flex items-center gap-1">
                <button
                  onClick={() => moveCell(cell.localId, 'up')}
                  disabled={idx === 0}
                  className="p-1 rounded hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-30"
                >
                  <ChevronUp className="h-3 w-3" />
                </button>
                <button
                  onClick={() => moveCell(cell.localId, 'down')}
                  disabled={idx === cells.length - 1}
                  className="p-1 rounded hover:bg-gray-200 dark:hover:bg-gray-700 disabled:opacity-30"
                >
                  <ChevronDown className="h-3 w-3" />
                </button>
                {cell.cellType === 'Query' && (
                  <button className="p-1 rounded hover:bg-gray-200 dark:hover:bg-gray-700 text-indigo-500">
                    <Play className="h-3 w-3" />
                  </button>
                )}
                <button
                  onClick={() => toggleCollapse(cell.localId)}
                  className="p-1 rounded hover:bg-gray-200 dark:hover:bg-gray-700"
                >
                  {cell.isCollapsed
                    ? <ChevronDown className="h-3 w-3" />
                    : <ChevronUp className="h-3 w-3" />}
                </button>
                <button
                  onClick={() => removeCell(cell.localId)}
                  className="p-1 rounded hover:bg-red-100 dark:hover:bg-red-900 text-red-500"
                >
                  <Trash2 className="h-3 w-3" />
                </button>
              </div>
            </div>

            {/* Cell content */}
            {!cell.isCollapsed && (
              <div className="p-3">
                <textarea
                  className="w-full bg-transparent font-mono text-sm text-gray-800 dark:text-gray-200 outline-none resize-none min-h-[80px]"
                  placeholder={`${t(`notebook.cellType${cell.cellType}`)} content…`}
                  value={cell.content}
                  onChange={(e) => updateCell(cell.localId, e.target.value)}
                  rows={cell.cellType === 'Markdown' ? 4 : 3}
                />
                {cell.outputJson && (
                  <div className="mt-2 p-2 bg-gray-50 dark:bg-gray-800 rounded text-xs font-mono text-gray-600 dark:text-gray-400 max-h-40 overflow-auto">
                    {cell.outputJson}
                  </div>
                )}
              </div>
            )}
          </Card>
        ))}
      </div>

      {/* Add cell bar */}
      <div className="flex items-center gap-2 pt-2">
        <span className="text-xs text-gray-400 mr-1">{t('notebook.addCell')}</span>
        {(['Markdown', 'Query', 'Widget', 'Action', 'Ai'] as const).map((type) => (
          <button
            key={type}
            onClick={() => addCell(type)}
            className="flex items-center gap-1 px-2 py-1 text-xs rounded border border-dashed border-gray-300 dark:border-gray-600 hover:border-indigo-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
          >
            {CELL_ICONS[type]}
            {t(`notebook.cellType${type}`)}
          </button>
        ))}
      </div>
    </div>
  );
}
