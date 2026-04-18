# Frontend Architecture

## Technology Stack

| Category | Technology | Version |
|----------|-----------|---------|
| **Framework** | React | 19.x |
| **Language** | TypeScript | ~5.9.x |
| **Build Tool** | Vite | 7.x |
| **Routing** | react-router-dom | 7.x |
| **Data Fetching** | TanStack Query (React Query) | 5.x |
| **Styling** | Tailwind CSS | 4.x |
| **UI Primitives** | Radix UI | Latest |
| **Charts** | Apache ECharts (via echarts-for-react) | Latest |
| **Code Editor** | Monaco Editor (via @monaco-editor/react) | 4.x |
| **Forms** | react-hook-form + zod | Latest |
| **i18n** | i18next + react-i18next | Latest |
| **Icons** | Lucide React | Latest |
| **HTTP Client** | Axios | 1.x |
| **Unit Tests** | Vitest + @testing-library/react | Latest |
| **E2E Tests** | Playwright | Latest |

## Architecture Principles

- Feature-based architecture aligned with bounded contexts from the backend.
- Persona-aware layouts — the same module renders differently per role (Engineer, Tech Lead, Architect, Executive, etc.).
- All UI text must use i18n (4 locales: en, pt-BR, pt-PT, es).
- API calls via centralized Axios client with CSRF protection.
- Reusable design system with Tailwind CSS and Radix UI primitives.

## Directory Structure

```
src/frontend/src/
├── features/                     # Feature modules (bounded contexts)
│   ├── ai-hub/                   # AI Hub — conversations, model registry
│   ├── audit-compliance/         # Audit & Compliance — trails, campaigns
│   ├── catalog/                  # Service Catalog — services, contracts
│   ├── change-governance/        # Change Intelligence — risk, blast radius
│   ├── configuration/            # Platform Configuration
│   ├── governance/               # Governance — policies, risk center
│   ├── identity-access/          # Identity & Access — login, teams, roles
│   ├── integrations/             # Integrations — CI/CD, connectors, webhooks
│   ├── knowledge/                # Knowledge Hub — documents, notes, runbooks
│   ├── notifications/            # Notifications — channels, preferences
│   ├── operations/               # Operational Intelligence — incidents, SLOs
│   └── product-analytics/        # Product Analytics — adoption, journeys
├── shared/                       # Shared code
│   ├── api/                      # Re-export do Axios client centralizado
│   └── lib/                      # Utilitários partilhados
├── contexts/                     # React Contexts globais
│   ├── AuthContext.tsx            # Autenticação e utilizador ativo
│   ├── EnvironmentContext.tsx     # Ambiente ativo do tenant
│   ├── PersonaContext.tsx         # Persona derivada do papel do utilizador
│   ├── ThemeContext.tsx           # Tema (dark/light)
│   └── BrandingContext.tsx        # Customização visual por tenant
├── routes/                       # Route grouping por módulo
│   ├── catalogRoutes.tsx
│   ├── changesRoutes.tsx
│   ├── aiHubRoutes.tsx
│   └── …
├── components/                   # Componentes de shell e design system
│   ├── shell/                    # Sidebar, Topbar, ContextStrip
│   └── ui/                       # Primitives do design system
├── locales/                      # Ficheiros de tradução JSON
│   ├── en.json
│   ├── pt-BR.json
│   ├── pt-PT.json
│   └── es.json
├── api/                          # Cliente HTTP centralizado
│   └── client.ts
└── App.tsx                       # Root component com router e providers
```

## Key Patterns

### API Integration
All API calls go through a centralized Axios instance with:
- CSRF double-submit cookie pattern
- JWT token handling via httpOnly cookies
- Automatic retry and error handling
- TanStack Query for caching and server state management

### Routing
Uses `react-router-dom` v7 with feature-based route grouping.
Each bounded context module registers its own routes.
113 routes organized by product module.

### State Management
- **Server state**: TanStack Query (React Query v5)
- **Form state**: react-hook-form with zod validation
- **Local UI state**: React useState/useReducer (no global store needed)

