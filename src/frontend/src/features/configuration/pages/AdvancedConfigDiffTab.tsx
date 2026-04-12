/**
 * Tab "Diff & Compare" da AdvancedConfigurationConsolePage.
 *
 * Permite comparar valores efectivos de configuração entre dois scopes,
 * destacando as diferenças encontradas.
 */
import { useTranslation } from 'react-i18next';
import { ArrowLeftRight, CheckCircle2 } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import type {
  ConfigurationDefinitionDto,
  ConfigurationScope,
} from '../types';

// ── Types ──────────────────────────────────────────────────────────────

export interface DiffItem {
  def: ConfigurationDefinitionDto;
  leftVal: string | null;
  rightVal: string | null;
  isDifferent: boolean;
}

// ── Props ──────────────────────────────────────────────────────────────

export interface AdvancedConfigDiffTabProps {
  selectedScope: ConfigurationScope;
  compareScope: ConfigurationScope;
  diffItems: DiffItem[];
  setSelectedScope: (scope: ConfigurationScope) => void;
  setCompareScope: (scope: ConfigurationScope) => void;
}

// ── Component ──────────────────────────────────────────────────────────

export function AdvancedConfigDiffTab({
  selectedScope,
  compareScope,
  diffItems,
  setSelectedScope,
  setCompareScope,
}: AdvancedConfigDiffTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <Card>
        <CardBody>
          <div className="flex items-center gap-4">
            <div className="flex-1">
              <label className="text-xs text-faded mb-1 block">{t('advancedConfig.diff.leftScope', 'Left Scope')}</label>
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
            <ArrowLeftRight className="w-5 h-5 text-muted mt-5" />
            <div className="flex-1">
              <label className="text-xs text-faded mb-1 block">{t('advancedConfig.diff.rightScope', 'Right Scope')}</label>
              <select
                value={compareScope}
                onChange={(e) => setCompareScope(e.target.value as ConfigurationScope)}
                className="w-full px-3 py-2 border border-edge rounded-lg text-sm bg-card"
              >
                <option value="System">System</option>
                <option value="Tenant">Tenant</option>
                <option value="Environment">Environment</option>
              </select>
            </div>
          </div>
        </CardBody>
      </Card>

      <div className="flex items-center gap-2 text-sm text-faded">
        <ArrowLeftRight className="w-4 h-4" />
        {diffItems.length} {t('advancedConfig.diff.differences', 'differences found')}
      </div>

      {diffItems.map(({ def, leftVal, rightVal }) => (
        <Card key={def.key}>
          <CardBody>
            <div className="flex items-center gap-2 mb-3">
              <span className="font-medium text-sm">{def.displayName}</span>
              <span className="text-xs text-muted">{def.key}</span>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="p-3 bg-critical/15 rounded-lg">
                <div className="text-xs text-critical mb-1">{selectedScope}</div>
                <pre className="text-xs overflow-x-auto">{def.isSensitive ? '***' : (leftVal ?? 'null')}</pre>
              </div>
              <div className="p-3 bg-success/15 rounded-lg">
                <div className="text-xs text-success mb-1">{compareScope}</div>
                <pre className="text-xs overflow-x-auto">{def.isSensitive ? '***' : (rightVal ?? 'null')}</pre>
              </div>
            </div>
          </CardBody>
        </Card>
      ))}

      {diffItems.length === 0 && (
        <Card>
          <CardBody>
            <div className="text-center py-8 text-faded">
              <CheckCircle2 className="w-8 h-8 mx-auto mb-2 text-success" />
              <p>{t('advancedConfig.diff.noDifferences', 'No differences between the selected scopes.')}</p>
            </div>
          </CardBody>
        </Card>
      )}
    </div>
  );
}
