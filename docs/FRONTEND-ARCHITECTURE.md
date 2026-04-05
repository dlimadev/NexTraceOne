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
│   ├── ai/                       # AI Hub — conversations, model registry
│   ├── audit/                    # Audit & Compliance — trails, campaigns
│   ├── catalog/                  # Service Catalog — services, contracts
│   ├── changes/                  # Change Intelligence — risk, blast radius
│   ├── configuration/            # Platform Configuration
│   ├── governance/               # Governance — policies, risk center
│   ├── identity/                 # Identity & Access — login, teams, roles
│   ├── integrations/             # Integrations — CI/CD, multi-cluster
│   ├── knowledge/                # Knowledge Hub — documents, notes, runbooks
│   ├── notifications/            # Notifications — channels, preferences
│   ├── operations/               # Operational Intelligence — incidents, SLOs
│   └── analytics/                # Product Analytics — adoption, journeys
├── shared/                       # Shared code
│   ├── components/               # Reusable UI components
│   ├── hooks/                    # Custom React hooks
│   ├── utils/                    # Utility functions
│   ├── api/                      # Centralized API client
│   └── i18n/                     # Internationalization config & translations
├── layouts/                      # Page layouts (sidebar, topbar)
└── App.tsx                       # Root component with router
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
