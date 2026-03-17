import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Monitor, Code, Search, Shield, ShieldCheck, Users, Plug, Terminal,
  CheckCircle, XCircle, Clock, FileText, AlertTriangle, Zap, Eye,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';

interface IdeClient {
  id: string;
  userId: string;
  userDisplayName: string;
  clientType: 'VsCode' | 'VisualStudio';
  clientVersion: string;
  deviceIdentifier: string;
  lastAccessAt: string;
  isActive: boolean;
}

interface IdeCapabilityPolicy {
  id: string;
  clientType: 'VsCode' | 'VisualStudio';
  persona: string | null;
  allowedCommands: string;
  allowedContextScopes: string;
  allowContractGeneration: boolean;
  allowIncidentTroubleshooting: boolean;
  allowExternalAI: boolean;
  maxTokensPerRequest: number;
  isActive: boolean;
}

const mockClients: IdeClient[] = [
  { id: '1', userId: 'john.doe', userDisplayName: 'John Doe', clientType: 'VsCode', clientVersion: '1.2.0', deviceIdentifier: 'vsc-dev-01', lastAccessAt: '2026-03-15T14:30:00Z', isActive: true },
  { id: '2', userId: 'jane.smith', userDisplayName: 'Jane Smith', clientType: 'VsCode', clientVersion: '1.2.0', deviceIdentifier: 'vsc-dev-02', lastAccessAt: '2026-03-15T11:15:00Z', isActive: true },
  { id: '3', userId: 'bob.wilson', userDisplayName: 'Bob Wilson', clientType: 'VisualStudio', clientVersion: '1.0.0', deviceIdentifier: 'vs-dev-01', lastAccessAt: '2026-03-14T16:45:00Z', isActive: true },
  { id: '4', userId: 'alice.chen', userDisplayName: 'Alice Chen', clientType: 'VsCode', clientVersion: '1.1.0', deviceIdentifier: 'vsc-dev-03', lastAccessAt: '2026-03-13T09:00:00Z', isActive: false },
];

const mockPolicies: IdeCapabilityPolicy[] = [
  { id: '1', clientType: 'VsCode', persona: null, allowedCommands: 'Chat,ServiceLookup,ContractLookup,ContractGenerate,ContractValidate,IncidentLookup,ChangeLookup,RunbookLookup,ServiceSummary,SourceOfTruthQuery', allowedContextScopes: 'services,contracts,incidents,changes,runbooks', allowContractGeneration: true, allowIncidentTroubleshooting: true, allowExternalAI: false, maxTokensPerRequest: 4096, isActive: true },
  { id: '2', clientType: 'VisualStudio', persona: null, allowedCommands: 'Chat,ServiceLookup,ContractLookup,ContractGenerate,ContractValidate,IncidentLookup,ChangeLookup,RunbookLookup,ServiceSummary,SourceOfTruthQuery', allowedContextScopes: 'services,contracts,incidents,changes,runbooks', allowContractGeneration: true, allowIncidentTroubleshooting: true, allowExternalAI: false, maxTokensPerRequest: 4096, isActive: true },
  { id: '3', clientType: 'VsCode', persona: 'Engineer', allowedCommands: 'Chat,ServiceLookup,ContractLookup,ContractGenerate,IncidentLookup,RunbookLookup', allowedContextScopes: 'services,contracts,incidents,runbooks', allowContractGeneration: true, allowIncidentTroubleshooting: true, allowExternalAI: false, maxTokensPerRequest: 4096, isActive: true },
];

type ClientFilter = 'all' | 'VsCode' | 'VisualStudio';

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}m`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h`;
  return `${Math.floor(hrs / 24)}d`;
}

