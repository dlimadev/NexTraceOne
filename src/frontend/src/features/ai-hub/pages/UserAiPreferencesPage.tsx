import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Brain,
  Power,
  Globe,
  Monitor,
  ChevronRight,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Sparkles,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import {
  useUserAiPreferences,
  useUpsertUserAiPreference,
  useDeleteUserAiPreference,
  useAiExecutionPreview,
  type UserAiPreferenceDto,
  type AiExecutionPlanDto,
} from '../hooks/useAiPreferences';
import { aiGovernanceApi } from '../api';

// ── Known features that users can configure ───────────────────────────

interface KnownFeature {
  key: string;
  label: string;
  description: string;
  icon: React.ReactNode;
  requestType: string;
}

const KNOWN_FEATURES: KnownFeature[] = [
  {
    key: '*',
    label: 'Preferência Global',
    description: 'Configuração padrão aplicada a todas as funcionalidades de IA.',
    icon: <Globe className="w-5 h-5" />,
    requestType: 'chat',
  },
  {
    key: 'aiknowledge.agent.doc-agent',
    label: 'Agente de Documentação',
    description: 'Geração e atualização de documentação técnica.',
    icon: <Monitor className="w-5 h-5" />,
    requestType: 'agent',
  },
  {
    key: 'aiknowledge.agent.security-review',
    label: 'Revisão de Segurança',
    description: 'Análise de vulnerabilidades e recomendações de segurança.',
    icon: <Monitor className="w-5 h-5" />,
    requestType: 'agent',
  },
  {
    key: 'catalog.contract-draft',
    label: 'Geração de Contratos',
    description: 'Geração de drafts de contratos e APIs.',
    icon: <Monitor className="w-5 h-5" />,
    requestType: 'chat',
  },
  {
    key: 'governance.dashboard-composer',
    label: 'Composição de Dashboards',
    description: 'Criação assistida de dashboards e widgets.',
    icon: <Monitor className="w-5 h-5" />,
    requestType: 'chat',
  },
];

const PREFERENCE_TYPE_LABELS: Record<number, string> = {
  0: 'Desabilitado',
  1: 'IA Interna',
  2: 'Produto Externo',
};

const PREFERENCE_TYPE_VARIANTS: Record<number, 'default' | 'success' | 'warning' | 'danger'> = {
  0: 'danger',
  1: 'success',
  2: 'warning',
};

const EXTERNAL_PRODUCT_LABELS: Record<number, string> = {
  0: 'ChatGPT',
  1: 'Claude',
  2: 'Gemini',
  3: 'GitHub Copilot',
};

// ── Components ────────────────────────────────────────────────────────

function PreferencePreview({ featureKey, requestType }: { featureKey: string; requestType: string }) {
  const { data: preview, isLoading } = useAiExecutionPreview(featureKey, requestType, true);

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <Loader2 className="w-4 h-4 animate-spin" />
        Analisando...
      </div>
    );
  }

  if (!preview) {
    return (
      <div className="flex items-center gap-2 text-sm text-slate-400">
        <AlertCircle className="w-4 h-4" />
        Preview indisponível
      </div>
    );
  }

  const plan = preview as unknown as AiExecutionPlanDto;

  if (!plan.isAvailable) {
    return (
      <div className="flex items-center gap-2 text-sm text-amber-600">
        <AlertCircle className="w-4 h-4" />
        {plan.unavailabilityReason ?? 'IA indisponível para esta funcionalidade'}
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2 text-sm text-emerald-700">
      <CheckCircle2 className="w-4 h-4" />
      <span className="font-medium">{plan.modelDisplayName}</span>
      <span className="text-slate-400">via</span>
      <Badge variant="default" size="sm">{plan.providerId}</Badge>
    </div>
  );
}

