import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Server,
  FileText,
  GitBranch,
  Shield,
  Activity,
  Bot,
  Plus,
  Plug,
  List,
  FileCode,
  Zap,
  ArrowUpCircle,
  Crosshair,
  Calendar,
  Key,
  Eye,
  ShieldAlert,
  Play,
  Phone,
  AlertTriangle,
  Wand2,
  FileQuestion,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

interface ActionItem {
  icon: React.ReactNode;
  titleKey: string;
  descKey: string;
  href: string;
}

interface ActionGroup {
  groupIcon: React.ReactNode;
  groupKey: string;
  actions: ActionItem[];
}

const ACTION_GROUPS: ActionGroup[] = [
  {
    groupIcon: <Server size={18} />,
    groupKey: 'services',
    actions: [
      {
        icon: <Plus size={20} />,
        titleKey: 'selfServicePortal.actions.createService',
        descKey: 'selfServicePortal.actions.createService_desc',
        href: '/catalog/services/create',
      },
      {
        icon: <Plug size={20} />,
        titleKey: 'selfServicePortal.actions.registerIntegration',
        descKey: 'selfServicePortal.actions.registerIntegration_desc',
        href: '/integrations',
      },
      {
        icon: <List size={20} />,
        titleKey: 'selfServicePortal.actions.viewServiceCatalog',
        descKey: 'selfServicePortal.actions.viewServiceCatalog_desc',
        href: '/catalog/services',
      },
    ],
  },
  {
    groupIcon: <FileText size={18} />,
    groupKey: 'contracts',
    actions: [
      {
        icon: <FileCode size={20} />,
        titleKey: 'selfServicePortal.actions.publishRestContract',
        descKey: 'selfServicePortal.actions.publishRestContract_desc',
        href: '/contracts/new?type=rest',
      },
      {
        icon: <Zap size={20} />,
        titleKey: 'selfServicePortal.actions.publishEventContract',
        descKey: 'selfServicePortal.actions.publishEventContract_desc',
        href: '/contracts/new?type=event',
      },
      {
        icon: <Activity size={20} />,
        titleKey: 'selfServicePortal.actions.viewContractHealth',
        descKey: 'selfServicePortal.actions.viewContractHealth_desc',
        href: '/contracts/governance/health',
      },
    ],
  },
  {
    groupIcon: <GitBranch size={18} />,
    groupKey: 'changes',
    actions: [
      {
        icon: <ArrowUpCircle size={20} />,
        titleKey: 'selfServicePortal.actions.promoteRelease',
        descKey: 'selfServicePortal.actions.promoteRelease_desc',
        href: '/changes/promotion',
      },
      {
        icon: <Crosshair size={20} />,
        titleKey: 'selfServicePortal.actions.requestBlastRadius',
        descKey: 'selfServicePortal.actions.requestBlastRadius_desc',
        href: '/changes',
      },
      {
        icon: <Calendar size={20} />,
        titleKey: 'selfServicePortal.actions.viewReleaseCalendar',
        descKey: 'selfServicePortal.actions.viewReleaseCalendar_desc',
        href: '/changes/calendar',
      },
    ],
  },
  {
    groupIcon: <Shield size={18} />,
    groupKey: 'access',
    actions: [
      {
        icon: <Key size={20} />,
        titleKey: 'selfServicePortal.actions.requestJitAccess',
        descKey: 'selfServicePortal.actions.requestJitAccess_desc',
        href: '/identity/jit-access',
      },
      {
        icon: <Eye size={20} />,
        titleKey: 'selfServicePortal.actions.viewMyPermissions',
        descKey: 'selfServicePortal.actions.viewMyPermissions_desc',
        href: '/identity/access-reviews',
      },
      {
        icon: <ShieldAlert size={20} />,
        titleKey: 'selfServicePortal.actions.breakGlassAccess',
        descKey: 'selfServicePortal.actions.breakGlassAccess_desc',
        href: '/identity/break-glass',
      },
    ],
  },
  {
    groupIcon: <Activity size={18} />,
    groupKey: 'operations',
    actions: [
      {
        icon: <Play size={20} />,
        titleKey: 'selfServicePortal.actions.runRunbook',
        descKey: 'selfServicePortal.actions.runRunbook_desc',
        href: '/operations/runbooks',
      },
      {
        icon: <Phone size={20} />,
        titleKey: 'selfServicePortal.actions.viewOnCallStatus',
        descKey: 'selfServicePortal.actions.viewOnCallStatus_desc',
        href: '/operations/on-call-intelligence',
      },
      {
        icon: <AlertTriangle size={20} />,
        titleKey: 'selfServicePortal.actions.reportIncident',
        descKey: 'selfServicePortal.actions.reportIncident_desc',
        href: '/operations/incidents/new',
      },
    ],
  },
  {
    groupIcon: <Bot size={18} />,
    groupKey: 'ai',
    actions: [
      {
        icon: <Bot size={20} />,
        titleKey: 'selfServicePortal.actions.aiAgentMarketplace',
        descKey: 'selfServicePortal.actions.aiAgentMarketplace_desc',
        href: '/ai/marketplace',
      },
      {
        icon: <Wand2 size={20} />,
        titleKey: 'selfServicePortal.actions.createAiScaffold',
        descKey: 'selfServicePortal.actions.createAiScaffold_desc',
        href: '/catalog/scaffold',
      },
      {
        icon: <FileQuestion size={20} />,
        titleKey: 'selfServicePortal.actions.generateAdr',
        descKey: 'selfServicePortal.actions.generateAdr_desc',
        href: '/catalog/scaffold',
      },
    ],
  },
];

export function SelfServicePortalPage() {
  const { t } = useTranslation();

  return (
    <PageContainer>
      <PageHeader
        title={t('selfServicePortal.title')}
        subtitle={t('selfServicePortal.subtitle')}
        icon={<Wand2 size={24} />}
      />

      {ACTION_GROUPS.map((group) => (
        <PageSection
          key={group.groupKey}
          title={
            <span className="flex items-center gap-2">
              {group.groupIcon}
              {t(`selfServicePortal.groups.${group.groupKey}`)}
            </span>
          }
        >
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {group.actions.map((action) => (
              <Link
                key={action.titleKey}
                to={action.href}
                className="group flex items-start gap-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 shadow-sm transition-all hover:border-blue-400 dark:hover:border-blue-500 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-blue-400"
              >
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400 group-hover:bg-blue-100 dark:group-hover:bg-blue-900/40 transition-colors">
                  {action.icon}
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-semibold text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                    {t(action.titleKey)}
                  </p>
                  <p className="mt-0.5 text-xs text-gray-500 dark:text-gray-400 leading-snug">
                    {t(action.descKey)}
                  </p>
                </div>
              </Link>
            ))}
          </div>
        </PageSection>
      ))}
    </PageContainer>
  );
}
