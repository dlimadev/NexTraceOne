# NexTraceOne Frontend Architecture

> Last updated: 2026-03-17 — After Etapa 6 (hardening & stabilization)

## Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Framework | React 19 + TypeScript 5.9 | UI rendering + type safety |
| Build | Vite 7 | Development server + production bundling |
| Routing | React Router 7 | Client-side navigation + lazy loading |
| State (server) | TanStack React Query 5 | Async data fetching, caching, invalidation |
| Forms | React Hook Form 7 + Zod 4 | Form state + schema validation |
| HTTP | Axios 1.13 | API communication with interceptors |
| Styling | Tailwind CSS 4 | Utility-first CSS with design tokens |
| Icons | Lucide React | Consistent icon library |
| i18n | react-i18next 15 | Internationalization (en, pt-BR, pt-PT, es) |
| Testing | Vitest 4 + Testing Library | Unit & integration tests |
| E2E | Playwright 1.58 | End-to-end browser tests |

## Directory Structure

```
src/
├── __tests__/           # Test files organized by type
│   ├── components/      # Component unit tests
│   ├── contexts/        # Context integration tests
│   ├── hooks/           # Hook tests
│   ├── pages/           # Page-level tests
│   └── utils/           # Utility function tests
├── api/                 # Central API client configuration
├── auth/                # Authentication: persona, permissions, roles
├── components/          # Shared base components
│   └── shell/           # App Shell: sidebar, topbar, page containers
├── contexts/            # React contexts (Auth, Persona)
├── features/            # Feature modules (domain-organized)
│   ├── ai-hub/          # AI assistant, models, policies
│   ├── audit-compliance/# Audit logging
│   ├── catalog/         # Service catalog, contracts, search
│   ├── change-governance/# Releases, workflow, promotion
│   ├── contracts/       # Contract workspace, builders, governance
│   ├── governance/      # Governance: compliance, risk, finops
│   ├── identity-access/ # Auth pages, user management
│   ├── integrations/    # Connector hub, ingestion
│   ├── operations/      # Incidents, runbooks, reliability
│   ├── product-analytics/# Platform analytics
│   └── shared/          # Dashboard (Command Center)
├── hooks/               # Shared custom hooks
├── lib/                 # Utilities (cn for class merging)
├── locales/             # i18n translation files
├── shared/              # Design system foundations, shared API
├── types/               # Centralized TypeScript types
└── utils/               # Utility functions (token storage, sanitize)
```

## Design System

Design tokens are defined as CSS custom properties in `src/index.css`.

### Color Tokens
- Canvas/surfaces: `--color-canvas`, `--color-panel`, `--color-card`, `--color-elevated`
- Text hierarchy: `--color-heading`, `--color-body`, `--color-muted`, `--color-faded`
- Accents: `--color-accent` (cyan), `--color-cyan`, `--color-mint`
- Semantic: `--color-success`, `--color-warning`, `--color-critical`, `--color-info`

### Typography
- Sans: Inter (system fallback)
- Mono: JetBrains Mono (technical data)
- Scale: `type-display-01` through `type-caption`, `type-mono-sm`

### Spacing
- 8pt base grid: `--spacing-1` (4px) through `--spacing-16` (64px)

### Shadows & Radius
- Depth: `--shadow-xs` through `--shadow-floating`
- Radius: `--radius-xs` (6px) through `--radius-pill` (999px)

## Component Conventions

### Base Components (`src/components/`)
- `Button` — primary/secondary/danger/ghost/subtle variants with sm/md/lg sizes
- `Card`, `CardHeader`, `CardBody` — content surface with divider support
- `Badge` — semantic variants (success/warning/danger/info)
- `EmptyState` — contextual empty state with title, description, action
- `PageHeader` — title + subtitle + badge + actions
- `Select`, `SearchInput`, `TextField`, `PasswordInput` — form inputs with label/error/helper
- `Tabs` — underline or pill variant with icons
- `Modal`, `Drawer` — overlay components with proper ARIA attributes
- `Loader`, `Skeleton` — loading state indicators

### Shell Components (`src/components/shell/`)
- `AppShell` — main layout wrapper (sidebar + topbar + content)
- `AppSidebar` — persona-aware collapsible sidebar
- `AppTopbar` — search, workspace switcher, user menu
- `PageContainer` — standardized page wrapper (max-width, padding, fade-in)
- `PageSection` — section with optional title/icon/actions header
- `ContentGrid` — responsive grid (1-4 columns)

### Page Pattern
All module pages follow this anatomy (via shell components):
1. `PageContainer` — outer wrapper
2. `PageHeader` or custom header — title + actions
3. `PageSection` — KPI strip / summary stats
4. `PageSection` — filters + content (table/cards)
5. Error/empty/loading states handled within each section

## Data Layer

### React Query Conventions
- Query keys: `['domain', 'resource']` or `['domain', 'resource', id]`
- Stale time: 30s default
- Hooks organized per feature: `features/<module>/api/`
- Loading/error handling at the component level

### API Client
- Axios instance at `src/api/` with base URL from Vite proxy
- Auth headers injected via interceptors
- Tenant context via headers when applicable

## Form Strategy

- React Hook Form for complex forms (login, admin, contract import)
- Zod schemas for validation with `@hookform/resolvers`
- Simple controlled inputs for filters and search
- Error messages via `role="alert"` for accessibility

## Testing Strategy

### Unit Tests (Vitest + Testing Library)
- Base components: Button, Card, Badge, EmptyState, Select, Tabs, PageHeader, Loader
- Shell components: AppShell, sidebar, topbar
- Auth: AuthContext, permissions, token storage
- Pages: key detail pages (ServiceDetail, IncidentDetail, ContractDetail)

### E2E Tests (Playwright)
- Auth flows: login, forgot password
- Navigation: module switching, sidebar
- Critical flows: search, filters, form submission

## Accessibility

- All form inputs have `label`/`htmlFor` or `aria-label` association
- Error messages use `role="alert"` and `aria-invalid`
- `Modal` uses native `<dialog>` element
- `Drawer` has `role="dialog"` + `aria-modal="true"`
- `Tabs` have `role="tablist"`, `role="tab"`, `aria-selected`
- Search inputs have default `aria-label="Search"`
- Keyboard navigation: Escape closes modals/drawers, Tab order preserved

## Scripts

```bash
npm run dev          # Start Vite dev server
npm run build        # TypeScript check + Vite production build
npm run typecheck    # TypeScript compilation check only
npm run lint         # ESLint check
npm run lint:fix     # ESLint auto-fix
npm run test         # Run Vitest tests
npm run test:watch   # Vitest in watch mode
npm run test:coverage # Vitest with coverage report
npm run test:e2e     # Playwright e2e tests
npm run test:e2e:ui  # Playwright with UI
```
