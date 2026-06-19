# Betterstack Operations DS Polish — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply minimal surgical DS polish to three Operations pages (RunbooksPage, OnCallSchedulePage, ErrorTrackingPage) so every control uses design-system components and no raw HTML inputs/buttons remain.

**Architecture:** Pure view-layer changes only — no data fetching, mutations, query keys, or i18n keys touched. Each page gets its own commit. Branch `redesign/betterstack-foundation` already exists.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4, DS components from `src/frontend/src/components/` — specifically `PageHeader` (actions prop), `SearchInput` (size="sm"), `Button` (variant="primary", icon prop), `Tabs` (variant="pill", size="sm").

---

## Pre-flight: Confirm branch and install state

- [ ] **Step 1: Verify branch**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git branch --show-current
# deve mostrar: redesign/betterstack-foundation
```

- [ ] **Step 2: Verify npm deps installed**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm install
```

---

## Task 1: RunbooksPage.tsx

**Files:**
- Modify: `src/frontend/src/features/operations/pages/RunbooksPage.tsx`

### What to change (confirmed by reading the file)

1. Move the "Create New" `<button>` from the loose `<PageSection>` below the header into `PageHeader actions` prop as a DS `<Button variant="primary" icon={<Plus size={14} />}>`.
2. Replace the raw `<div className="relative">…<input type="text">` search block with `<SearchInput size="sm" …/>`.
3. Remove now-unused imports: `Search` (lucide), the raw `<input>` wrapper `<div className="relative">`, the standalone `<PageSection>` that only contained the button.

The `Plus` import is already present. `Search` from lucide can be removed (SearchInput has its own icon).

- [ ] **Step 3: Read the file to confirm current state before editing**

File: `src/frontend/src/features/operations/pages/RunbooksPage.tsx`

Current raw button (lines 64–71):
```tsx
<PageSection>
  <div className="flex justify-end mb-4">
    <button
      onClick={() => navigate('/operations/runbooks/create')}
      className="flex items-center gap-1 px-3 py-2 rounded-md bg-accent text-white text-sm font-medium hover:bg-accent/90 transition-colors"
    >
      <Plus size={14} />
      {t('runbooks.builder.createNew')}
    </button>
  </div>
</PageSection>
```

Current raw search input (lines 82–93):
```tsx
<div className="mb-4">
  <div className="relative">
    <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
    <input
      type="text"
      placeholder={t('runbooks.searchPlaceholder', 'Search runbooks...')}
      value={search}
      onChange={(e) => setSearch(e.target.value)}
      className="w-full pl-10 pr-4 py-2 rounded-md border border-edge bg-input text-sm text-body placeholder:text-muted focus:border-accent focus:ring-1 focus:ring-accent transition-colors"
    />
  </div>
</div>
```

- [ ] **Step 4: Apply the edit to RunbooksPage.tsx**

Replace the import line for Search (remove it) and add SearchInput import. Final import block:

```tsx
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, FileText, Clock, Server, AlertTriangle, Plus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { SearchInput } from '../../../components/SearchInput';
import { incidentsApi } from '../api/incidents';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
```

Move the CTA into PageHeader actions and remove the standalone PageSection:

```tsx
<PageContainer>
  <PageHeader
    title={t('runbooks.title')}
    subtitle={t('runbooks.subtitle')}
    actions={
      <Button
        variant="primary"
        size="sm"
        icon={<Plus size={14} />}
        onClick={() => navigate('/operations/runbooks/create')}
      >
        {t('runbooks.builder.createNew')}
      </Button>
    }
  />

  <StatsGrid columns={4}>
    {/* ... stats unchanged ... */}
  </StatsGrid>

  <PageSection>
    <div className="mb-4">
      <SearchInput
        size="sm"
        placeholder={t('runbooks.searchPlaceholder', 'Search runbooks...')}
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
    </div>
    {/* ... rest unchanged ... */}
  </PageSection>
</PageContainer>
```

Complete final file content for RunbooksPage.tsx:

