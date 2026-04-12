import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Download } from 'lucide-react';
import Modal from './Modal';
import { Button } from './Button';

export type ExportFormat = 'csv' | 'json' | 'pdf';

interface ExportColumn {
  key: string;
  label: string;
}

interface ExportModalProps {
  /** Whether the modal is open. */
  open: boolean;
  /** Close callback. */
  onClose: () => void;
  /** Available columns for selection. If empty, no column selector is shown. */
  columns?: ExportColumn[];
  /** Called when user confirms export. */
  onExport: (format: ExportFormat, selectedColumns?: string[]) => void | Promise<void>;
  /** Whether the export operation is in progress. */
  isExporting?: boolean;
}

/**
 * Modal reutilizável para exportação de dados em CSV, JSON ou PDF.
 * Permite selecção de formato e colunas (quando `columns` é fornecido).
 */
export function ExportModal({ open, onClose, columns, onExport, isExporting }: ExportModalProps) {
  const { t } = useTranslation();
  const [format, setFormat] = useState<ExportFormat>('csv');
  const [selectedColumns, setSelectedColumns] = useState<string[]>(
    columns?.map(c => c.key) ?? []
  );

  const handleToggleColumn = (key: string) => {
    setSelectedColumns(prev =>
      prev.includes(key) ? prev.filter(k => k !== key) : [...prev, key]
    );
  };

  const handleExport = async () => {
    await onExport(format, columns ? selectedColumns : undefined);
    onClose();
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('export.title')}
      description={t('export.description')}
      size="md"
      footer={
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose} disabled={isExporting}>
            {t('common.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleExport}
            disabled={isExporting || (columns !== undefined && selectedColumns.length === 0)}
            className="flex items-center gap-2"
          >
            <Download className="w-4 h-4" />
            {isExporting ? t('export.exporting') : t('export.download')}
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        {/* Format selector */}
        <div>
          <label className="block text-sm font-medium text-slate-300 mb-2">
            {t('export.format')}
          </label>
          <div className="flex gap-3">
            {(['csv', 'json', 'pdf'] as ExportFormat[]).map(f => (
              <button
                key={f}
                type="button"
                onClick={() => setFormat(f)}
                className={`px-4 py-2 rounded-lg text-sm font-medium border transition-colors ${
                  format === f
                    ? 'border-brand-blue bg-brand-blue/10 text-brand-blue'
                    : 'border-slate-700 text-slate-400 hover:border-slate-500'
                }`}
              >
                {f.toUpperCase()}
              </button>
            ))}
          </div>
        </div>

        {/* Column selector (optional) */}
        {columns && columns.length > 0 && (
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              {t('export.columns')}
            </label>
            <div className="grid grid-cols-2 gap-2 max-h-48 overflow-y-auto pr-1">
              {columns.map(col => (
                <label
                  key={col.key}
                  className="flex items-center gap-2 text-sm text-slate-300 cursor-pointer"
                >
                  <input
                    type="checkbox"
                    checked={selectedColumns.includes(col.key)}
                    onChange={() => handleToggleColumn(col.key)}
                    className="accent-brand-blue"
                  />
                  {col.label}
                </label>
              ))}
            </div>
            <p className="mt-1 text-xs text-slate-500">
              {t('export.columnsSelected', { count: selectedColumns.length, total: columns.length })}
            </p>
          </div>
        )}
      </div>
    </Modal>
  );
}