### Testing Strategy
- **Unit tests**: Vitest with @testing-library/react for component testing
- **E2E tests**: Playwright for critical user flows
- **Coverage**: Vitest runs with coverage enabled (`npm test -- --coverage`)

---

## Como Criar um Novo Módulo de Feature

Cada feature module alinha-se com um bounded context do backend. A estrutura é consistente entre módulos.

### Estrutura de um Feature Module

```
src/frontend/src/features/{modulo}/
├── pages/                    # Componentes de página (nível de rota)
│   ├── {Modulo}DashboardPage.tsx
│   └── {Aggregate}DetailPage.tsx
├── components/               # Componentes específicos do módulo
│   ├── {Aggregate}Card.tsx
│   └── {Aggregate}Form.tsx
├── api/                      # Funções de API do módulo
│   └── {modulo}.ts
└── index.ts                  # Barrel export (opcional)
```

### Exemplo — Criar feature module "analytics"

**1. Criar estrutura de pastas:**

```bash
mkdir -p src/frontend/src/features/product-analytics/{pages,components,api}
```

**2. Criar o módulo de API (`api/analytics.ts`):**

```typescript
// src/frontend/src/features/product-analytics/api/analytics.ts
import apiClient from '../../../api/client';

export interface AnalyticsSummaryDto {
  totalEvents: number;
  uniqueUsers: number;
  adoptionScore: number;
  valueScore: number;
  frictionScore: number;
  periodLabel: string;
}

export const analyticsApi = {
  getSummary: (params?: {
    persona?: string;
    module?: string;
    range?: string;
  }) =>
    apiClient
      .get<AnalyticsSummaryDto>('/analytics/summary', { params })
      .then((r) => r.data),
};
```

**3. Criar o componente de página:**

```tsx
// src/frontend/src/features/product-analytics/pages/AnalyticsDashboardPage.tsx
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { analyticsApi } from '../api/analytics';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

export function AnalyticsDashboardPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();

  const { data, isLoading, isError } = useQuery({
    queryKey: queryKeys.analytics.summary(activeEnvironmentId),
    queryFn: () => analyticsApi.getSummary(),
  });

  if (isLoading) return <AnalyticsSkeleton />;
  if (isError) return <ErrorState message={t('analytics.error.loadFailed')} />;
  if (!data) return <EmptyState message={t('analytics.empty.noData')} />;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">{t('analytics.title')}</h1>
      <div className="grid grid-cols-3 gap-4">
        <MetricCard
          label={t('analytics.metrics.totalEvents')}
          value={data.totalEvents}
        />
        <MetricCard
          label={t('analytics.metrics.uniqueUsers')}
          value={data.uniqueUsers}
        />
        <MetricCard
          label={t('analytics.metrics.adoptionScore')}
          value={`${data.adoptionScore}%`}
        />
      </div>
    </div>
  );
}
```

**4. Adicionar rota:**

```tsx
// src/frontend/src/routes/analyticsRoutes.tsx
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

const AnalyticsDashboardPage = lazy(() =>
  import('../features/product-analytics/pages/AnalyticsDashboardPage')
    .then(m => ({ default: m.AnalyticsDashboardPage }))
);

export function analyticsRoutes() {
  return (
    <Route element={<ProtectedRoute requiredPermission="analytics:read" />}>
      <Route path="/analytics" element={<AnalyticsDashboardPage />} />
    </Route>
  );
}
```

**5. Registar rota no App.tsx:**

```tsx
// src/frontend/src/App.tsx
import { analyticsRoutes } from './routes/analyticsRoutes';

// Dentro do <Routes>:
{analyticsRoutes()}
```

---

## TanStack Query Patterns

### Query Keys Factory

Todos os query keys são centralizados em `src/frontend/src/shared/api/queryKeys.ts`. Este padrão garante invalidação consistente e evita strings soltas.