```tsx
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BookOpen, FileText, Clock, Server, AlertTriangle, Plus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { SearchInput } from '../../../components/SearchInput';
import { incidentsApi } from '../api/incidents';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

interface RunbookSummaryDto {
  runbookId: string;
  title: string;
  summary: string;
  linkedServiceId: string | null;
  linkedIncidentType: string | null;
  stepCount: number;
  createdAt: string;
}

interface RunbooksResponse {
  runbooks: RunbookSummaryDto[];
}

/**
 * Página de Runbooks — procedimentos operacionais e guias de mitigação.
 * Parte do módulo Operations do NexTraceOne.
 */
export function RunbooksPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');

  const { data, isLoading, isError, refetch } = useQuery<RunbooksResponse>({
    queryKey: ['runbooks', search, activeEnvironmentId],
    queryFn: () => incidentsApi.listRunbooks(search ? { search } : undefined),
  });

  const runbooks = data?.runbooks ?? [];
  const filtered = runbooks;
  const totalCount = filtered.length;
  const withService = filtered.filter((r) => r.linkedServiceId).length;
  const avgSteps = totalCount > 0 ? Math.round(filtered.reduce((s, r) => s + r.stepCount, 0) / totalCount) : 0;

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={() => refetch()} />;

  return (
    <PageContainer>
      {/* CTA principal movido para actions do PageHeader (padrão DS) */}
      <PageHeader
        title={t('runbooks.title')}
        subtitle={t('runbooks.subtitle')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={() => navigate('/operations/runbooks/create')}
          >
            {t('runbooks.builder.createNew')}
          </Button>
        }
      />

      <StatsGrid columns={4}>
        <StatCard title={t('runbooks.stats.total')} value={totalCount} icon={<BookOpen size={20} />} color="text-accent" />
        <StatCard title={t('runbooks.stats.withService')} value={withService} icon={<Server size={20} />} color="text-info" />
        <StatCard title={t('runbooks.stats.avgSteps')} value={avgSteps} icon={<FileText size={20} />} color="text-warning" />
        <StatCard title={t('runbooks.stats.incidentTypes')} value={new Set(filtered.map((r) => r.linkedIncidentType).filter(Boolean)).size} icon={<AlertTriangle size={20} />} color="text-critical" />
      </StatsGrid>

      <PageSection>
        {/* SearchInput DS substitui o input raw com ícone manual */}
        <div className="mb-4">
          <SearchInput
            size="sm"
            placeholder={t('runbooks.searchPlaceholder', 'Search runbooks...')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        {filtered.length === 0 ? (
          <Card>
            <CardBody>
              <EmptyState
                icon={<BookOpen size={24} />}
                title={t('runbooks.emptyTitle')}
                description={t('productPolish.emptyRunbooks')}
              />
            </CardBody>
          </Card>
        ) : (
          <div className="space-y-3">
            {filtered.map((runbook) => (
              <Card key={runbook.runbookId}>
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-2">
                      <BookOpen size={16} className="text-accent shrink-0" />
                      <h3 className="text-sm font-semibold text-heading">{runbook.title}</h3>
                    </div>
                    <div className="flex items-center gap-2">
                      {runbook.linkedIncidentType && (
                        <Badge variant="warning">{runbook.linkedIncidentType}</Badge>
                      )}
                      <Badge variant="default">{runbook.stepCount} {t('runbooks.steps', 'steps')}</Badge>
                    </div>
                  </div>
                </CardHeader>
                <CardBody>
                  <p className="text-xs text-muted mb-2">{runbook.summary}</p>
                  <div className="flex items-center gap-4 text-xs text-muted">
                    {runbook.linkedServiceId && (
                      <span className="flex items-center gap-1">
                        <Server size={12} /> {runbook.linkedServiceId}
                      </span>
                    )}
                    <span className="flex items-center gap-1">
                      <Clock size={12} /> {new Date(runbook.createdAt).toLocaleDateString()}
                    </span>
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
```

- [ ] **Step 5: Run build (from src/frontend)**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: build completes with 0 errors.

