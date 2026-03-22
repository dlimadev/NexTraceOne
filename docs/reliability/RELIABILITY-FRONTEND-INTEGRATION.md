# Reliability Frontend Integration

## Endpoints Consumed

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v1/reliability/services` | GET | List services with reliability summary |
| `/api/v1/reliability/services/{serviceId}` | GET | Service reliability detail |
| `/api/v1/reliability/teams/{teamId}/summary` | GET | Team reliability summary |

All endpoints require the `operations:reliability:read` permission.

## API Contracts

### ListServiceReliability Response

```typescript
interface ServiceReliabilityListResponse {
  items: ServiceReliabilityItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface ServiceReliabilityItem {
  serviceName: string;         // service identifier
  displayName: string;         // human-readable name
  serviceType: string;
  domain: string;
  teamName: string;
  criticality: string;
  reliabilityStatus: string;   // 'Healthy' | 'Degraded' | 'Unavailable' | 'NeedsAttention'
  operationalSummary: string;
  trend: string;               // 'Improving' | 'Stable' | 'Declining'
  activeFlags: number;         // bitmask of OperationalFlag enum
  openIncidents: number;
  recentChangeImpact: boolean;
  overallScore: number;        // 0–100
  lastComputedAt: string;      // ISO 8601 UTC
}
```

### GetServiceReliabilityDetail Response

```typescript
interface ServiceReliabilityDetailResponse {
  identity: ServiceReliabilityDetailIdentity;
  status: string;
  operationalSummary: string;
  trend: ServiceReliabilityDetailTrend;
  metrics: ServiceReliabilityDetailMetrics;
  activeFlags: number;
  recentChanges: ServiceReliabilityDetailChange[];
  linkedIncidents: ServiceReliabilityDetailIncident[];
  dependencies: ServiceReliabilityDetailDependency[];
  linkedContracts: ServiceReliabilityDetailContract[];
  runbooks: ServiceReliabilityDetailRunbook[];
  anomalySummary: string;
  coverage: ServiceReliabilityCoverage;
}
```

## UI States

Both pages implement proper loading, error, and empty states:

| State | Component |
|-------|-----------|
| Loading | `PageLoadingState` |
| Error (non-404) | `PageErrorState` with `reliability.loadError` i18n key |
| Not Found (404) | Inline message with `reliability.detail.notFound` key |
| Empty data | Empty list with `common.noResults` key |
| Success | Full page content from API response |

## Mock Removal Summary

### TeamReliabilityPage.tsx
- **Removed**: `const mockServices = [...]` (8 hardcoded service objects)
- **Removed**: `<DemoBanner />` component
- **Removed**: `import { DemoBanner }` 
- **Added**: `useQuery` calling `reliabilityApi.listServices()`
- **Added**: `import { PageLoadingState, PageErrorState }` from components
- **Added**: `import { useQuery } from '@tanstack/react-query'`

### ServiceReliabilityDetailPage.tsx
- **Removed**: `const mockDetails: Record<string, ...>` (3 hardcoded objects)
- **Removed**: `<DemoBanner />` component
- **Removed**: Hardcoded response building logic
- **Added**: `useQuery` calling `reliabilityApi.getServiceDetail(serviceId)`
- **Added**: Real data binding for all sections (metrics, incidents, coverage, etc.)
- **Added**: Proper 404 vs generic error differentiation

## Query Parameters (ListServiceReliability)

| Parameter | Type | Description |
|-----------|------|-------------|
| `teamId` | string? | Filter by team name |
| `serviceId` | string? | Filter by specific service |
| `domain` | string? | Filter by domain |
| `environment` | string? | Filter by environment |
| `status` | string? | Filter by ReliabilityStatus |
| `serviceType` | string? | Filter by service type |
| `criticality` | string? | Filter by criticality |
| `search` | string? | Search by service name |
| `page` | int | Page number (min 1) |
| `pageSize` | int | Page size (1–100) |