/**
 * Página de IDE Integrations — gestão administrativa de integrações com IDE.
 * Mostra clientes registados, políticas de capacidade e estado geral.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function IdeIntegrationsPage() {
  const { t } = useTranslation();
  const [clientFilter, setClientFilter] = useState<ClientFilter>('all');
  const [search, setSearch] = useState('');

  const filteredClients = mockClients.filter((c) => {
    if (clientFilter !== 'all' && c.clientType !== clientFilter) return false;
    if (search && !c.userDisplayName.toLowerCase().includes(search.toLowerCase()) && !c.userId.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const totalActive = mockClients.filter((c) => c.isActive).length;
  const vsCodeCount = mockClients.filter((c) => c.clientType === 'VsCode' && c.isActive).length;
  const vsCount = mockClients.filter((c) => c.clientType === 'VisualStudio' && c.isActive).length;
  const activePolicies = mockPolicies.filter((p) => p.isActive).length;

  const filters: { key: ClientFilter; label: string }[] = [
    { key: 'all', label: t('aiHub.ideFilterAll') },
    { key: 'VsCode', label: t('aiHub.ideFilterVsCode') },
    { key: 'VisualStudio', label: t('aiHub.ideFilterVisualStudio') },
  ];

  const clientIcon = (type: string) =>
    type === 'VsCode' ? <Code size={14} /> : <Monitor size={14} />;

  return (
    <PageContainer>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('aiHub.ideTitle')}</h1>
        <p className="text-muted mt-1">{t('aiHub.ideSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('aiHub.ideTotalClients')} value={totalActive} icon={<Plug size={20} />} color="text-accent" />
        <StatCard title={t('aiHub.ideVsCodeClients')} value={vsCodeCount} icon={<Code size={20} />} color="text-info" />
        <StatCard title={t('aiHub.ideVisualStudioClients')} value={vsCount} icon={<Monitor size={20} />} color="text-info" />
        <StatCard title={t('aiHub.ideActivePolicies')} value={activePolicies} icon={<ShieldCheck size={20} />} color="text-success" />
      </div>

      {/* Client Types Overview */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-heading mb-3">{t('aiHub.ideSupportedClients')}</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* VS Code */}
          <Card>
            <CardBody>
              <div className="flex items-center gap-3 mb-3">
                <div className="w-10 h-10 rounded-lg bg-accent/10 flex items-center justify-center">
                  <Code size={20} className="text-accent" />
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-heading">{t('aiHub.ideVsCodeTitle')}</h3>
                  <p className="text-xs text-muted">{t('aiHub.ideVsCodeDescription')}</p>
                </div>
                <Badge variant="success" className="ml-auto">{t('aiHub.ideStatusReady')}</Badge>
              </div>
              <div className="flex flex-wrap gap-2 mb-2">
                <Badge variant="info">{t('aiHub.ideActiveClientsLabel')}: {vsCodeCount}</Badge>
                <Badge variant={mockPolicies.some((p) => p.clientType === 'VsCode' && p.isActive) ? 'success' : 'default'}>
                  <Shield size={10} className="mr-1 inline" />{t('aiHub.idePolicyConfigured')}
                </Badge>
              </div>
              <div className="mt-2 flex flex-wrap gap-1">
                {['Chat', 'ServiceLookup', 'ContractLookup', 'ContractGenerate', 'IncidentLookup'].map((cmd) => (
                  <span key={cmd} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{cmd}</span>
                ))}
                <span className="text-[10px] text-faded">+5</span>
              </div>
            </CardBody>
          </Card>

          {/* Visual Studio */}
          <Card>
            <CardBody>
              <div className="flex items-center gap-3 mb-3">
                <div className="w-10 h-10 rounded-lg bg-accent/10 flex items-center justify-center">
                  <Monitor size={20} className="text-accent" />
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-heading">{t('aiHub.ideVisualStudioTitle')}</h3>
                  <p className="text-xs text-muted">{t('aiHub.ideVisualStudioDescription')}</p>
                </div>
                <Badge variant="success" className="ml-auto">{t('aiHub.ideStatusReady')}</Badge>
              </div>
              <div className="flex flex-wrap gap-2 mb-2">
                <Badge variant="info">{t('aiHub.ideActiveClientsLabel')}: {vsCount}</Badge>
                <Badge variant={mockPolicies.some((p) => p.clientType === 'VisualStudio' && p.isActive) ? 'success' : 'default'}>
                  <Shield size={10} className="mr-1 inline" />{t('aiHub.idePolicyConfigured')}
                </Badge>
              </div>
              <div className="mt-2 flex flex-wrap gap-1">
                {['Chat', 'ServiceLookup', 'ContractLookup', 'ContractGenerate', 'IncidentLookup'].map((cmd) => (
                  <span key={cmd} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{cmd}</span>
                ))}
                <span className="text-[10px] text-faded">+5</span>
              </div>
            </CardBody>
          </Card>
        </div>
      </div>

      {/* Registered Clients */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-heading mb-3">{t('aiHub.ideRegisteredClients')}</h2>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 min-w-[200px] max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              placeholder={t('aiHub.ideSearchClients')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-3 py-2 rounded-md bg-surface border border-edge text-body text-sm placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
            />
          </div>
          <div className="flex gap-1.5">
            {filters.map((f) => (
              <button
                key={f.key}
                onClick={() => setClientFilter(f.key)}
                className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${clientFilter === f.key ? 'bg-accent text-heading' : 'bg-elevated text-muted hover:text-body'}`}
              >
                {f.label}
              </button>
            ))}
          </div>
        </div>

        <div className="space-y-2">
          {filteredClients.map((c) => (
            <Card key={c.id}>
              <CardBody>
                <div className="flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-8 h-8 rounded-full bg-accent/10 flex items-center justify-center shrink-0">
                      {clientIcon(c.clientType)}
                    </div>
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-heading truncate">{c.userDisplayName}</span>
                        <Badge variant={c.isActive ? 'success' : 'default'}>{c.isActive ? t('aiHub.statusActive') : t('aiHub.ideStatusRevoked')}</Badge>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-muted">
                        <span>{c.clientType}</span>
                        <span>·</span>
                        <span>v{c.clientVersion}</span>
                        <span>·</span>
                        <Clock size={10} className="inline" />
                        <span>{timeAgo(c.lastAccessAt)}</span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-muted">{c.deviceIdentifier}</span>
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}
          {filteredClients.length === 0 && (
            <Card>
              <CardBody>
                <p className="text-center text-muted py-8">{t('aiHub.ideNoClientsFound')}</p>
              </CardBody>
            </Card>
          )}
        </div>
      </div>

      {/* Capability Policies */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-heading mb-3">{t('aiHub.ideCapabilityPolicies')}</h2>
        <div className="space-y-3">
          {mockPolicies.map((p) => (
            <Card key={p.id}>
              <CardBody>
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-sm font-semibold text-heading">{p.clientType}</span>
                      {p.persona && <Badge variant="info">{p.persona}</Badge>}
                      {!p.persona && <Badge variant="default">{t('aiHub.ideAllPersonas')}</Badge>}
                      <Badge variant={p.isActive ? 'success' : 'default'}>{p.isActive ? t('aiHub.statusActive') : t('aiHub.ideStatusInactive')}</Badge>
                    </div>
                    <div className="flex flex-wrap gap-2 mb-2">
                      {p.allowContractGeneration && (
                        <Badge variant="info"><FileText size={10} className="mr-1 inline" />{t('aiHub.ideContractGen')}</Badge>
                      )}
                      {p.allowIncidentTroubleshooting && (
                        <Badge variant="info"><AlertTriangle size={10} className="mr-1 inline" />{t('aiHub.ideTroubleshooting')}</Badge>
                      )}
                      {p.allowExternalAI ? (
                        <Badge variant="warning"><Zap size={10} className="mr-1 inline" />{t('aiHub.ideExternalAiAllowed')}</Badge>
                      ) : (
                        <Badge variant="default"><Shield size={10} className="mr-1 inline" />{t('aiHub.ideInternalOnly')}</Badge>
                      )}
                      <span className="text-xs text-muted">{t('aiHub.ideMaxTokens')}: {p.maxTokensPerRequest.toLocaleString()}</span>
                    </div>
                    <div className="flex flex-wrap gap-1">
                      {p.allowedCommands.split(',').slice(0, 5).map((cmd) => (
                        <span key={cmd} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{cmd}</span>
                      ))}
                      {p.allowedCommands.split(',').length > 5 && (
                        <span className="text-[10px] text-faded">+{p.allowedCommands.split(',').length - 5}</span>
                      )}
                    </div>
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      </div>

      {/* Governance Notice */}
      <div className="p-4 rounded-lg bg-elevated border border-edge">
        <div className="flex items-start gap-3">
          <ShieldCheck size={18} className="text-accent mt-0.5 shrink-0" />
          <p className="text-xs text-muted">{t('aiHub.ideGovernanceNotice')}</p>
        </div>
      </div>
    </PageContainer>
  );
}
