import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
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
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { usePersona } from '../../../contexts/PersonaContext';
import type { Persona } from '../../../auth/persona';
import { personaHomeApi, type HomeCardDto } from '../api/personaHome';

// ── Icon mapping ──────────────────────────────────────────────────────────────

const ICON_MAP: Record<string, React.ReactNode> = {
  my_services: <Server size={18} />,
  open_incidents: <AlertTriangle size={18} />,
  pending_approvals: <ClipboardCheck size={18} />,
  slo_status: <Activity size={18} />,
  last_deploy: <Rocket size={18} />,
  on_call_status: <PhoneCall size={18} />,
  team_health: <HeartPulse size={18} />,
  velocity: <Zap size={18} />,
  blockers: <ShieldAlert size={18} />,
  slo_compliance: <Activity size={18} />,
  change_confidence: <TrendingUp size={18} />,
  ownership_gaps: <UserX size={18} />,
  compliance_score: <ShieldCheck size={18} />,
  risk_level: <ShieldAlert size={18} />,
  finops_budget: <DollarSign size={18} />,
  dora_tier: <BarChart3 size={18} />,
  active_incidents: <AlertCircle size={18} />,
  open_changes: <GitPullRequest size={18} />,
  services: <Server size={18} />,
  changes: <GitPullRequest size={18} />,
  compliance: <ShieldCheck size={18} />,
  incidents: <AlertTriangle size={18} />,
  risk: <ShieldAlert size={18} />,
  slos: <Activity size={18} />,
};

const SEVERITY_COLOR: Record<string, string> = {
  info: 'text-info',
  success: 'text-success',
  warning: 'text-warning',
  critical: 'text-critical',
};

function cardIcon(key: string): React.ReactNode {
  return ICON_MAP[key] ?? <RefreshCw size={18} />;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

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

export function PersonaHomePage() {
  const { t } = useTranslation();
  const { persona: ctxPersona } = usePersona();
  const { persona: urlPersona } = useParams<{ persona?: string }>();

  const resolvedPersona: Persona = (urlPersona as Persona) ?? ctxPersona;
  const displayName = getPersonaDisplayName(resolvedPersona);
  const subtitle = getPersonaSubtitle(resolvedPersona);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['persona-home', resolvedPersona],
    queryFn: () =>
      personaHomeApi.getPersonaHome('current', resolvedPersona, 'default'),
    staleTime: 60_000,
  });

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError)
    return (
      <PageContainer>
        <PageErrorState onRetry={() => refetch()} />
      </PageContainer>
    );

  const cards: HomeCardDto[] = data?.cards ?? [];

  return (
    <PageContainer>
      {data?.isSimulated && (
        <div className="mb-4 rounded-lg border border-warning/30 bg-warning/8 px-4 py-2 text-xs text-warning font-medium flex items-center gap-2">
          <FlaskConical size={14} />
          {data.simulatedNote ??
            t(
              'governance.simulated',
              'Simulated data — metrics will connect to live API in production',
            )}
        </div>
      )}

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

      <StatsGrid columns={3}>
        {cards.map((card) => (
          <StatCard
            key={card.key}
            title={card.title}
            value={card.value ?? '—'}
            icon={cardIcon(card.key)}
            color={SEVERITY_COLOR[card.severity] ?? 'text-muted'}
            trend={
              card.trend
                ? { direction: 'up', label: card.trend }
                : undefined
            }
          />
        ))}
      </StatsGrid>
    </PageContainer>
  );
}
