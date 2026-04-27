import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { GitBranch, AlertTriangle, Users, Globe, BarChart3 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../../components/shell';
import { PageHeader } from '../../../../components/PageHeader';
import { Card, CardBody } from '../../../../components/Card';
import { Badge } from '../../../../components/Badge';
import { PageLoadingState } from '../../../../components/PageLoadingState';
import client from '../../../../api/client';

const useBlastRadius = (releaseId?: string) =>
  useQuery({
    queryKey: ['blast-radius', releaseId],
    queryFn: () =>
      client
        .get(releaseId ? `/api/v1/changes/releases/${releaseId}/blast-radius` : '/api/v1/changes/blast-radius/latest', {
          params: { tenantId: 'default' },
        })
        .then((r) => r.data as {
          bucket?: string;
          directImpact?: number;
          transitiveImpact?: number;
          domains?: string[];
          environments?: string[];
          impactedNodes?: Array<{ service: string; impactLevel: string; reason: string }>;
          isSimulated?: boolean;
        }),
  });

const BUCKET_COLOR = {
  Zero: 'text-success',
  Small: 'text-info',
  Medium: 'text-warning',
  Large: 'text-destructive',
} as const;

export function BlastRadiusExplorerPage() {
  const { t } = useTranslation();
  const { releaseId } = useParams<{ releaseId: string }>();
  const { data, isLoading } = useBlastRadius(releaseId);

  return (
    <PageContainer>
      <PageHeader
        title={t('blastRadius.title')}
        subtitle={t('blastRadius.subtitle')}
      />
      <PageSection>
        {isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              <Card>
                <CardBody className="p-4">
                  <p className="text-xs text-muted-foreground mb-1">{t('blastRadius.bucket')}</p>
                  <p className={`text-2xl font-bold ${BUCKET_COLOR[data?.bucket as keyof typeof BUCKET_COLOR] ?? 'text-foreground'}`}>
                    {data?.bucket ?? 'Medium'}
                  </p>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-1 text-warning mb-1">
                    <Users size={12} />
                    <p className="text-xs">{t('blastRadius.directImpact')}</p>
                  </div>
                  <p className="text-2xl font-bold">{data?.directImpact ?? 0}</p>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-1 text-info mb-1">
                    <GitBranch size={12} />
                    <p className="text-xs">{t('blastRadius.transitiveImpact')}</p>
                  </div>
                  <p className="text-2xl font-bold">{data?.transitiveImpact ?? 0}</p>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="p-4">
                  <div className="flex items-center gap-1 text-accent mb-1">
                    <Globe size={12} />
                    <p className="text-xs">{t('blastRadius.domains')}</p>
                  </div>
                  <p className="text-2xl font-bold">{data?.domains?.length ?? 0}</p>
                </CardBody>
              </Card>
            </div>

            {/* Impacted Nodes */}
            <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <BarChart3 size={14} />
              {t('blastRadius.impactedServices')}
            </h3>
            <div className="space-y-2">
              {(data?.impactedNodes ?? []).map((node) => (
                <Card key={node.service}>
                  <CardBody className="p-3 flex items-center gap-3">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">{node.service}</p>
                      <p className="text-xs text-muted-foreground">{node.reason}</p>
                    </div>
                    <Badge
                      variant={node.impactLevel === 'critical' ? 'destructive' : node.impactLevel === 'high' ? 'warning' : 'secondary'}
                    >
                      {node.impactLevel}
                    </Badge>
                  </CardBody>
                </Card>
              ))}
              {(data?.impactedNodes ?? []).length === 0 && (
                <div className="p-4 text-center text-sm text-muted-foreground">
                  {t('blastRadius.noImpact')}
                </div>
              )}
            </div>

            {data?.isSimulated && (
              <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
                {t('sotCenter.simulatedBanner')}
              </div>
            )}
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
