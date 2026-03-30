/** Barrel export — bounded context Operations (incidentes, runbooks, consistência operacional). */
export { IncidentsPage } from './pages/IncidentsPage';
export { IncidentDetailPage } from './pages/IncidentDetailPage';
export { RunbooksPage } from './pages/RunbooksPage';
export { TeamReliabilityPage } from './pages/TeamReliabilityPage';
export { ServiceReliabilityDetailPage } from './pages/ServiceReliabilityDetailPage';
export { AutomationWorkflowsPage } from './pages/AutomationWorkflowsPage';
export { AutomationWorkflowDetailPage } from './pages/AutomationWorkflowDetailPage';
export { AutomationAdminPage } from './pages/AutomationAdminPage';
export { EnvironmentComparisonPage } from './pages/EnvironmentComparisonPage';
export { PlatformOperationsPage } from './pages/PlatformOperationsPage';

export { automationApi } from './api/automation';
export { incidentsApi } from './api/incidents';
export { platformOpsApi } from './api/platformOps';
export { reliabilityApi } from './api/reliability';
export { runtimeIntelligenceApi } from './api/runtimeIntelligence';
