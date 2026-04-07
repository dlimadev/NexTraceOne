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
    { label: t('knowledge.graph.totalNodes'), value: data?.totalNodes ?? 0 },
    { label: t('knowledge.graph.totalEdges'), value: data?.totalEdges ?? 0 },
    { label: t('knowledge.graph.connectedComponents'), value: data?.connectedComponents ?? 0 },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('knowledge.graph.title')}
        subtitle={t('knowledge.graph.subtitle')}
        icon={<Network size={24} />}
        actions={
          <div className="flex items-center gap-2">
            <label className="text-sm text-gray-600 dark:text-gray-400">
              {t('knowledge.graph.maxDepth')}:
            </label>
            <select
              value={maxDepth}
              onChange={(e) => setMaxDepth(Number(e.target.value))}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              {[1, 2, 3, 4, 5].map((d) => (
                <option key={d} value={d}>{d}</option>
              ))}
            </select>
            <Button size="sm" onClick={() => refetch()}>
              <Search size={14} className="mr-1" />
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid stats={stats} />

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
                    <Circle size={16} className="mt-1 text-indigo-500 flex-shrink-0" />
                    <div className="min-w-0">
                      <p className="font-medium text-sm text-gray-900 dark:text-white truncate">
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
                    <span className="font-medium text-gray-800 dark:text-gray-200">{edge.sourceId}</span>
                    <Layers size={14} className="text-gray-400" />
                    <Badge variant="secondary" className="text-xs">{edge.relationshipType}</Badge>
                    <Layers size={14} className="text-gray-400" />
                    <span className="font-medium text-gray-800 dark:text-gray-200">{edge.targetId}</span>
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
