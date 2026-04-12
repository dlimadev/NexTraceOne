/**
 * Tab "History & Timeline" da AdvancedConfigurationConsolePage.
 *
 * Exibe a timeline de todas as alterações de configuração para uma
 * chave seleccionada, com filtragem por chave, utilizador e período.
 */
import { useTranslation } from 'react-i18next';
import { Search, History } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { ConfigurationAuditEntryDto } from '../types';

// ── Props ──────────────────────────────────────────────────────────────

export interface AdvancedConfigHistoryTabProps {
  selectedAuditKey: string | null;
  auditData: ConfigurationAuditEntryDto[] | undefined;
  setSelectedAuditKey: (key: string | null) => void;
}

// ── Component ──────────────────────────────────────────────────────────

export function AdvancedConfigHistoryTab({
  selectedAuditKey,
  auditData,
  setSelectedAuditKey,
}: AdvancedConfigHistoryTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <Card>
        <CardBody>
          <div className="flex items-center gap-2 mb-4">
            <History className="w-5 h-5 text-brand-600" />
            <h3 className="font-semibold">{t('advancedConfig.history.title', 'Configuration Change Timeline')}</h3>
          </div>
          <p className="text-sm text-faded mb-4">
            {t('advancedConfig.history.description', 'View all configuration changes across domains. Filter by key, user, or time period.')}
          </p>

          <div className="relative mb-4">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-muted" />
            <input
              type="text"
              placeholder={t('advancedConfig.history.searchPlaceholder', 'Search by key...')}
              className="w-full pl-10 pr-4 py-2 border border-edge rounded-lg bg-card text-sm"
              onChange={(e) => {
                if (e.target.value.length > 2) setSelectedAuditKey(e.target.value);
              }}
            />
          </div>

          {selectedAuditKey && auditData && auditData.length > 0 && (
            <div className="space-y-2">
              {auditData.map((entry, idx) => (
                <div key={idx} className="flex items-start gap-3 py-3 border-b border-edge last:border-0">
                  <div className="w-2 h-2 rounded-full bg-brand-500 mt-2 flex-shrink-0" />
                  <div className="flex-1">
                    <div className="flex items-center gap-2 text-xs">
                      <span className="font-medium text-body">{entry.key}</span>
                      <Badge variant={entry.action === 'Set' ? 'success' : entry.action === 'Remove' ? 'danger' : 'default'} className="text-xs">{entry.action}</Badge>
                      <span className="text-muted">{entry.scope}</span>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-faded mt-1">
                      <span>{entry.changedBy}</span>
                      <span>•</span>
                      <span>{new Date(entry.changedAt).toLocaleString()}</span>
                      {entry.changeReason && <span className="italic">— {entry.changeReason}</span>}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {(!selectedAuditKey || !auditData || auditData.length === 0) && (
            <div className="text-center py-8 text-faded">
              <History className="w-8 h-8 mx-auto mb-2 opacity-50" />
              <p>{t('advancedConfig.history.empty', 'Enter a configuration key to view its change timeline.')}</p>
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
