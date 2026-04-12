/**
 * Tab "Effective Explorer" da AdvancedConfigurationConsolePage.
 *
 * Exibe todas as definições de configuração com o seu valor efectivo,
 * herança, scope resolvido e acesso ao histórico de auditoria.
 */
import { memo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ChevronDown,
  ChevronUp,
  Lock,
  Shield,
  Layers,
  History,
  Filter,
  RefreshCw,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { renderValuePreview } from './AdvancedConfigConsoleTypes';
import type {
  ConfigurationDefinitionDto,
  EffectiveConfigurationDto,
  ConfigurationScope,
} from '../types';

// ── Props ──────────────────────────────────────────────────────────────

export interface AdvancedConfigExplorerTabProps {
  selectedScope: ConfigurationScope;
  filteredDefs: ConfigurationDefinitionDto[];
  effectiveMap: Map<string, EffectiveConfigurationDto>;
  expandedKey: string | null;
  loadingEffective: boolean;
  showSensitive: boolean;
  setSelectedScope: (scope: ConfigurationScope) => void;
  setExpandedKey: (key: string | null) => void;
  setSelectedAuditKey: (key: string | null) => void;
}

// ── Component ──────────────────────────────────────────────────────────

export const AdvancedConfigExplorerTab = memo(function AdvancedConfigExplorerTab({
  selectedScope,
  filteredDefs,
  effectiveMap,
  expandedKey,
  loadingEffective,
  showSensitive,
  setSelectedScope,
  setExpandedKey,
  setSelectedAuditKey,
}: AdvancedConfigExplorerTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-4 mb-4">
        <select
          value={selectedScope}
          onChange={(e) => setSelectedScope(e.target.value as ConfigurationScope)}
          className="px-3 py-2 border border-edge rounded-lg text-sm bg-card"
        >
          <option value="System">System</option>
          <option value="Tenant">Tenant</option>
          <option value="Environment">Environment</option>
        </select>
        <span className="text-sm text-faded">
          {t('advancedConfig.explorer.showing', 'Showing')} {filteredDefs.length} {t('advancedConfig.explorer.definitions', 'definitions')}
          {loadingEffective && <RefreshCw className="w-3 h-3 ml-2 animate-spin inline" />}
        </span>
      </div>

      {filteredDefs.map((def: ConfigurationDefinitionDto) => {
        const eff = effectiveMap.get(def.key);
        const isExpanded = expandedKey === def.key;

        return (
          <Card key={def.key}>
            <CardBody>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => setExpandedKey(isExpanded ? null : def.key)}
                      className="flex items-center gap-1 text-left"
                    >
                      {isExpanded ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
                      <span className="font-medium text-sm">{def.displayName}</span>
                    </button>
                    {eff?.isInherited && <Badge variant="info" className="text-xs">Inherited</Badge>}
                    {eff?.isDefault && <Badge variant="default" className="text-xs">Default</Badge>}
                    {!def.isInheritable && <Badge variant="warning" className="text-xs"><Lock className="w-3 h-3 mr-1" />Mandatory</Badge>}
                    {def.isSensitive && <Badge variant="warning" className="text-xs"><Shield className="w-3 h-3 mr-1" />Sensitive</Badge>}
                  </div>
                  <p className="text-xs text-faded mt-1 ml-5">{def.key}</p>
                </div>
                <div className="flex items-center gap-3">
                  {eff && (
                    <div className="text-right">
                      <div className="text-xs text-muted">{eff.resolvedScope}</div>
                      {renderValuePreview(eff.effectiveValue, def.isSensitive && !showSensitive)}
                    </div>
                  )}
                  {!eff && renderValuePreview(def.defaultValue, def.isSensitive && !showSensitive)}
                </div>
              </div>

              {isExpanded && (
                <div className="mt-4 pt-4 border-t border-edge space-y-3">
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
                    <div>
                      <span className="text-muted">{t('advancedConfig.explorer.type', 'Type')}</span>
                      <p className="font-medium">{def.valueType}</p>
                    </div>
                    <div>
                      <span className="text-muted">{t('advancedConfig.explorer.scopes', 'Scopes')}</span>
                      <p className="font-medium">{def.allowedScopes?.join(', ')}</p>
                    </div>
                    <div>
                      <span className="text-muted">{t('advancedConfig.explorer.editor', 'Editor')}</span>
                      <p className="font-medium">{def.uiEditorType ?? 'text'}</p>
                    </div>
                    <div>
                      <span className="text-muted">{t('advancedConfig.explorer.inheritable', 'Inheritable')}</span>
                      <p className="font-medium">{def.isInheritable ? 'Yes' : 'No'}</p>
                    </div>
                  </div>
                  {def.description && (
                    <p className="text-xs text-faded">{def.description}</p>
                  )}
                  <div>
                    <span className="text-xs text-muted">{t('advancedConfig.explorer.defaultValue', 'Default Value')}</span>
                    <pre className="mt-1 p-2 bg-subtle rounded text-xs overflow-x-auto">
                      {def.defaultValue ?? 'null'}
                    </pre>
                  </div>
                  {eff && (
                    <div className="p-3 bg-brand-50 rounded-lg">
                      <div className="flex items-center gap-2 text-xs text-brand-700 mb-1">
                        <Layers className="w-3 h-3" />
                        {t('advancedConfig.explorer.effectiveValue', 'Effective Value')}
                        <Badge variant="info" className="text-xs">{eff.resolvedScope}</Badge>
                        {eff.isInherited && <Badge variant="default" className="text-xs">Inherited</Badge>}
                      </div>
                      <pre className="text-xs overflow-x-auto">
                        {def.isSensitive && !showSensitive ? '***MASKED***' : (eff.effectiveValue ?? 'null')}
                      </pre>
                    </div>
                  )}
                  <div className="flex gap-2">
                    <button
                      onClick={() => setSelectedAuditKey(def.key)}
                      className="flex items-center gap-1 px-2 py-1 text-xs text-faded hover:text-brand-600 transition-colors"
                    >
                      <History className="w-3 h-3" />
                      {t('advancedConfig.explorer.viewHistory', 'View History')}
                    </button>
                  </div>
                </div>
              )}
            </CardBody>
          </Card>
        );
      })}

      {filteredDefs.length === 0 && (
        <Card>
          <CardBody>
            <div className="text-center py-8 text-faded">
              <Filter className="w-8 h-8 mx-auto mb-2 opacity-50" />
              <p>{t('advancedConfig.explorer.noResults', 'No matching definitions found.')}</p>
            </div>
          </CardBody>
        </Card>
      )}
    </div>
  );
});
