export { PreflightPage } from './pages/PreflightPage';
export { SetupWizardPage } from './pages/SetupWizardPage';
export { PlatformHealthDashboardPage } from './pages/PlatformHealthDashboardPage';
export { SystemHealthPage } from './pages/SystemHealthPage';
export { platformAdminApi } from './api/platformAdmin';
export type {
  PreflightReport,
  PreflightCheckResult,
  ConfigHealthResponse,
  ConfigCheckDto,
  PendingMigrationsResponse,
  MigrationDto,
  OptionalProviderDto,
  OptionalProviderStatus,
  OptionalProvidersResponse,
} from './api/platformAdmin';
