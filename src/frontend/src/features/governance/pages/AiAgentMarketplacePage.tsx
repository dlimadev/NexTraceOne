import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Bot, Search, Star, Shield, Coins, Play, Pause } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

interface AiAgent {
  id: string;
  name: string;
  description: string;
  category: string;
  provider: string;
  model: string;
  tokensPerMonth: number;
  budgetUsed: number;
  budgetLimit: number;
  status: 'Active' | 'Paused' | 'Disabled';
  rating: number;
  auditEnabled: boolean;
}

const useAiAgents = () =>
  useQuery({
    queryKey: ['ai-agent-marketplace'],
    queryFn: () =>
      client
        .get<{ agents: AiAgent[]; totalTokensUsed: number; totalBudget: number; isSimulated: boolean }>(
          '/api/v1/ai/agents',
          { params: { tenantId: 'default' } }
        )
        .then((r) => r.data),
  });

const CATEGORIES = ['all', 'governance', 'contracts', 'operations', 'finops', 'security', 'analytics'];

export function AiAgentMarketplacePage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('all');
  const { data, isLoading } = useAiAgents();

  const agents = (data?.agents ?? []).filter(
    (a) =>
      (category === 'all' || a.category === category) &&
      (!search || a.name.toLowerCase().includes(search.toLowerCase()) || a.description.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('aiAgentMarketplace.title')}
        subtitle={t('aiAgentMarketplace.subtitle')}
      />
      <PageSection>
        {/* Token budget overview */}
        {data && (
          <div className="grid grid-cols-3 gap-3 mb-6">
            <Card>
              <CardBody className="p-3">
                <p className="text-xs text-muted-foreground mb-1">{t('aiAgentMarketplace.totalAgents')}</p>
                <p className="text-2xl font-bold">{data.agents.length}</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="p-3">
                <div className="flex items-center gap-1 text-warning mb-1">
                  <Coins size={12} />
                  <p className="text-xs">{t('aiAgentMarketplace.tokensUsed')}</p>
                </div>
                <p className="text-2xl font-bold">{(data.totalTokensUsed / 1000).toFixed(0)}k</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="p-3">
                <div className="flex items-center gap-1 text-accent mb-1">
                  <Shield size={12} />
                  <p className="text-xs">{t('aiAgentMarketplace.budgetRemaining')}</p>
                </div>
                <p className="text-2xl font-bold">${((data.totalBudget - data.totalTokensUsed * 0.0001)).toFixed(0)}</p>
              </CardBody>
            </Card>
          </div>
        )}

        {/* Filters */}
        <div className="flex flex-wrap gap-2 mb-4">
          <div className="relative flex-1 min-w-48">
            <Search size={12} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              placeholder={t('aiAgentMarketplace.search')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-7 pr-3 py-1.5 text-xs border rounded bg-background"
            />
          </div>
          <div className="flex gap-1 flex-wrap">
            {CATEGORIES.map((cat) => (
              <button
                key={cat}
                onClick={() => setCategory(cat)}
                className={`px-2.5 py-1.5 text-xs font-medium rounded-md transition-colors ${
                  category === cat ? 'bg-accent text-accent-foreground' : 'bg-muted text-muted-foreground hover:bg-muted/80'
                }`}
              >
                {cat}
              </button>
            ))}
          </div>
        </div>

        {isLoading ? (
          <PageLoadingState />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {agents.map((agent) => {
              const budgetPct = agent.budgetLimit > 0 ? (agent.budgetUsed / agent.budgetLimit) * 100 : 0;
              return (
                <Card key={agent.id} className="group">
                  <CardBody className="p-4">
                    <div className="flex items-start justify-between gap-3 mb-2">
                      <div className="flex items-center gap-2">
                        <div className="p-1.5 rounded-md bg-muted">
                          <Bot size={14} className="text-accent" />
                        </div>
                        <div>
                          <p className="text-sm font-semibold">{agent.name}</p>
                          <p className="text-xs text-muted-foreground">{agent.provider} · {agent.model}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-1.5">
                        {agent.auditEnabled && (
                          <Badge variant="secondary" className="text-xs">
                            <Shield size={10} className="mr-1" />
                            {t('aiAgentMarketplace.audited')}
                          </Badge>
                        )}
                        <Badge
                          variant={agent.status === 'Active' ? 'success' : agent.status === 'Paused' ? 'warning' : 'secondary'}
                          className="text-xs"
                        >
                          {agent.status}
                        </Badge>
                      </div>
                    </div>

                    <p className="text-xs text-muted-foreground mb-3">{agent.description}</p>

                    <div className="flex items-center gap-1 mb-2 text-xs text-muted-foreground">
                      {Array.from({ length: 5 }).map((_, i) => (
                        <Star key={i} size={10} className={i < agent.rating ? 'text-warning fill-warning' : ''} />
                      ))}
                      <span className="ml-1">{agent.rating}/5</span>
                    </div>

                    <div className="mb-3">
                      <div className="flex justify-between text-xs text-muted-foreground mb-1">
                        <span>{t('aiAgentMarketplace.budget')}</span>
                        <span>{agent.budgetUsed.toLocaleString()} / {agent.budgetLimit.toLocaleString()} tokens</span>
                      </div>
                      <div className="w-full bg-muted rounded-full h-1.5">
                        <div
                          className={`h-1.5 rounded-full ${budgetPct > 80 ? 'bg-destructive' : budgetPct > 60 ? 'bg-warning' : 'bg-success'}`}
                          style={{ width: `${Math.min(budgetPct, 100)}%` }}
                        />
                      </div>
                    </div>

                    <div className="flex gap-2">
                      {agent.status === 'Active' ? (
                        <Button size="sm" variant="ghost" className="flex-1 text-xs">
                          <Pause size={10} className="mr-1" />
                          {t('aiAgentMarketplace.pause')}
                        </Button>
                      ) : (
                        <Button size="sm" variant="ghost" className="flex-1 text-xs">
                          <Play size={10} className="mr-1" />
                          {t('aiAgentMarketplace.activate')}
                        </Button>
                      )}
                      <Button size="sm" variant="ghost" className="text-xs">
                        {t('aiAgentMarketplace.configure')}
                      </Button>
                    </div>
                  </CardBody>
                </Card>
              );
            })}
            {agents.length === 0 && (
              <div className="col-span-2 text-center p-8 text-muted-foreground text-sm">
                {t('aiAgentMarketplace.empty')}
              </div>
            )}
          </div>
        )}

        <div className="mt-4 p-3 rounded-lg bg-muted/40 text-xs text-muted-foreground">
          {t('sotCenter.simulatedBanner')}
        </div>
      </PageSection>
    </PageContainer>
  );
}
