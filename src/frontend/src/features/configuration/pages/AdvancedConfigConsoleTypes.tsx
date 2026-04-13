/**
 * Tipos, constantes e helpers partilhados pela AdvancedConfigurationConsolePage
 * e pelos seus sub-componentes de tab.
 */
import {
  Settings,
  Activity,
  ArrowLeftRight,
  Shield,
  FileJson,
  Eye,
  Layers,
  ArrowRight,
  Lock,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import type { TFunction } from 'i18next';

// ── Domain Navigation ──────────────────────────────────────────────────

export type ConfigDomain =
  | 'all'
  | 'instance'
  | 'notifications'
  | 'workflows'
  | 'governance'
  | 'catalog'
  | 'operations'
  | 'ai'
  | 'integrations';

export type AdminTab =
  | 'explorer'
  | 'diff'
  | 'importExport'
  | 'rollback'
  | 'history'
  | 'health';

export interface DomainMeta {
  key: ConfigDomain;
  prefixes: string[];
  icon: React.ReactNode;
}

export const DOMAINS: DomainMeta[] = [
  { key: 'all', prefixes: [], icon: <Layers className="w-4 h-4" /> },
  { key: 'instance', prefixes: ['instance.', 'tenant.', 'environment.', 'branding.', 'featureFlags.'], icon: <Settings className="w-4 h-4" /> },
  { key: 'notifications', prefixes: ['notifications.'], icon: <Activity className="w-4 h-4" /> },
  { key: 'workflows', prefixes: ['workflow.', 'promotion.'], icon: <ArrowRight className="w-4 h-4" /> },
  { key: 'governance', prefixes: ['governance.'], icon: <Shield className="w-4 h-4" /> },
  { key: 'catalog', prefixes: ['catalog.', 'change.'], icon: <FileJson className="w-4 h-4" /> },
  { key: 'operations', prefixes: ['incidents.', 'operations.', 'finops.', 'benchmarking.'], icon: <Activity className="w-4 h-4" /> },
  { key: 'ai', prefixes: ['ai.'], icon: <Eye className="w-4 h-4" /> },
  { key: 'integrations', prefixes: ['integrations.'], icon: <ArrowLeftRight className="w-4 h-4" /> },
];

// ── Helpers ────────────────────────────────────────────────────────────

export function matchDomain(key: string, domain: DomainMeta): boolean {
  if (domain.key === 'all') return true;
  return domain.prefixes.some((p) => key.startsWith(p));
}

export function renderValuePreview(value: string | null, isSensitive: boolean, t: TFunction): React.ReactNode {
  if (isSensitive) return <Badge variant="warning"><Lock className="w-3 h-3 mr-1" />{t('advancedConfig.badges.masked', 'Masked')}</Badge>;
  if (!value) return <span className="text-muted italic text-xs">null</span>;
  if (value === 'true') return <Badge variant="success">{t('advancedConfig.badges.enabled', 'Enabled')}</Badge>;
  if (value === 'false') return <Badge variant="default">{t('advancedConfig.badges.disabled', 'Disabled')}</Badge>;
  try {
    const parsed = JSON.parse(value);
    if (Array.isArray(parsed)) return <Badge variant="info">{parsed.length} items</Badge>;
    if (typeof parsed === 'object') return <Badge variant="info">{Object.keys(parsed).length} fields</Badge>;
  } catch { /* not JSON */ }
  return <span className="text-sm text-faded truncate max-w-[200px] inline-block">{value}</span>;
}