```typescript
// src/frontend/src/shared/api/queryKeys.ts
export const queryKeys = {
  // ── Analytics ──
  analytics: {
    all: ['analytics'] as const,
    summary: (envId?: string | null) =>
      [...queryKeys.analytics.all, 'summary', envId] as const,
    heatmap: (module?: string, envId?: string | null) =>
      [...queryKeys.analytics.all, 'heatmap', module, envId] as const,
    journeys: (params?: Record<string, unknown>) =>
      [...queryKeys.analytics.all, 'journeys', params] as const,
  },

  // ── Integrations ──
  integrations: {
    all: ['integrations'] as const,
    connectors: {
      all: () => [...queryKeys.integrations.all, 'connectors'] as const,
      list: (params?: Record<string, unknown>) =>
        [...queryKeys.integrations.connectors.all(), 'list', params] as const,
      detail: (id: string) =>
        [...queryKeys.integrations.connectors.all(), 'detail', id] as const,
    },
    webhooks: {
      list: () => [...queryKeys.integrations.all, 'webhooks'] as const,
    },
  },
};
```

**Regra de ambiente:** Queries que retornam dados ambiente-específicos devem incluir `envId` como último elemento da key. Isto garante cache separado por ambiente e permite invalidação por prefixo.

### useQuery — Padrão de uso

```typescript
import { useQuery } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

function useConnectorsList(params?: ConnectorFilters) {
  const { activeEnvironmentId } = useEnvironment();

  return useQuery({
    queryKey: queryKeys.integrations.connectors.list({ ...params, envId: activeEnvironmentId }),
    queryFn: () => integrationsApi.listConnectors({ ...params, environment: activeEnvironmentId }),
    staleTime: 30_000,  // 30 segundos antes de refetch
  });
}
```

### useMutation — Padrão de uso

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../../../shared/api/queryKeys';

function useCreateConnector() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: integrationsApi.createConnector,
    onSuccess: () => {
      // Invalidar a lista após criação — força refetch
      queryClient.invalidateQueries({
        queryKey: queryKeys.integrations.connectors.all(),
      });
    },
  });
}

// Uso no componente:
const { mutate: createConnector, isPending } = useCreateConnector();

const handleSubmit = (data: CreateConnectorForm) => {
  createConnector(data, {
    onSuccess: () => toast.success(t('integrations.connector.created')),
    onError: () => toast.error(t('integrations.connector.createFailed')),
  });
};
```

---

## Como Adicionar uma Nova Rota

As rotas são agrupadas por módulo em ficheiros separados em `src/frontend/src/routes/`.

### Padrão de um ficheiro de rotas

```tsx
// src/frontend/src/routes/integrationsRoutes.tsx
import { lazy } from 'react';
import { Route } from 'react-router-dom';
import { ProtectedRoute } from '../components/ProtectedRoute';

// Code splitting automático com lazy loading
const IntegrationHubPage = lazy(() =>
  import('../features/integrations/pages/IntegrationHubPage')
);
const ConnectorDetailPage = lazy(() =>
  import('../features/integrations/pages/ConnectorDetailPage')
);

export function integrationsRoutes() {
  return (
    // ProtectedRoute valida autenticação e permissão antes de renderizar
    <Route element={<ProtectedRoute requiredPermission="integrations:read" />}>
      <Route path="/integrations" element={<IntegrationHubPage />} />
      <Route
        path="/integrations/connectors/:id"
        element={<ConnectorDetailPage />}
      />
    </Route>
  );
}
```

**Importante:** Cada página usa `lazy()` com `import()` dinâmico para code splitting automático. O Vite gera chunks separados por página.

---

## Como Adicionar Novas Chaves de i18n

O NexTraceOne suporta 4 locales: `en`, `pt-BR`, `pt-PT`, `es`. Os ficheiros de tradução estão em `src/frontend/src/locales/`.

### Estrutura dos ficheiros de tradução

Os ficheiros são JSON com namespacing implícito por módulo:

```json
// src/frontend/src/locales/en.json (trecho)
{
  "common": {
    "actions": {
      "save": "Save",
      "cancel": "Cancel",
      "delete": "Delete"
    },
    "states": {
      "loading": "Loading…",
      "error": "Something went wrong",
      "empty": "No items found"
    }
  },
  "integrations": {
    "title": "Integration Hub",
    "connector": {
      "created": "Connector created successfully",
      "createFailed": "Failed to create connector",
      "fields": {
        "name": "Connector name",
        "type": "Connector type"
      }
    },
    "empty": {
      "noConnectors": "No connectors configured yet"
    }
  }
}
```

### Passos para adicionar novas chaves

1. **Adicionar em `en.json` primeiro** (fonte canónica):

```json
{
  "integrations": {
    "webhooks": {
      "title": "Webhook Subscriptions",
      "createButton": "Add Webhook",
      "empty": "No webhook subscriptions yet"
    }
  }
}
```

2. **Adicionar nos outros 3 locales** (`pt-BR.json`, `pt-PT.json`, `es.json`):

```json
// pt-BR.json
{
  "integrations": {
    "webhooks": {
      "title": "Assinaturas de Webhook",
      "createButton": "Adicionar Webhook",
      "empty": "Nenhuma assinatura de webhook configurada"
    }
  }
}
```

3. **Usar no componente**:

```tsx
import { useTranslation } from 'react-i18next';

