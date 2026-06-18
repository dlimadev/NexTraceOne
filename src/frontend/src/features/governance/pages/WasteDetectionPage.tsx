import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { RefreshCw, AlertTriangle, TrendingDown, Trash2, CheckCircle2, Server, Clock } from 'lucide-react';
import { finOpsApi, type WasteSignalDetail } from '../api/finOps';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';

export function WasteDetectionPage() {
  const { t } = useTranslation('wasteDetection');
  const { activeEnvironmentId } = useEnvironment();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['waste-signals', activeEnvironmentId],
    queryFn: () => finOpsApi.getWasteSignals(),
  });

  return (
    <PageContainer>
      {/* Cabeçalho com botão refresh no slot de ações */}
      <PageHeader
        title={t('title')}
        subtitle={t('subtitle')}
        icon={<AlertTriangle size={24} />}
        actions={
          <Button
            variant="outline"
            size="sm"
            icon={<RefreshCw size={14} />}
            onClick={() => refetch()}
          >
            {t('refresh')}
          </Button>
        }
      />

      {/* Estado de carregamento padronizado */}
      {isLoading && <PageLoadingState />}

      {/* Estado de erro padronizado */}
      {isError && <PageErrorState onRetry={() => refetch()} />}

      {data && (
        <>
          {/* Cards de resumo */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-card border border-edge rounded-lg p-4">
              <p className="text-xs text-muted uppercase tracking-wide">{t('totalWaste')}</p>
              <p className="text-2xl font-bold text-warning mt-1">
                {Number(data.totalWaste).toLocaleString(undefined, { maximumFractionDigits: 2 })}
              </p>
            </div>
            <div className="bg-card border border-edge rounded-lg p-4">
              <p className="text-xs text-muted uppercase tracking-wide">{t('signalCount')}</p>
              <p className="text-2xl font-bold text-heading mt-1">{data.signalCount}</p>
            </div>
            <div className="bg-card border border-edge rounded-lg p-4">
              <p className="text-xs text-muted uppercase tracking-wide">{t('byTypeTitle')}</p>
              <div className="mt-2 space-y-1">
                {data.byType.slice(0, 3).map((bt) => (
                  <div key={bt.type} className="flex items-center justify-between text-xs">
                    <span className="text-body">{bt.type}</span>
                    <span className="font-medium text-heading">{bt.count}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Lista de sinais ou estado vazio */}
          {data.signals.length === 0 ? (
            <EmptyState
              icon={<CheckCircle2 size={32} />}
              title={t('noWaste')}
              variant="default"
            />
          ) : (
            <section>
              <h2 className="text-base font-medium text-heading mb-3">{t('signalsTitle')}</h2>
              <div className="space-y-3">
                {data.signals.map((s) => (
                  <WasteSignalCard key={s.signalId} signal={s} t={t} />
                ))}
              </div>
            </section>
          )}

          {data.isSimulated && (
            <p className="text-xs text-muted italic">{t('simulatedNote')}</p>
          )}

          <p className="text-xs text-muted">
            {t('generatedAt')}: {new Date(data.generatedAt).toLocaleString()}
          </p>
        </>
      )}
    </PageContainer>
  );
}

/* Mapeamento de severidade para variante do DS Badge e classe de borda do card */
const SEVERITY_BADGE: Record<string, 'critical' | 'warning' | 'info' | 'gray'> = {
  Critical: 'critical',
  High:     'warning',
  Medium:   'info',
  Low:      'gray',
};

/* Classe de borda semântica por severidade (tokens de design) */
const SEVERITY_CARD_CLS: Record<string, string> = {
  Critical: 'border-critical/40 bg-critical-muted',
  High:     'border-warning/40 bg-warning/5',
  Medium:   'border-info/40 bg-info/5',
  Low:      'border-edge bg-card',
};

function WasteSignalCard({
  signal,
  t,
}: {
  signal: WasteSignalDetail;
  t: (key: string) => string;
}) {
  const badgeVariant = SEVERITY_BADGE[signal.severity] ?? 'gray';
  const cardCls = SEVERITY_CARD_CLS[signal.severity] ?? SEVERITY_CARD_CLS.Low;

  const typeIcon = signal.type.includes('Idle')
    ? <Clock size={14} className="text-muted" />
    : signal.type.includes('Cpu') || signal.type.includes('Memory')
    ? <Server size={14} className="text-muted" />
    : <Trash2 size={14} className="text-muted" />;

  return (
    <div className={`border rounded-lg p-4 ${cardCls}`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className="mt-0.5">
            <AlertTriangle size={16} className="text-warning" />
          </div>
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-medium text-sm text-heading">{signal.serviceName}</span>
              {/* Badge de tipo com ícone embutido */}
              <Badge variant={badgeVariant} size="sm" className="inline-flex items-center gap-1">
                {typeIcon}
                {signal.type}
              </Badge>
              <Badge variant={badgeVariant} size="sm">{signal.severity}</Badge>
            </div>
            <p className="text-xs text-body mt-1">{signal.description}</p>
            <p className="text-xs text-muted mt-1">{signal.pattern}</p>
            {signal.correlatedCause && (
              <p className="text-xs text-muted mt-1">
                {t('correlatedCause')}: {signal.correlatedCause}
              </p>
            )}
            <div className="flex items-center gap-3 mt-2 text-xs text-muted">
              <span>{t('team')}: {signal.team}</span>
              <span>·</span>
              <span>{t('domain')}: {signal.domain}</span>
            </div>
          </div>
        </div>
        <div className="text-right flex-shrink-0">
          <div className="flex items-center gap-1 text-warning text-sm font-semibold">
            <TrendingDown size={14} />
            {Number(signal.estimatedWaste).toLocaleString(undefined, { maximumFractionDigits: 2 })}
          </div>
          <p className="text-xs text-muted">{t('estimatedWaste')}</p>
        </div>
      </div>
    </div>
  );
}
