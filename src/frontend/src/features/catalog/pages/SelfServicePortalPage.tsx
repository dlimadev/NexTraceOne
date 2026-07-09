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
  ArrowRight,
} from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { ServicesNeedingSetupSection } from '../components/ServicesNeedingSetupSection';

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
        href: '/services/onboard',
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
        href: '/contracts/new?type=RestApi',
      },
      {
        icon: <Zap size={20} />,
        titleKey: 'selfServicePortal.actions.publishEventContract',
        descKey: 'selfServicePortal.actions.publishEventContract_desc',
        href: '/contracts/new?type=Event',
      },
      {
        icon: <Activity size={20} />,
        titleKey: 'selfServicePortal.actions.viewContractHealth',
        descKey: 'selfServicePortal.actions.viewContractHealth_desc',
        href: '/contracts/health',
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
        href: '/catalog/templates',
      },
      {
        icon: <FileQuestion size={20} />,
        titleKey: 'selfServicePortal.actions.generateAdr',
        descKey: 'selfServicePortal.actions.generateAdr_desc',
        href: '/catalog/templates',
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

      {/* ── Golden paths: arranque da jornada do produtor ── */}
      <section className="mb-6">
        <h2 className="text-sm font-semibold text-heading mb-3">{t('selfServicePortal.goldenPaths.title')}</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Link
            to="/services/onboard"
            className="group flex items-start gap-3 rounded-lg border border-accent/30 bg-accent/5 p-4 shadow-sm transition-all hover:border-accent/60"
          >
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/15 text-accent">
              <Plus size={20} />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-heading group-hover:text-accent">{t('selfServicePortal.goldenPaths.onboard')}</p>
              <p className="mt-0.5 text-xs text-muted leading-snug">{t('selfServicePortal.goldenPaths.onboard_desc')}</p>
            </div>
            <ArrowRight size={16} className="ml-auto shrink-0 text-accent" />
          </Link>
          <Link
            to="/catalog/templates"
            className="group flex items-start gap-3 rounded-lg border border-edge bg-card p-4 shadow-sm transition-all hover:border-accent/40"
          >
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent">
              <Wand2 size={20} />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-heading group-hover:text-accent">{t('selfServicePortal.goldenPaths.template')}</p>
              <p className="mt-0.5 text-xs text-muted leading-snug">{t('selfServicePortal.goldenPaths.template_desc')}</p>
            </div>
          </Link>
        </div>
      </section>

      <ServicesNeedingSetupSection />

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
                className="group flex items-start gap-3 rounded-lg border border-edge bg-card p-4 shadow-sm transition-all hover:border-accent/40 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-accent"
              >
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent group-hover:bg-accent/20 transition-colors">
                  {action.icon}
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-semibold text-heading group-hover:text-accent transition-colors">
                    {t(action.titleKey)}
                  </p>
                  <p className="mt-0.5 text-xs text-muted leading-snug">
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