export function WebhookSubscriptionsPage() {
  const { t } = useTranslation();

  return (
    <div>
      <h1>{t('integrations.webhooks.title')}</h1>
      <button>{t('integrations.webhooks.createButton')}</button>
    </div>
  );
}
```

**Regra absoluta:** Nunca usar texto hardcoded em português ou inglês visível ao utilizador. Todo texto usa `t('chave.traduzivel')`.

---

## Error Handling Patterns

O NexTraceOne tem três componentes padrão para estados de erro, distinguindo o contexto:

### ErrorState — Erros em secções de conteúdo

Usado quando uma secção específica falha ao carregar, mas o layout permanece intacto.

```tsx
import { ErrorState } from '../../components/ui/ErrorState';

function ConnectorListSection() {
  const { data, isError, error, refetch } = useConnectorsList();

  if (isError) {
    return (
      <ErrorState
        title={t('integrations.error.loadFailed')}
        description={t('common.error.tryAgain')}
        onRetry={refetch}
      />
    );
  }
  // …
}
```

### EmptyState — Ausência de dados (sem erro)

Usado quando a query retorna sucesso mas sem itens — convida à criação.

```tsx
import { EmptyState } from '../../components/ui/EmptyState';

function ConnectorListSection() {
  const { data } = useConnectorsList();

  if (data?.items.length === 0) {
    return (
      <EmptyState
        title={t('integrations.empty.noConnectors')}
        description={t('integrations.empty.noConnectorsHint')}
        action={
          <button onClick={openCreateDialog}>
            {t('integrations.connector.createFirst')}
          </button>
        }
      />
    );
  }
  // …
}
```

### PageErrorState — Erros que afetam a página inteira

Usado no Error Boundary a nível de rota quando a página falha catastroficamente.

```tsx
// Geralmente configurado no router, não manualmente
<Route errorElement={<PageErrorState />} path="/integrations" element={<IntegrationHubPage />} />
```

---

## Loading States

Três padrões distintos para estados de carregamento:

### Skeleton — Para listas e conteúdo estruturado

Preferível quando a estrutura do conteúdo é conhecida antecipadamente.

```tsx
import { Skeleton } from '../../components/ui/Skeleton';

function ConnectorList() {
  const { data, isLoading } = useConnectorsList();

  if (isLoading) {
    return (
      <div className="space-y-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-16 w-full rounded-lg" />
        ))}
      </div>
    );
  }
  // …
}
```

### Spinner / Loader — Para ações e transições rápidas

Para mutations, submissão de formulários e transições curtas.

```tsx
import { Loader2 } from 'lucide-react';