- [ ] **Step 6: No existing test for RunbooksPage — skip test step**

No test file exists under `src/frontend/src/features/operations/`. Nothing to run.

- [ ] **Step 7: Commit RunbooksPage**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/operations/pages/RunbooksPage.tsx
git commit -m "feat(operations): jornada Betterstack na RunbooksPage (DS controls)"
```

---

## Task 2: OnCallSchedulePage.tsx

**Files:**
- Modify: `src/frontend/src/features/operations/pages/OnCallSchedulePage.tsx`

### What to change

1. Remove the manual flex wrapper (`<div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">`) around PageHeader. Move the tab strip and action buttons into `PageHeader actions`.
2. Replace the raw tab strip (a `<div className="flex rounded-md border border-edge overflow-hidden text-xs">` with `<button>` children) with `<Tabs variant="pill" size="sm" items={…} activeId={timeRange} onChange={(id) => setTimeRange(id as TimeRange)} />`.
3. Fix `bg-emerald-500` hardcoded Tailwind color on the "on-call now" dot → replace with `bg-success` (DS token).
4. Add `Tabs` import from `../../../components/Tabs`.
5. Remove `CalendarDays` from lucide imports only if not used elsewhere (it was in PageHeader icon — keep it).

The `Tabs` component expects `items: Array<{ id: string; label: string; icon?: ReactNode; disabled?: boolean }>`. The `TIME_RANGE_OPTIONS` array holds `{ value, labelKey }` — map it at render time into `{ id, label }` using `t(opt.labelKey)`.

- [ ] **Step 8: Read the file to confirm current state before editing**

File: `src/frontend/src/features/operations/pages/OnCallSchedulePage.tsx`

Raw tab strip (lines 89–100):
```tsx
<div className="flex rounded-md border border-edge overflow-hidden text-xs">
  {TIME_RANGE_OPTIONS.map((opt) => (
    <button
      key={opt.value}
      type="button"
      onClick={() => setTimeRange(opt.value)}
      className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-accent text-on-accent font-semibold' : 'hover:bg-muted text-muted'}`}
    >
      {t(opt.labelKey)}
    </button>
  ))}
</div>
```

Manual wrapper around PageHeader (lines 82–110):
```tsx
<div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
  <PageHeader ... />
  <div className="flex items-center gap-2 flex-wrap">
    {/* tab strip + buttons */}
  </div>
