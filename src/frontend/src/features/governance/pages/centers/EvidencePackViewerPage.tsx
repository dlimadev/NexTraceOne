import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { Package, ShieldCheck, Download, FileCheck, Lock, Clock, Tag } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

interface EvidenceArtifact {
  id: string;
  name: string;
  type: string;
  hash: string;
  slsaLevel: number;
  signedAt: string;
  signer: string;
}

interface EvidencePackDetail {
  id: string;
  name: string;
  status: 'Draft' | 'Sealed' | 'Exported';
  createdAt: string;
  sealedAt?: string;
  changeId?: string;
  serviceName: string;
  version: string;
  artifacts: EvidenceArtifact[];
  complianceStandards: string[];
  isSimulated: boolean;
}

const useEvidencePack = (packId?: string) =>
  useQuery({
    queryKey: ['evidence-pack-viewer', packId],
    queryFn: () =>
      packId
        ? client.get<EvidencePackDetail>(`/api/v1/changes/evidence-packs/${packId}`, { params: { tenantId: 'default' } }).then((r) => r.data)
        : client.get<EvidencePackDetail>('/api/v1/changes/evidence-packs/latest', { params: { tenantId: 'default' } }).then((r) => r.data),
  });

const SLSA_COLORS = ['', 'text-warning', 'text-info', 'text-success', 'text-accent'];

const STATUS_BADGE = {
  Draft: 'secondary' as const,
  Sealed: 'success' as const,
  Exported: 'info' as const,
};

export function EvidencePackViewerPage() {
  const { t } = useTranslation();
  const { packId } = useParams<{ packId: string }>();
  const [filter, setFilter] = useState('');
  const { data, isLoading } = useEvidencePack(packId);

  const artifacts = (data?.artifacts ?? []).filter(
    (a) => !filter || a.name.toLowerCase().includes(filter.toLowerCase()) || a.type.toLowerCase().includes(filter.toLowerCase())
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('evidencePack.title')}
        subtitle={t('evidencePack.subtitle')}
        actions={
          data?.status === 'Sealed' ? (
            <Button size="sm" variant="ghost">
              <Download size={14} className="mr-1" />
              {t('evidencePack.export')}
            </Button>
          ) : undefined
        }
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : data ? (
          <>
            {/* Pack header */}
            <Card className="mb-4">
              <CardBody className="p-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3">
                    <Package size={20} className="text-accent" />
                    <div>
                      <div className="flex items-center gap-2">
                        <h2 className="text-sm font-semibold">{data.name}</h2>
                        <Badge variant={STATUS_BADGE[data.status]}>{data.status}</Badge>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        {data.serviceName} {data.version}
                      </p>
                    </div>
                  </div>
                  <div className="text-right text-xs text-muted-foreground">
                    {data.sealedAt ? (
                      <div className="flex items-center gap-1">
                        <Lock size={10} />
                        {new Date(data.sealedAt).toLocaleString()}
                      </div>
                    ) : (
                      <div className="flex items-center gap-1">
                        <Clock size={10} />
                        {new Date(data.createdAt).toLocaleString()}
                      </div>
                    )}
                  </div>
                </div>
                {data.complianceStandards.length > 0 && (
                  <div className="flex flex-wrap gap-1 mt-3">
                    {data.complianceStandards.map((s) => (
                      <Badge key={s} variant="secondary" className="text-xs">
                        <Tag size={10} className="mr-1" />
                        {s}
                      </Badge>
                    ))}
                  </div>
                )}
              </CardBody>
            </Card>

            {/* Artifacts */}
            <div className="flex items-center gap-2 mb-3">
              <FileCheck size={14} className="text-muted-foreground" />
              <h3 className="text-sm font-semibold">
                {t('evidencePack.artifacts')} ({artifacts.length})
              </h3>
              <input
                type="text"
                placeholder={t('evidencePack.filterArtifacts')}
                value={filter}
                onChange={(e) => setFilter(e.target.value)}
                className="ml-auto px-2 py-1 text-xs border rounded bg-background w-48"
              />
            </div>

            <div className="space-y-2">
              {artifacts.map((art) => (
                <Card key={art.id}>
                  <CardBody className="p-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-0.5">
                          <ShieldCheck size={12} className={SLSA_COLORS[art.slsaLevel] ?? 'text-muted-foreground'} />
                          <span className="text-sm font-medium truncate">{art.name}</span>
                          <Badge variant="secondary" className="text-xs">{art.type}</Badge>
                        </div>
                        <p className="text-xs font-mono text-muted-foreground truncate">{art.hash}</p>
                        <div className="text-xs text-muted-foreground mt-0.5">
                          {art.signer} · {new Date(art.signedAt).toLocaleString()}
                        </div>
                      </div>
                      <Badge
                        variant={art.slsaLevel >= 3 ? 'success' : art.slsaLevel >= 2 ? 'warning' : 'secondary'}
                        className="text-xs shrink-0"
                      >
                        SLSA {art.slsaLevel}
                      </Badge>
                    </div>
                  </CardBody>
                </Card>
              ))}
              {artifacts.length === 0 && (
                <div className="text-center p-8 text-muted-foreground text-sm">
                  {t('evidencePack.noArtifacts')}
                </div>
              )}
            </div>

            <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
              {t('sotCenter.simulatedBanner')}
            </div>
          </>
        ) : null}
      </PageSection>
    </PageContainer>
  );
}
