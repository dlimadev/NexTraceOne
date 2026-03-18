import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';
import {
  Play, ArrowLeft, CheckCircle, XCircle, AlertTriangle,
  Users, Globe, Shield,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';

/**
 * Tipos locais para simulação de Governance Pack — alinhados com o backend SimulateGovernancePack.
 */
interface ImpactedEntity {
  entityId: string;
  entityName: string;
  entityType: string;
  currentStatus: string;
  projectedStatus: string;
  isBlocking: boolean;
}

interface TeamImpact {
  teamName: string;
  affectedCount: number;
  blockingCount: number;
}

interface DomainImpact {
  domainName: string;
  affectedCount: number;
  blockingCount: number;
}

/**
 * Dados simulados de simulação — alinhados com o backend SimulateGovernancePack.
 */
const mockSimulation = {
  packId: 'contracts-baseline',
  packName: 'Contracts Baseline',
  totalAffected: 42,
  compliantCount: 35,
  nonCompliantCount: 7,
  blockingImpacts: 3,
  teamImpacts: [
    { teamName: 'Platform Team', affectedCount: 12, blockingCount: 1 },
    { teamName: 'Payments Team', affectedCount: 8, blockingCount: 1 },
    { teamName: 'Identity Team', affectedCount: 6, blockingCount: 0 },
    { teamName: 'Notifications Team', affectedCount: 9, blockingCount: 1 },
    { teamName: 'Analytics Team', affectedCount: 7, blockingCount: 0 },
  ] as TeamImpact[],
  domainImpacts: [
    { domainName: 'Core Platform', affectedCount: 18, blockingCount: 1 },
    { domainName: 'Payments', affectedCount: 10, blockingCount: 1 },
    { domainName: 'Customer Experience', affectedCount: 14, blockingCount: 1 },
  ] as DomainImpact[],
  impactedEntities: [
    { entityId: 'e-001', entityName: 'Payment Gateway API', entityType: 'Service', currentStatus: 'Non-Compliant', projectedStatus: 'Compliant', isBlocking: false },
    { entityId: 'e-002', entityName: 'Order Events Schema', entityType: 'Contract', currentStatus: 'Non-Compliant', projectedStatus: 'Non-Compliant', isBlocking: true },
    { entityId: 'e-003', entityName: 'User Auth Service', entityType: 'Service', currentStatus: 'Compliant', projectedStatus: 'Compliant', isBlocking: false },
    { entityId: 'e-004', entityName: 'Notification Webhook', entityType: 'Contract', currentStatus: 'Non-Compliant', projectedStatus: 'Non-Compliant', isBlocking: true },
    { entityId: 'e-005', entityName: 'Billing Service', entityType: 'Service', currentStatus: 'Non-Compliant', projectedStatus: 'Compliant', isBlocking: false },
    { entityId: 'e-006', entityName: 'Inventory SOAP Service', entityType: 'Service', currentStatus: 'Non-Compliant', projectedStatus: 'Non-Compliant', isBlocking: true },
    { entityId: 'e-007', entityName: 'Analytics Event Stream', entityType: 'Contract', currentStatus: 'Compliant', projectedStatus: 'Compliant', isBlocking: false },
  ] as ImpactedEntity[],
};

const projectedBadge = (status: string): 'success' | 'danger' => {
  return status === 'Compliant' ? 'success' : 'danger';
};

/**
 * Página de simulação de Governance Pack — mostra impacto projetado antes de aplicar.
 */
export function PackSimulationPage() {
  const { t } = useTranslation();
  const { packId } = useParams<{ packId: string }>();
  const sim = mockSimulation;

  return (
    <PageContainer>
      {/* Back navigation */}
      <Link
        to={`/governance/packs/${packId ?? sim.packId}`}
        className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4"
      >
        <ArrowLeft size={14} />
        {t('governancePacks.simulation.backToPack')}
      </Link>

      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center gap-3 mb-2">
          <Play size={24} className="text-accent" />
          <h1 className="text-2xl font-bold text-heading">{t('governancePacks.simulation.title')}</h1>
        </div>
        <p className="text-muted mt-1">
          {t('governancePacks.simulation.subtitle', { packName: sim.packName })}
        </p>
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">{t('governance.preview.simulationReason')}</span>
        </div>
      </div>

      {/* Summary stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governancePacks.simulation.totalAffected')} value={sim.totalAffected} icon={<Shield size={20} />} color="text-accent" />
        <StatCard title={t('governancePacks.simulation.compliant')} value={sim.compliantCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('governancePacks.simulation.nonCompliant')} value={sim.nonCompliantCount} icon={<XCircle size={20} />} color="text-critical" />
        <StatCard title={t('governancePacks.simulation.blockingImpacts')} value={sim.blockingImpacts} icon={<AlertTriangle size={20} />} color="text-warning" />
      </div>

      {/* Impact breakdowns */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        {/* By team */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Users size={16} className="text-accent" />
              {t('governancePacks.simulation.impactByTeam')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {sim.teamImpacts.map(team => (
                <div key={team.teamName} className="flex items-center justify-between px-4 py-3 hover:bg-hover transition-colors">
                  <span className="text-sm text-heading">{team.teamName}</span>
                  <div className="flex items-center gap-3 text-xs">
                    <span className="text-muted">{t('governancePacks.simulation.affected')}: {team.affectedCount}</span>
                    {team.blockingCount > 0 && (
                      <Badge variant="danger">{team.blockingCount} {t('governancePacks.simulation.blocking')}</Badge>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>

        {/* By domain */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Globe size={16} className="text-accent" />
              {t('governancePacks.simulation.impactByDomain')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {sim.domainImpacts.map(domain => (
                <div key={domain.domainName} className="flex items-center justify-between px-4 py-3 hover:bg-hover transition-colors">
                  <span className="text-sm text-heading">{domain.domainName}</span>
                  <div className="flex items-center gap-3 text-xs">
                    <span className="text-muted">{t('governancePacks.simulation.affected')}: {domain.affectedCount}</span>
                    {domain.blockingCount > 0 && (
                      <Badge variant="danger">{domain.blockingCount} {t('governancePacks.simulation.blocking')}</Badge>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Impacted entities table */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Shield size={16} className="text-accent" />
            {t('governancePacks.simulation.impactedEntities')}
          </h2>
          <p className="text-xs text-muted mt-1">{t('governancePacks.simulation.impactedEntitiesDescription')}</p>
        </CardHeader>
        <CardBody className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-xs text-faded">
                  <th className="px-4 py-3 text-left font-medium">{t('governancePacks.simulation.entityName')}</th>
                  <th className="px-4 py-3 text-left font-medium">{t('governancePacks.simulation.entityType')}</th>
                  <th className="px-4 py-3 text-left font-medium">{t('governancePacks.simulation.currentStatus')}</th>
                  <th className="px-4 py-3 text-left font-medium">{t('governancePacks.simulation.projectedStatus')}</th>
                  <th className="px-4 py-3 text-left font-medium">{t('governancePacks.simulation.blockingLabel')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {sim.impactedEntities.map(entity => (
                  <tr key={entity.entityId} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-3 text-heading font-medium">{entity.entityName}</td>
                    <td className="px-4 py-3">
                      <Badge variant="default">{entity.entityType}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={projectedBadge(entity.currentStatus)}>{entity.currentStatus}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={projectedBadge(entity.projectedStatus)}>{entity.projectedStatus}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      {entity.isBlocking ? (
                        <Badge variant="danger">{t('governancePacks.simulation.yes')}</Badge>
                      ) : (
                        <span className="text-xs text-faded">{t('governancePacks.simulation.no')}</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