</div>
```

Hardcoded color dot (line 165):
```tsx
<div className="w-2 h-2 rounded-full bg-emerald-500" />
```

- [ ] **Step 9: Apply the edit to OnCallSchedulePage.tsx**

Complete final file content for OnCallSchedulePage.tsx:

```tsx
/**
 * OnCallSchedulePage — Gestão de escalas de plantão com políticas de escalação e overrides.
 *
 * Centraliza rotações de on-call (weekly/follow-the-sun/custom), políticas de escalação
 * por níveis e gestão de substituições temporárias com rastreabilidade completa.
 *
 * @module operations/incidents
 * @pillar Operational Reliability, Operational Consistency
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CalendarDays, RefreshCw, Plus } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Tabs } from '../../../components/Tabs';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getOnCallSchedules, type OnCallSchedule } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'onCallSchedule.timeRange.1h' },
  { value: '6h', labelKey: 'onCallSchedule.timeRange.6h' },
  { value: '24h', labelKey: 'onCallSchedule.timeRange.24h' },
  { value: '7d', labelKey: 'onCallSchedule.timeRange.7d' },
];

function timeRangeToInterval(range: TimeRange) {
  const until = new Date();
  const from = new Date(until);
  switch (range) {
    case '1h': from.setHours(from.getHours() - 1); break;
    case '6h': from.setHours(from.getHours() - 6); break;
    case '24h': from.setHours(from.getHours() - 24); break;
    case '7d': from.setDate(from.getDate() - 7); break;
  }
  return { from: from.toISOString(), until: until.toISOString() };
}

const FALLBACK: OnCallSchedule[] = [
  { id: '1', name: 'Payments On-Call', teamName: 'Platform Team', serviceName: 'payment-service', currentOnCall: 'João Silva', nextOnCall: 'Maria Costa', rotationType: 'weekly', timezone: 'America/Sao_Paulo', escalationLevels: 3, activeOverrides: 0, environment: 'production' },
  { id: '2', name: 'Orders On-Call', teamName: 'Orders Team', serviceName: 'order-service', currentOnCall: 'Carlos Mendes', nextOnCall: 'Ana Ferreira', rotationType: 'weekly', timezone: 'Europe/Lisbon', escalationLevels: 2, activeOverrides: 1, environment: 'production' },
  { id: '3', name: 'Infra On-Call - Follow-the-Sun', teamName: 'Infrastructure Team', serviceName: 'infra-services', currentOnCall: 'Pedro Santos', nextOnCall: 'Lisa Anderson', rotationType: 'followTheSun', timezone: 'UTC', escalationLevels: 4, activeOverrides: 0, environment: 'production' },
  { id: '4', name: 'Auth On-Call', teamName: 'Security Team', serviceName: 'auth-service', currentOnCall: 'Rita Alves', nextOnCall: 'Miguel Sousa', rotationType: 'custom', timezone: 'America/Sao_Paulo', escalationLevels: 3, activeOverrides: 2, environment: 'production' },
];

export function OnCallSchedulePage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['on-call-schedules', environment, timeRange, refreshKey],
    queryFn: () => getOnCallSchedules({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const schedules = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const onCallNow = schedules.length;
  const totalOverrides = schedules.reduce((a, s) => a + s.activeOverrides, 0);
  const totalEscalations = schedules.filter((s) => s.escalationLevels > 0).length;

  /* Itens do Tabs DS derivados das opções de intervalo de tempo */
  const tabItems = TIME_RANGE_OPTIONS.map((opt) => ({
    id: opt.value,
    label: t(opt.labelKey),
  }));

  return (
    <PageContainer>
      {/* PageHeader com tab strip e CTAs nas actions — padrão DS */}
      <PageHeader
        title={t('onCallSchedule.title')}
        subtitle={t('onCallSchedule.subtitle')}
        icon={<CalendarDays className="w-5 h-5" />}
        actions={
          <div className="flex items-center gap-2 flex-wrap">
            <Tabs
              variant="pill"
              size="sm"
              items={tabItems}
              activeId={timeRange}
              onChange={(id) => setTimeRange(id as TimeRange)}
            />
            <Button variant="outline" size="sm" onClick={handleRefresh}>
              <RefreshCw className="w-3.5 h-3.5 mr-1.5" />
              {t('common.refresh')}
            </Button>
            <Button size="sm" icon={<Plus className="w-3.5 h-3.5" />}>
              {t('onCallSchedule.overrides.add')}
            </Button>
          </div>
        }
      />

      {isError && <PageErrorState message={t('onCallSchedule.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('onCallSchedule.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('onCallSchedule.stats.activeSchedules'), value: String(schedules.length) },
                { label: t('onCallSchedule.stats.onCallNow'), value: String(onCallNow) },
                { label: t('onCallSchedule.stats.overrides'), value: String(totalOverrides) },
                { label: t('onCallSchedule.stats.escalations'), value: String(totalEscalations) },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-3">
                    <div className="text-xs text-muted mb-1">{stat.label}</div>
                    <div className="text-2xl font-bold tabular-nums">{stat.value}</div>
                  </CardBody>
                </Card>
              ))}
            </div>
          </PageSection>

          <PageSection>
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold">{t('onCallSchedule.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {schedules.length === 0 ? (
                  <div className="p-8 text-center text-muted text-sm">{t('onCallSchedule.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-edge bg-muted/40 text-xs text-muted">
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.schedule')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.team')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.currentOnCall')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.nextOnCall')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.rotationType')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('onCallSchedule.table.timezone')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {schedules.map((s) => (
                          <tr key={s.id} className="border-b border-edge/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{s.name}</td>
                            <td className="px-4 py-2.5 text-muted">{s.teamName}</td>
                            <td className="px-4 py-2.5"><Badge variant="secondary">{s.serviceName}</Badge></td>
                            <td className="px-4 py-2.5">
                              <div className="flex items-center gap-1.5">
                                {/* Token DS bg-success substitui bg-emerald-500 hardcoded */}
                                <div className="w-2 h-2 rounded-full bg-success" />
                                <span className="font-medium">{s.currentOnCall}</span>
                              </div>
                            </td>
                            <td className="px-4 py-2.5 text-muted">{s.nextOnCall}</td>
                            <td className="px-4 py-2.5">
                              <Badge variant="info">{t(`onCallSchedule.rotationTypes.${s.rotationType}`)}</Badge>
                            </td>
                            <td className="px-4 py-2.5 text-xs text-muted">{s.timezone}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
