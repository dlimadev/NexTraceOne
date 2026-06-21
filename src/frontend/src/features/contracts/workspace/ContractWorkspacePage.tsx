import * as React from 'react';
import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { PageHeader } from '../../../components/PageHeader';
import { WorkspaceLayout } from './WorkspaceLayout';
import { WorkspaceTabs } from './components/WorkspaceTabs';
import { ContractWorkspaceIdentityCard } from './components/ContractWorkspaceIdentityCard';
import { ContractLifecycleActions } from './components/ContractLifecycleActions';
import { LoadingState, ErrorState } from '../shared/components/StateIndicators';
import {
  SummarySection,
  ContractSection,
  VersioningSection,
  ComplianceSection,
  ValidationSection,
  DefinitionSection,
  OperationsSection,
  SchemasSection,
  SecuritySection,
  ChangelogSection,
  ApprovalsSection,
  ConsumersSection,
  DependenciesSection,
  AiAgentsSection,
  ScorecardSection,
  DeploymentsSection,
} from './sections';
import { StudioRail } from './components/StudioRail';
import {
  useContractDetail,
  useContractViolations,
  useContractTransition,
  useContractExport,
} from '../hooks';
import { toStudioContract } from './toStudioContract';
import type { WorkspaceSectionId, ContractLifecycleState } from '../types';

/**
 * Página principal do studio de contrato.
 * Agrega todas as secções, acções e rail contextual sobre uma versão de contrato.
 * Usa StudioContract (enriched view-model) para alimentar secções com dados completos.
 */
export function ContractWorkspacePage() {
  const { contractVersionId } = useParams<{ contractVersionId: string }>();

  const detailQuery = useContractDetail(contractVersionId);
  const violationsQuery = useContractViolations(contractVersionId);
  const transition = useContractTransition(contractVersionId);
  const { exportVersion } = useContractExport();

  const [activeSection, setActiveSection] = useState<WorkspaceSectionId>('summary');

  const studioContract = useMemo(() => {
    if (!detailQuery.data) return null;
    return toStudioContract(detailQuery.data);
  }, [detailQuery.data]);

  if (detailQuery.isLoading) {
    return <LoadingState />;
  }

  if (detailQuery.isError || !detailQuery.data || !studioContract) {
    return <ErrorState onRetry={() => detailQuery.refetch()} />;
  }

  const detail = detailQuery.data;
  const violations = violationsQuery.data ?? [];

  const handleTransition = (targetState: ContractLifecycleState) => {
    transition.mutate(targetState);
  };

  const handleExport = async () => {
    if (!contractVersionId) return;
    try {
      await exportVersion(contractVersionId, `${studioContract.technicalName || 'contract'}-${detail.semVer}.${detail.format}`);
    } catch {
      // Error handled by toast
    }
  };

  const renderSection = (section: WorkspaceSectionId, onNavigate?: (section: WorkspaceSectionId) => void) => {
    switch (section) {
      case 'summary':
        return (
          <SummarySection
            contract={studioContract}
            violationCount={violations.length}
            onNavigate={onNavigate}
          />
        );

      case 'definition':
        return (
          <DefinitionSection
            contract={studioContract}
            isReadOnly={detail.isLocked}
          />
        );

      case 'contract':
        return (
          <ContractSection
            specContent={detail.specContent}
            format={detail.format}
            protocol={detail.protocol}
            isReadOnly={detail.isLocked}
          />
        );

      case 'operations':
        return (
          <OperationsSection
            specContent={detail.specContent}
            protocol={detail.protocol}
            isReadOnly={detail.isLocked}
            onAddOperation={onNavigate ? () => onNavigate('contract') : undefined}
          />
        );

      case 'schemas':
        return (
          <SchemasSection
            specContent={detail.specContent}
            protocol={detail.protocol}
            isReadOnly={detail.isLocked}
          />
        );

      case 'security':
        return (
          <SecuritySection
            specContent={detail.specContent}
            protocol={detail.protocol}
            isReadOnly={detail.isLocked}
          />
        );

      case 'versioning':
        return (
          <VersioningSection
            apiAssetId={detail.apiAssetId}
            currentVersionId={contractVersionId!}
          />
        );

      case 'changelog':
        return (
          <ChangelogSection
            apiAssetId={detail.apiAssetId}
            currentVersionId={contractVersionId!}
          />
        );

      case 'approvals':
        return (
          <ApprovalsSection
            contract={studioContract}
            onTransition={handleTransition}
          />
        );

      case 'compliance':
        return (
          <ComplianceSection contractVersionId={contractVersionId!} />
        );

      case 'validation':
        return (
          <ValidationSection contractVersionId={contractVersionId!} />
        );

      case 'scorecard':
        return (
          <ScorecardSection contractVersionId={contractVersionId!} />
        );

      case 'consumers':
        return <ConsumersSection contract={studioContract} />;

      case 'dependencies':
        return <DependenciesSection contract={studioContract} />;

      case 'deployments':
        return <DeploymentsSection contractVersionId={contractVersionId!} />;

      case 'ai-agents':
        return <AiAgentsSection contract={studioContract} />;

      default:
        return null;
    }
  };

  return (
    <WorkspaceLayout
      header={
        <PageHeader
          title={studioContract.technicalName}
          subtitle={`${detail.protocol} · v${detail.semVer}${studioContract.domain ? ` · ${studioContract.domain}` : ''}`}
          actions={
            <ContractLifecycleActions
              lifecycleState={detail.lifecycleState as ContractLifecycleState}
              isLocked={detail.isLocked}
              onTransition={handleTransition}
              onExport={handleExport}
            />
          }
        />
      }
      identityCard={<ContractWorkspaceIdentityCard contract={studioContract} />}
      rail={<StudioRail contract={studioContract} />}
    >
      <WorkspaceTabs activeSection={activeSection} onSelect={setActiveSection} />
      {renderSection(activeSection, setActiveSection)}
    </WorkspaceLayout>
  );
}
