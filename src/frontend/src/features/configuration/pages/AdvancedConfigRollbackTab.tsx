/**
 * Tab "Rollback & Restore" da AdvancedConfigurationConsolePage.
 *
 * Permite pesquisar uma chave de configuração, visualizar o seu historial
 * de versões e restaurar um valor anterior. Todas as acções são auditadas.
 */
import { useTranslation } from 'react-i18next';
import { Search, Clock, RotateCcw } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type {
  ConfigurationDefinitionDto,
  ConfigurationAuditEntryDto,
} from '../types';

// ── Props ──────────────────────────────────────────────────────────────

export interface AdvancedConfigRollbackTabProps {
  selectedAuditKey: string | null;
  auditData: ConfigurationAuditEntryDto[] | undefined;
  definitions: ConfigurationDefinitionDto[] | undefined;
  setSearchQuery: (q: string) => void;
  setSelectedAuditKey: (key: string | null) => void;
}

// ── Component ──────────────────────────────────────────────────────────

export function AdvancedConfigRollbackTab({
  selectedAuditKey,
  auditData,
  definitions,
  setSearchQuery,
  setSelectedAuditKey,
}: AdvancedConfigRollbackTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <Card>
        <CardBody>
          <div className="flex items-center gap-2 mb-4">
            <RotateCcw className="w-5 h-5 text-brand-600" />
            <h3 className="font-semibold">{t('advancedConfig.rollback.title', 'Configuration Rollback')}</h3>
          </div>
          <p className="text-sm text-faded mb-6">
            {t('advancedConfig.rollback.description', 'Select a configuration key to view its version history and restore a previous value. All rollbacks are audited and validated.')}
          </p>

          <div className="relative mb-4">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-muted" />
            <input
              type="text"
              placeholder={t('advancedConfig.rollback.searchKey', 'Search key to rollback...')}
              className="w-full pl-10 pr-4 py-2 border border-edge rounded-lg bg-card text-sm"
              onChange={(e) => {
                setSearchQuery(e.target.value);
                if (e.target.value.length > 3) {
                  const found = definitions?.find((d: ConfigurationDefinitionDto) => d.key === e.target.value);
                  if (found) setSelectedAuditKey(found.key);
                }
              }}
            />
          </div>

          {selectedAuditKey && auditData && auditData.length > 0 && (
            <div className="space-y-3">
              <h4 className="text-sm font-medium text-body">
                {t('advancedConfig.rollback.historyFor', 'Version History for')} <code className="text-brand-600">{selectedAuditKey}</code>
              </h4>
              {auditData.map((entry, idx) => (
                <div key={idx} className="flex items-start gap-3 p-3 bg-subtle rounded-lg">
                  <Clock className="w-4 h-4 text-muted mt-0.5 flex-shrink-0" />
                  <div className="flex-1">
                    <div className="flex items-center gap-2 text-xs text-faded">
                      <span>{new Date(entry.changedAt).toLocaleString()}</span>
                      <span>•</span>
                      <span>{entry.changedBy}</span>
                      <Badge variant="default" className="text-xs">{entry.action}</Badge>
                    </div>
                    <div className="grid grid-cols-2 gap-2 mt-2 text-xs">
                      {entry.previousValue !== null && (
                        <div className="p-2 bg-critical/15 rounded">
                          <span className="text-critical">Previous:</span>
                          <pre className="mt-1 overflow-x-auto">{entry.isSensitive ? '***' : entry.previousValue}</pre>
                        </div>
                      )}
                      <div className="p-2 bg-success/15 rounded">
                        <span className="text-success">New:</span>
                        <pre className="mt-1 overflow-x-auto">{entry.isSensitive ? '***' : entry.newValue}</pre>
                      </div>
                    </div>
                    {entry.changeReason && (
                      <p className="text-xs text-faded mt-1 italic">"{entry.changeReason}"</p>
                    )}
                  </div>
                  {idx > 0 && (
                    <button className="flex items-center gap-1 px-2 py-1 text-xs text-brand-600 hover:bg-brand-50 rounded transition-colors">
                      <RotateCcw className="w-3 h-3" />
                      {t('advancedConfig.rollback.restore', 'Restore')}
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}

          {(!selectedAuditKey || !auditData || auditData.length === 0) && (
            <div className="text-center py-8 text-faded">
              <RotateCcw className="w-8 h-8 mx-auto mb-2 opacity-50" />
              <p>{t('advancedConfig.rollback.selectKey', 'Search and select a configuration key to view its version history.')}</p>
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