function FeaturePreferenceCard({
  feature,
  existing,
}: {
  feature: KnownFeature;
  existing: UserAiPreferenceDto | undefined;
}) {
  const { t } = useTranslation();
  const upsert = useUpsertUserAiPreference();
  const remove = useDeleteUserAiPreference();
  const [preferenceType, setPreferenceType] = useState<number>(existing?.preferenceType ?? 1);
  const [externalProduct, setExternalProduct] = useState<number>(existing?.externalProduct ?? 0);
  const [isSaving, setIsSaving] = useState(false);

  const hasChanges = useMemo(() => {
    if (!existing) return true;
    if (existing.preferenceType !== preferenceType) return true;
    if (preferenceType === 2 && existing.externalProduct !== externalProduct) return true;
    return false;
  }, [existing, preferenceType, externalProduct]);

  async function handleSave() {
    setIsSaving(true);
    await upsert.mutateAsync({
      featureKey: feature.key,
      preferenceType,
      externalProduct: preferenceType === 2 ? externalProduct : null,
      externalProductModel: preferenceType === 2 ? null : null,
      preferredModelId: preferenceType === 1 ? null : null,
      preferredProviderId: preferenceType === 1 ? null : null,
    });
    setIsSaving(false);
  }

  async function handleReset() {
    if (existing) {
      await remove.mutateAsync(feature.key);
    }
    setPreferenceType(1);
    setExternalProduct(0);
  }

  return (
    <Card className="mb-4">
      <CardBody>
        <div className="flex items-start justify-between">
          <div className="flex items-start gap-3">
            <div className="mt-0.5 text-slate-500">{feature.icon}</div>
            <div>
              <h3 className="text-base font-semibold text-slate-900">{feature.label}</h3>
              <p className="text-sm text-slate-500 mt-0.5">{feature.description}</p>
              <div className="mt-3">
                <PreferencePreview featureKey={feature.key} requestType={feature.requestType} />
              </div>
            </div>
          </div>
          <Badge
            variant={PREFERENCE_TYPE_VARIANTS[existing?.preferenceType ?? preferenceType] ?? 'default'}
            size="sm"
          >
            {PREFERENCE_TYPE_LABELS[existing?.preferenceType ?? preferenceType] ?? 'IA Interna'}
          </Badge>
        </div>

        <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1.5">
              Modo de IA
            </label>
            <select
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              value={preferenceType}
              onChange={(e) => setPreferenceType(Number(e.target.value))}
            >
              <option value={1}>IA Interna (NexTraceOne)</option>
              <option value={2}>Produto Externo</option>
              <option value={0}>Desabilitado</option>
            </select>
          </div>

          {preferenceType === 2 && (
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">
                Produto Externo
              </label>
              <select
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={externalProduct}
                onChange={(e) => setExternalProduct(Number(e.target.value))}
              >
                <option value={0}>ChatGPT (OpenAI)</option>
                <option value={1}>Claude (Anthropic)</option>
                <option value={2}>Gemini (Google)</option>
                <option value={3}>GitHub Copilot</option>
              </select>
            </div>
          )}
        </div>

        <div className="mt-4 flex items-center gap-3">
          <Button
            size="sm"
            onClick={handleSave}
            disabled={!hasChanges || isSaving || upsert.isPending}
          >
            {isSaving || upsert.isPending ? (
              <Loader2 className="w-4 h-4 animate-spin mr-1.5" />
            ) : (
              <Sparkles className="w-4 h-4 mr-1.5" />
            )}
            Aplicar
          </Button>
          {existing && (
            <Button size="sm" variant="ghost" onClick={handleReset} disabled={remove.isPending}>
              <TrashIcon className="w-4 h-4 mr-1.5" />
              Remover preferência
            </Button>
          )}
        </div>
      </CardBody>
    </Card>
  );
}

function TrashIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
    >
      <path d="M3 6h18" />
      <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" />
      <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
      <line x1="10" x2="10" y1="11" y2="17" />
      <line x1="14" x2="14" y1="11" y2="17" />
    </svg>
  );
}

// ── Page ──────────────────────────────────────────────────────────────

export function UserAiPreferencesPage() {
  const { t } = useTranslation();
  const { data: preferencesData, isLoading, error } = useUserAiPreferences();

  const preferences = useMemo(() => {
    if (!preferencesData) return [] as UserAiPreferenceDto[];
    if (Array.isArray(preferencesData)) return preferencesData;
    return (preferencesData as { items?: UserAiPreferenceDto[] }).items ?? [];
  }, [preferencesData]);

  if (isLoading) {
    return (
      <PageContainer>
        <PageHeader title="Preferências de IA" subtitle="Configurar como você usa a inteligência artificial na plataforma" />
        <div className="flex items-center justify-center h-64">
          <Loader2 className="w-8 h-8 animate-spin text-indigo-600" />
        </div>
      </PageContainer>
    );
  }

  if (error) {
    return (
      <PageContainer>
        <PageHeader title="Preferências de IA" subtitle="Configurar como você usa a inteligência artificial na plataforma" />
        <Card className="border-red-200 bg-red-50">
          <CardBody className="flex items-center gap-3 text-red-700">
            <AlertCircle className="w-5 h-5" />
            Erro ao carregar preferências. Tente novamente.
          </CardBody>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <PageHeader
        title="Preferências de IA"
        subtitle="Escolha como a inteligência artificial deve funcionar para você"
      />

      <div className="mb-6">
        <p className="text-sm text-slate-600">
          Configure o comportamento da IA por funcionalidade. Você pode usar a IA interna da
          plataforma, produtos externos (ChatGPT, Claude, Gemini, GitHub Copilot) ou desabilitar
          completamente.
        </p>
      </div>

      <div className="space-y-2">
        {KNOWN_FEATURES.map((feature) => (
          <FeaturePreferenceCard
            key={feature.key}
            feature={feature}
            existing={preferences.find((p) => p.featureKey === feature.key)}
          />
        ))}
      </div>
    </PageContainer>
  );
}