```

- [ ] **Step 10: Run build**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: 0 errors.

- [ ] **Step 11: No existing test for OnCallSchedulePage — skip test step**

- [ ] **Step 12: Commit OnCallSchedulePage**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/operations/pages/OnCallSchedulePage.tsx
git commit -m "feat(operations): jornada Betterstack na OnCallSchedulePage (DS controls)"
```

---

## Task 3: ErrorTrackingPage.tsx

**Files:**
- Modify: `src/frontend/src/features/operations/pages/ErrorTrackingPage.tsx`

### What to change

1. Same pattern as OnCallSchedulePage: remove manual flex wrapper, move the tab strip and Refresh button into `PageHeader actions`.
2. Replace raw tab strip with `<Tabs variant="pill" size="sm" items={tabItems} activeId={timeRange} onChange={(id) => setTimeRange(id as TimeRange)} />`.
3. Add `Tabs` import from `../../../components/Tabs`.

No dot color to fix here. No raw inputs.

- [ ] **Step 13: Read the file to confirm current state before editing**

File: `src/frontend/src/features/operations/pages/ErrorTrackingPage.tsx`

Manual wrapper + raw tab strip (lines 92–114):
```tsx
<div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
  <PageHeader
    title={t('errorTracking.title')}
    subtitle={t('errorTracking.subtitle')}
  />
  <div className="flex items-center gap-2 flex-wrap">
    <div className="flex rounded-md border border-edge overflow-hidden text-xs">
      {TIME_RANGE_OPTIONS.map((opt) => (
        <button
          key={opt.value}
          type="button"
          onClick={() => setTimeRange(opt.value)}
          className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-accent text-on-accent font-semibold' : 'hover:bg-muted text-muted'}`}
        >
          {t(opt.labelKey)}
        </button>
      ))}
    </div>
    <Button variant="outline" size="sm" onClick={handleRefresh}>
      <RefreshCw className="w-3.5 h-3.5 mr-1.5" />
      {t('common.refresh')}
    </Button>
  </div>
