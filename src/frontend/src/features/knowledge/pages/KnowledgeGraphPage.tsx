import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Network, Search, Layers, Circle } from 'lucide-react';
import { EmptyState } from '../../../components/EmptyState';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Select } from '../../../components/Select';
import { StatCard } from '../../../components/StatCard';
import client from '../../../api/client';

interface GraphNode {
  id: string;
  label: string;
  type: string;
}

interface GraphEdge {
  sourceId: string;
  targetId: string;
  relationshipType: string;
}

interface KnowledgeGraphOverviewResponse {
  totalNodes: number;
  totalEdges: number;
  connectedComponents: number;
  nodes: GraphNode[];
  edges: GraphEdge[];
}

const useKnowledgeGraph = (maxDepth: number) =>
  useQuery({
    queryKey: ['knowledge-graph', maxDepth],
    queryFn: () =>
      client
        .get<KnowledgeGraphOverviewResponse>('/knowledge/graph', { params: { maxDepth } })
        .then((r) => r.data),
  });

export function KnowledgeGraphPage() {
  const { t } = useTranslation();
  const [maxDepth, setMaxDepth] = useState(2);
  const { data, isLoading, isError, refetch } = useKnowledgeGraph(maxDepth);

  if (isLoading) return <PageLoadingState message={t('knowledge.graph.loading')} />;
  if (isError) return <PageErrorState message={t('knowledge.graph.error')} onRetry={() => refetch()} />;

  const stats = [
    { label: t('knowledge.graph.totalNodes'), value: data?.totalNodes ?? 0, color: 'text-accent' as const },
    { label: t('knowledge.graph.totalEdges'), value: data?.totalEdges ?? 0, color: 'text-info' as const },
    { label: t('knowledge.graph.connectedComponents'), value: data?.connectedComponents ?? 0, color: 'text-success' as const },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('knowledge.graph.title')}
        subtitle={t('knowledge.graph.subtitle')}
        icon={<Network size={24} />}
        actions={
          /* Controlos de cabeçalho: Select DS substitui raw <select> */
          <div className="flex items-center gap-2">
            <Select
              size="sm"
              label={t('knowledge.graph.maxDepth')}
              value={String(maxDepth)}
              options={[1, 2, 3, 4, 5].map((d) => ({ value: String(d), label: String(d) }))}
              onChange={(e) => setMaxDepth(Number(e.target.value))}
            />
            <Button
              size="sm"
              variant="outline"
              icon={<Search size={14} />}
              onClick={() => refetch()}
            >
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      {/* KPIs via StatCard DS — substituem divs artesanais */}
      <StatsGrid>
        {stats.map((s) => (
          <StatCard key={s.label} title={s.label} value={s.value} color={s.color} />
        ))}
      </StatsGrid>

      <PageSection title={t('knowledge.graph.nodesSection')}>
        {!data?.nodes?.length ? (
          <EmptyState
            icon={<Circle size={18} />}
            title={t('knowledge.graph.noNodes', 'No nodes found')}
            description={t('knowledge.graph.noNodesHint', 'Nodes will appear here once knowledge data is ingested.')}
            size="compact"
          />
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {data.nodes.map((node) => (
              <Card key={node.id} className="hover:shadow-md transition-shadow">
                <CardBody className="p-4">
                  <div className="flex items-start gap-3">
                    {/* text-indigo-500 → token semântico text-accent */}
                    <Circle size={16} className="mt-1 text-accent flex-shrink-0" />
                    <div className="min-w-0">
                      <p className="font-medium text-sm text-heading truncate">
                        {node.label}
                      </p>
                      <Badge variant="info" className="mt-1 text-xs">{node.type}</Badge>
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      <PageSection title={t('knowledge.graph.edgesSection')}>
        {!data?.edges?.length ? (
          <EmptyState
            icon={<Layers size={18} />}
            title={t('knowledge.graph.noEdges', 'No relationships found')}
            description={t('knowledge.graph.noEdgesHint', 'Relationships will appear here once connected knowledge is available.')}
            size="compact"
          />
        ) : (
          <div className="space-y-2">
            {data.edges.map((edge, idx) => (
              <Card key={idx}>
                <CardBody className="p-3">
                  <div className="flex items-center gap-2 text-sm">
                    <span className="font-medium text-body">{edge.sourceId}</span>
                    <Layers size={14} className="text-faded" />
                    <Badge variant="secondary" className="text-xs">{edge.relationshipType}</Badge>
                    <Layers size={14} className="text-faded" />
                    <span className="font-medium text-body">{edge.targetId}</span>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