<button disabled={isPending}>
  {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
  {isPending ? t('common.states.saving') : t('common.actions.save')}
</button>
```

### Suspense — Para lazy routes

React Suspense com fallback para rotas carregadas com `lazy()`.

```tsx
// App.tsx — configurado uma vez para todas as rotas
<Suspense fallback={<PageLoadingFallback />}>
  <Routes>
    {/* rotas lazy aqui */}
  </Routes>
</Suspense>
```

---

## Como Aceder EnvironmentContext e PersonaContext

Estes contextos são usados em toda a aplicação para adaptar UX ao ambiente e persona do utilizador.

### EnvironmentContext

```tsx
import { useEnvironment } from '../../contexts/EnvironmentContext';

function ServiceList() {
  const {
    activeEnvironmentId,    // ID do ambiente ativo
    activeEnvironment,      // objeto completo com nome, perfil, isProductionLike
    environments,           // lista de todos os ambientes disponíveis
    setActiveEnvironment,   // trocar o ambiente ativo
  } = useEnvironment();

  // Avisar o utilizador que está a ver dados de produção
  const isProd = activeEnvironment?.isProductionLike;

  return (
    <div>
      {isProd && (
        <ProductionWarningBanner environment={activeEnvironment.name} />
      )}
      {/* … */}
    </div>
  );
}
```

### PersonaContext

```tsx
import { usePersona } from '../../contexts/PersonaContext';

function Sidebar() {
  const { persona, config } = usePersona();

  // config.navigation define os itens do menu para esta persona
  // config.quickActions define as ações rápidas no home
  // persona é: 'engineer' | 'tech-lead' | 'architect' | 'executive' | …

  return (
    <nav>
      {config.navigation.map(item => (
        <SidebarItem key={item.path} {...item} />
      ))}
    </nav>
  );
}
```

---

## API Client Patterns

O cliente Axios centralizado está em `src/frontend/src/api/client.ts` e injeta automaticamente:

- **Authorization**: `Bearer <access_token>` de sessionStorage
- **X-Tenant-Id**: tenant ativo
- **X-Environment-Id**: ambiente ativo
- **X-CSRF-Token**: em mutations (POST/PUT/PATCH/DELETE)

```typescript
// Uso direto (apenas em módulos de api/)
import apiClient from '../../../api/client';

// GET com parâmetros
const data = await apiClient
  .get<ConnectorListResponse>('/integrations/connectors', {
    params: { status: 'Active', page: 1, pageSize: 20 },
  })
  .then(r => r.data);

// POST com body
const created = await apiClient
  .post<{ id: string }>('/integrations/connectors', {
    name: 'My GitLab',
    connectorType: 'GitLab',
  })
  .then(r => r.data);
```

**Regra:** Nunca usar `apiClient` directamente em componentes — criar um módulo de API por feature em `features/{modulo}/api/{modulo}.ts`.

---

## Testing Patterns Frontend

### Wrapper de Providers para Testes

Criar um helper reutilizável que encapsula todos os providers necessários:

```typescript
// src/frontend/src/test-utils/renderWithProviders.tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { I18nextProvider } from 'react-i18next';
import i18n from '../i18n';

interface RenderOptions {
  initialPath?: string;
}

export function renderWithProviders(
  ui: React.ReactElement,
  { initialPath = '/' }: RenderOptions = {}
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <MemoryRouter initialEntries={[initialPath]}>
          {ui}
        </MemoryRouter>
      </I18nextProvider>
    </QueryClientProvider>
  );
}
```

### Teste com MSW para chamadas de API

```typescript
import { server } from '../../../mocks/server';
import { http, HttpResponse } from 'msw';
import { renderWithProviders } from '../../../test-utils/renderWithProviders';
import { IntegrationHubPage } from '../pages/IntegrationHubPage';

describe('IntegrationHubPage', () => {
  it('renders connector list', async () => {
    // Override do handler padrão para este teste
    server.use(
      http.get('/api/v1/integrations/connectors', () =>
        HttpResponse.json({
          items: [{ id: '1', name: 'GitLab CI', connectorType: 'GitLab', status: 'Active' }],
          totalCount: 1,
        })
      )
    );

    renderWithProviders(<IntegrationHubPage />);

    expect(await screen.findByText('GitLab CI')).toBeInTheDocument();
  });
});
```

---

*Última atualização: Março 2026.*