</div>
```

- [ ] **Step 14: Apply the edit to ErrorTrackingPage.tsx**

Complete final file content for ErrorTrackingPage.tsx:

```tsx
/**
 * ErrorTrackingPage — Rastreamento e gestão de grupos de erros com correlação de deploy.
 *
 * Agrupa erros por fingerprint, exibe contagem, utilizadores afetados e status,
 * permitindo correlação direta com deploys para análise de causa raiz.
 *
 * @module operations/telemetry
 * @pillar Operational Reliability, Change Intelligence
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Tabs } from '../../../components/Tabs';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getErrorGroups, type ErrorGroup, type ErrorGroupStatus } from '../api/telemetry';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'errorTracking.timeRange.1h' },
  { value: '6h', labelKey: 'errorTracking.timeRange.6h' },
  { value: '24h', labelKey: 'errorTracking.timeRange.24h' },
  { value: '7d', labelKey: 'errorTracking.timeRange.7d' },
];

function timeRangeToInterval(range: TimeRange) {
  const until = new Date();
  const from = new Date(until);
  switch (range) {
    case '1h': from.setHours(from.getHours() - 1); break;
    case '6h': from.setHours(from.getHours() - 6); break;
    case '24h': from.setHours(from.getHours() - 24); break;
    case '7d': from.setDate(from.getDate() - 7); break;
  }
  return { from: from.toISOString(), until: until.toISOString() };
}

const FALLBACK: ErrorGroup[] = [
  { id: '1', fingerprint: 'a1b2c3d4', message: 'NullPointerException in OrderProcessor.process()', serviceName: 'order-service', count: 342, affectedUsers: 89, status: 'regressing', firstSeen: new Date(Date.now() - 86400000 * 3).toISOString(), lastSeen: new Date(Date.now() - 600000).toISOString(), deployCorrelated: true, deployId: 'deploy-001', environment: 'production' },
  { id: '2', fingerprint: 'e5f6g7h8', message: 'Connection timeout to payment-gateway after 30000ms', serviceName: 'payment-service', count: 128, affectedUsers: 34, status: 'new', firstSeen: new Date(Date.now() - 3600000).toISOString(), lastSeen: new Date(Date.now() - 60000).toISOString(), deployCorrelated: true, deployId: 'deploy-002', environment: 'production' },
  { id: '3', fingerprint: 'i9j0k1l2', message: 'Invalid schema: field "price" missing from response', serviceName: 'catalog-service', count: 56, affectedUsers: 12, status: 'new', firstSeen: new Date(Date.now() - 7200000).toISOString(), lastSeen: new Date(Date.now() - 300000).toISOString(), deployCorrelated: false, environment: 'production' },
  { id: '4', fingerprint: 'm3n4o5p6', message: 'Unhandled promise rejection: ECONNREFUSED 127.0.0.1:5432', serviceName: 'notification-service', count: 23, affectedUsers: 5, status: 'resolved', firstSeen: new Date(Date.now() - 172800000).toISOString(), lastSeen: new Date(Date.now() - 43200000).toISOString(), deployCorrelated: false, environment: 'production' },
  { id: '5', fingerprint: 'q7r8s9t0', message: 'Rate limit exceeded for external API calls', serviceName: 'integration-service', count: 891, affectedUsers: 203, status: 'ignored', firstSeen: new Date(Date.now() - 604800000).toISOString(), lastSeen: new Date(Date.now() - 1800000).toISOString(), deployCorrelated: false, environment: 'production' },
];

function statusVariant(status: ErrorGroupStatus): 'danger' | 'warning' | 'success' | 'secondary' {
  switch (status) {
    case 'new': return 'danger';
    case 'regressing': return 'warning';
    case 'resolved': return 'success';
    case 'ignored': return 'secondary';
  }
}

export function ErrorTrackingPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [timeRange, setTimeRange] = useState<TimeRange>('24h');
  const [refreshKey, setRefreshKey] = useState(0);

  const interval = timeRangeToInterval(timeRange);
  const environment = activeEnvironmentId ?? 'production';

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['error-groups', environment, timeRange, refreshKey],
    queryFn: () => getErrorGroups({ environment, from: interval.from, until: interval.until }),
    staleTime: 30_000,
    retry: false,
  });

  const groups = (data && data.length > 0) ? data : FALLBACK;

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const newCount = groups.filter((g) => g.status === 'new').length;
  const regressingCount = groups.filter((g) => g.status === 'regressing').length;
  const totalAffectedUsers = groups.reduce((a, g) => a + g.affectedUsers, 0);

  /* Itens do Tabs DS derivados das opções de intervalo de tempo */
  const tabItems = TIME_RANGE_OPTIONS.map((opt) => ({
    id: opt.value,
    label: t(opt.labelKey),
  }));

  return (
    <PageContainer>
      {/* PageHeader com tab strip e Refresh nas actions — padrão DS */}
      <PageHeader
        title={t('errorTracking.title')}
        subtitle={t('errorTracking.subtitle')}
        actions={
          <div className="flex items-center gap-2 flex-wrap">
            <Tabs
              variant="pill"
              size="sm"
              items={tabItems}
              activeId={timeRange}
              onChange={(id) => setTimeRange(id as TimeRange)}
            />
            <Button variant="outline" size="sm" onClick={handleRefresh}>
              <RefreshCw className="w-3.5 h-3.5 mr-1.5" />
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      {isError && <PageErrorState message={t('errorTracking.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('errorTracking.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('errorTracking.stats.totalGroups'), value: String(groups.length) },
                { label: t('errorTracking.stats.newErrors'), value: String(newCount) },
                { label: t('errorTracking.stats.regressing'), value: String(regressingCount) },
                { label: t('errorTracking.stats.affectedUsers'), value: String(totalAffectedUsers) },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-3">
                    <div className="text-xs text-muted mb-1">{stat.label}</div>
                    <div className="text-2xl font-bold tabular-nums">{stat.value}</div>
                  </CardBody>
                </Card>
              ))}
            </div>
          </PageSection>

          <PageSection>
            <Card>
              <CardHeader>
                <h3 className="text-sm font-semibold">{t('errorTracking.title')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                {groups.length === 0 ? (
                  <div className="p-8 text-center text-muted text-sm">{t('errorTracking.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-edge bg-muted/40 text-xs text-muted">
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.fingerprint')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.message')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.service')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.count')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.users')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.status')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('errorTracking.table.lastSeen')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {groups.map((g) => (
                          <tr key={g.id} className="border-b border-edge/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-mono text-xs text-muted">{g.fingerprint}</td>
                            <td className="px-4 py-2.5 max-w-xs truncate font-medium text-xs" title={g.message}>{g.message}</td>
                            <td className="px-4 py-2.5">{g.serviceName}</td>
                            <td className="px-4 py-2.5 tabular-nums font-semibold">{g.count.toLocaleString()}</td>
                            <td className="px-4 py-2.5 tabular-nums">{g.affectedUsers}</td>
                            <td className="px-4 py-2.5">
                              <Badge variant={statusVariant(g.status)}>
                                {t(`errorTracking.status.${g.status}`)}
                              </Badge>
                            </td>
                            <td className="px-4 py-2.5 text-xs text-muted">{new Date(g.lastSeen).toLocaleString()}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
```

- [ ] **Step 15: Run build**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend
npm run build
```

Expected: 0 errors.

- [ ] **Step 16: No existing test for ErrorTrackingPage — skip test step**

- [ ] **Step 17: Commit ErrorTrackingPage**

```bash
cd C:\Users\dlima\Documents\GitHub\NexTraceOne
git add src/frontend/src/features/operations/pages/ErrorTrackingPage.tsx
git commit -m "feat(operations): jornada Betterstack na ErrorTrackingPage (DS controls)"
```

---

## Self-Review

### Spec coverage check

| Requirement | Covered by |
|---|---|
| PageHeader with primary CTA in actions | Task 1 (RunbooksPage) |
| Raw `<input>` → SearchInput size="sm" | Task 1 (RunbooksPage) |
| Raw tab-strip buttons → DS Tabs variant="pill" size="sm" | Tasks 2 & 3 |
| CTA lives in PageHeader actions as Button variant="primary" | Task 1 |
| No legacy tokens (bg-emerald-500 hardcoded) | Task 2 (bg-success) |
| Keep exported component names | All tasks — names unchanged |
| No data-fetching / i18n key changes | All tasks — only view layer |
| Build passes after each file | Steps 5, 10, 15 |
| Commits per file with exact paths | Steps 7, 12, 17 |

### Placeholder scan
No TBD or TODO in this plan. All code is complete and final.

### Type consistency
- `Tabs` `onChange` receives `string`; cast to `TimeRange` via `id as TimeRange` — consistent across Tasks 2 and 3.
- `Button` `icon` prop takes `ReactNode` — `<Plus size={14} />` is valid.
- `SearchInput` `size="sm"` matches the `'sm' | 'md' | 'lg'` union in the component.

---

**Plan complete.** Three focused tasks, one commit each. No tests exist so no test updates needed.
