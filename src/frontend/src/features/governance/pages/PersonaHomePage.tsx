import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import {
  Server,
  AlertTriangle,
  ClipboardCheck,
  Activity,
  Rocket,
  PhoneCall,
  HeartPulse,
  Zap,
  ShieldAlert,
  TrendingUp,
  GitPullRequest,
  UserX,
  ShieldCheck,
  DollarSign,
  BarChart3,
  AlertCircle,
  RefreshCw,
  FlaskConical,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { PageHeader } from '../../../components/PageHeader';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { usePersona } from '../../../contexts/PersonaContext';
import type { Persona } from '../../../auth/persona';

// ── Types ─────────────────────────────────────────────────────────────────────

interface StatDef {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color: string;
  trend?: { direction: 'up' | 'down'; label: string };
  context?: string;
}

// ── Simulated stat definitions per persona ────────────────────────────────────

const ENGINEER_STATS: StatDef[] = [
  {
    title: 'My Services',
    value: '7',
    icon: <Server size={18} />,
    color: 'text-accent',
    context: '2 degraded',
  },
  {
    title: 'Open Incidents',
    value: '3',
    icon: <AlertTriangle size={18} />,
    color: 'text-critical',
    trend: { direction: 'up', label: '+1 today' },
  },
  {
    title: 'Pending Approvals',
    value: '5',
    icon: <ClipboardCheck size={18} />,
    color: 'text-warning',
    context: '2 urgent',
  },
  {
    title: 'SLO Status',
    value: '94.2%',
    icon: <Activity size={18} />,
    color: 'text-success',
    trend: { direction: 'up', label: '+0.4%' },
  },
  {
    title: 'Last Deploy',
    value: '2h ago',
    icon: <Rocket size={18} />,
    color: 'text-info',
    context: 'payment-svc v2.4.1',
  },
  {
    title: 'On-Call Status',
    value: 'Active',
    icon: <PhoneCall size={18} />,
    color: 'text-warning',
    context: 'Until 09:00 tomorrow',
  },
];

const TECHLEAD_STATS: StatDef[] = [
  {
    title: 'Team Health',
    value: '82 / 100',
    icon: <HeartPulse size={18} />,
    color: 'text-success',
    trend: { direction: 'up', label: '+4 pts' },
  },
  {
    title: 'Velocity',
    value: '41 pts',
    icon: <Zap size={18} />,
    color: 'text-accent',
    trend: { direction: 'down', label: '-3 pts' },
  },
  {
    title: 'Blockers',
    value: '2',
    icon: <ShieldAlert size={18} />,
    color: 'text-critical',
    context: '1 external dependency',
  },
  {
    title: 'SLO Compliance',
    value: '96.8%',
    icon: <Activity size={18} />,
    color: 'text-success',
    trend: { direction: 'up', label: '+0.2%' },
  },
  {
    title: 'Change Confidence',
    value: 'High',
    icon: <TrendingUp size={18} />,
    color: 'text-success',
    context: '3 pending merges',
  },
  {
    title: 'Ownership Gaps',
    value: '4',
    icon: <UserX size={18} />,
    color: 'text-warning',
    context: 'Services without owner',
  },
];

const EXECUTIVE_STATS: StatDef[] = [
  {
    title: 'Compliance Score',
    value: '87%',
    icon: <ShieldCheck size={18} />,
    color: 'text-success',
    trend: { direction: 'up', label: '+2% MoM' },
  },
  {
    title: 'Risk Level',
    value: 'Medium',
    icon: <ShieldAlert size={18} />,
    color: 'text-warning',
    context: '3 unmitigated',
  },
  {
    title: 'FinOps Budget',
    value: '71% used',
    icon: <DollarSign size={18} />,
    color: 'text-warning',
    context: '$42k remaining',
    trend: { direction: 'up', label: '+5% vs last month' },
  },
  {
    title: 'DORA Tier',
    value: 'Elite',
    icon: <BarChart3 size={18} />,
    color: 'text-accent',
    context: 'Top 10% industry',
  },
  {
    title: 'Active Incidents',
    value: '2',
    icon: <AlertCircle size={18} />,
    color: 'text-critical',
    context: '0 P1, 2 P2',
  },
  {
    title: 'Open Changes',
    value: '14',
    icon: <GitPullRequest size={18} />,
    color: 'text-info',
    trend: { direction: 'up', label: '+3 today' },
  },
];

const DEFAULT_STATS: StatDef[] = [
  {
    title: 'Policies Active',
    value: '34',
    icon: <ShieldCheck size={18} />,
    color: 'text-success',
  },
  {
    title: 'Open Violations',
    value: '7',
    icon: <AlertTriangle size={18} />,
    color: 'text-critical',
    trend: { direction: 'down', label: '-2 today' },
  },
  {
    title: 'Pending Reviews',
    value: '12',
    icon: <ClipboardCheck size={18} />,
    color: 'text-warning',
  },
  {
    title: 'Compliance',
    value: '88%',
    icon: <Activity size={18} />,
    color: 'text-success',
  },
  {
    title: 'Risk Score',
    value: 'Low',
    icon: <ShieldAlert size={18} />,
    color: 'text-success',
  },
  {
    title: 'Last Sync',
    value: '5m ago',
    icon: <RefreshCw size={18} />,
    color: 'text-info',
  },
];

// ── Helpers ───────────────────────────────────────────────────────────────────

function getPersonaStats(persona: Persona): StatDef[] {
  switch (persona) {
    case 'Engineer':
      return ENGINEER_STATS;
    case 'TechLead':
      return TECHLEAD_STATS;
    case 'Executive':
      return EXECUTIVE_STATS;
    default:
      return DEFAULT_STATS;
  }
}

function getPersonaDisplayName(persona: Persona): string {
  const names: Partial<Record<Persona, string>> = {
    Engineer: 'Engineer',
    TechLead: 'Tech Lead',
    Architect: 'Architect',
    Product: 'Product Manager',
    Executive: 'Executive',
    PlatformAdmin: 'Platform Admin',
    Auditor: 'Auditor',
    AiUser: 'AI User',
  };
  return names[persona] ?? persona;
}

function getPersonaSubtitle(persona: Persona): string {
  const subtitles: Partial<Record<Persona, string>> = {
    Engineer: "Your services, incidents, and on-call status at a glance.",
    TechLead: "Team health, velocity, and ownership coverage for your squad.",
    Executive: "Portfolio risk, compliance posture, and FinOps budget at executive level.",
    Auditor: "Compliance coverage, policy status, and governance review queue.",
    Architect: "System health, dependency graph, and architectural risk signals.",
    PlatformAdmin: "Platform health, configuration drift, and active incidents.",
  };
  return subtitles[persona] ?? 'Your personalised governance overview.';
}

// ── Page ──────────────────────────────────────────────────────────────────────

/**
 * PersonaHomePage — V3.10 persona-first home page.
 *
 * Renders 6 KPI stat cards tailored to the active persona (Engineer, TechLead,
 * Executive, or a sensible default). Persona is derived from PersonaContext;
 * an override can be passed via URL param `?persona=Engineer`.
 */
export function PersonaHomePage() {
  const { t } = useTranslation();
  const { persona: ctxPersona } = usePersona();
  const { persona: urlPersona } = useParams<{ persona?: string }>();

  // Allow URL param override for demo / testing purposes
  const resolvedPersona: Persona = (urlPersona as Persona) ?? ctxPersona;

  const stats = getPersonaStats(resolvedPersona);
  const displayName = getPersonaDisplayName(resolvedPersona);
  const subtitle = getPersonaSubtitle(resolvedPersona);

  return (
    <PageContainer>
      {/* IsSimulated banner */}
      <div className="mb-4 rounded-lg border border-warning/30 bg-warning/8 px-4 py-2 text-xs text-warning font-medium flex items-center gap-2">
        <FlaskConical size={14} />
        {t(
          'governance.simulated',
          'Simulated data — metrics will connect to live API in production',
        )}
      </div>

      <PageHeader
        title={t('governance.personaHome.title', '{{persona}} Home', {
          persona: displayName,
        })}
        subtitle={subtitle}
        badge={
          <Badge variant="info" size="sm">
            {resolvedPersona}
          </Badge>
        }
      />

      {/* 6 KPI stat cards */}
      <StatsGrid columns={3}>
        {stats.map((stat) => (
          <StatCard
            key={stat.title}
            title={stat.title}
            value={stat.value}
            icon={stat.icon}
            color={stat.color}
            trend={stat.trend}
            context={stat.context}
          />
        ))}
      </StatsGrid>
    </PageContainer>
  );
}
