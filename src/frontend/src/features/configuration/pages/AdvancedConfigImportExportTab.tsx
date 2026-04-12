/**
 * Tab "Import / Export" da AdvancedConfigurationConsolePage.
 *
 * Permite exportar configurações filtradas por scope e domínio como JSON,
 * e importar ficheiros de exportação anteriores com validação prévia.
 */
import { useTranslation } from 'react-i18next';
import { Download, Upload, AlertTriangle, Info } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { DOMAINS } from './AdvancedConfigConsoleTypes';
import type { ConfigDomain } from './AdvancedConfigConsoleTypes';
import type { ConfigurationScope } from '../types';

// ── Props ──────────────────────────────────────────────────────────────

export interface AdvancedConfigImportExportTabProps {
  selectedScope: ConfigurationScope;
  activeDomain: ConfigDomain;
  setSelectedScope: (scope: ConfigurationScope) => void;
  setActiveDomain: (domain: ConfigDomain) => void;
  onExport: () => void;
}

// ── Component ──────────────────────────────────────────────────────────

export function AdvancedConfigImportExportTab({
  selectedScope,
  activeDomain,
  setSelectedScope,
  setActiveDomain,
  onExport,
}: AdvancedConfigImportExportTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Export */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Download className="w-5 h-5 text-brand-600" />
              <h3 className="font-semibold">{t('advancedConfig.export.title', 'Export Configuration')}</h3>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-faded mb-4">
              {t('advancedConfig.export.description', 'Export configuration definitions and effective values as a validated JSON file. Sensitive values are automatically masked.')}
            </p>
            <div className="space-y-3">
              <div>
                <label className="text-xs text-faded mb-1 block">{t('advancedConfig.export.scope', 'Scope')}</label>
                <select
                  value={selectedScope}
                  onChange={(e) => setSelectedScope(e.target.value as ConfigurationScope)}
                  className="w-full px-3 py-2 border border-edge rounded-lg text-sm bg-card"
                >
                  <option value="System">System</option>
                  <option value="Tenant">Tenant</option>
                  <option value="Environment">Environment</option>
                </select>
              </div>
              <div>
                <label className="text-xs text-faded mb-1 block">{t('advancedConfig.export.domain', 'Domain')}</label>
                <select
                  value={activeDomain}
                  onChange={(e) => setActiveDomain(e.target.value as ConfigDomain)}
                  className="w-full px-3 py-2 border border-edge rounded-lg text-sm bg-card"
                >
                  {DOMAINS.map(d => <option key={d.key} value={d.key}>{d.key}</option>)}
                </select>
              </div>
              <div className="bg-warning/15 p-3 rounded-lg">
                <div className="flex items-start gap-2 text-xs text-warning">
                  <AlertTriangle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                  <span>{t('advancedConfig.export.sensitiveWarning', 'Sensitive values will be masked in the export for security.')}</span>
                </div>
              </div>
              <button
                onClick={onExport}
                className="w-full flex items-center justify-center gap-2 px-4 py-2 bg-brand-600 text-white rounded-lg text-sm font-medium hover:bg-brand-700"
              >
                <Download className="w-4 h-4" />
                {t('advancedConfig.export.button', 'Export JSON')}
              </button>
            </div>
          </CardBody>
        </Card>

        {/* Import */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Upload className="w-5 h-5 text-brand-600" />
              <h3 className="font-semibold">{t('advancedConfig.import.title', 'Import Configuration')}</h3>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-faded mb-4">
              {t('advancedConfig.import.description', 'Import a previously exported configuration file. All values will be validated against current definitions before applying.')}
            </p>
            <div className="space-y-3">
              <div className="border-2 border-dashed border-edge rounded-lg p-8 text-center">
                <Upload className="w-8 h-8 mx-auto mb-2 text-muted" />
                <p className="text-sm text-faded">{t('advancedConfig.import.dropzone', 'Drop JSON file here or click to select')}</p>
                <p className="text-xs text-muted mt-1">{t('advancedConfig.import.format', 'Accepts NexTraceOne configuration export format')}</p>
              </div>
              <div className="bg-info/15 p-3 rounded-lg">
                <div className="flex items-start gap-2 text-xs text-info">
                  <Info className="w-4 h-4 mt-0.5 flex-shrink-0" />
                  <span>{t('advancedConfig.import.previewNote', 'Import will show a preview and validation report before applying any changes.')}</span>
                </div>
              </div>
            </div>
          </CardBody>
        </Card>
      </div>
    </div>
  );
}
