import * as React from 'react';
import { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { WorkspaceLayout } from './WorkspaceLayout';
import { ContractHeader, ContractQuickActions } from '../shared/components';
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
} from './sections';
import { StudioRail } from './components/StudioRail';
import {
  useContractDetail,
  useContractViolations,
  useContractTransition,
  useContractExport,
} from '../hooks';
import { toStudioContract } from './toStudioContract';
import type { WorkspaceSectionId } from '../types';
import type { ContractLifecycleState } from '../types';

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

      case 'consumers':
        return <ConsumersSection contract={studioContract} />;

      case 'dependencies':
        return <DependenciesSection contract={studioContract} />;

      default:
        return null;
    }
  };

  return (
    <WorkspaceLayout
      header={
        <ContractHeader
          title={studioContract.technicalName}
          friendlyName={studioContract.friendlyName}
          version={detail.semVer}
          protocol={detail.protocol}
          lifecycleState={detail.lifecycleState}
          serviceType={studioContract.serviceType}
          domain={studioContract.domain || undefined}
          owner={studioContract.owner || undefined}
          isLocked={detail.isLocked}
          isSigned={!!detail.signedBy}
          actions={
            <ContractQuickActions
              lifecycleState={detail.lifecycleState as ContractLifecycleState}
              isLocked={detail.isLocked}
              isSigned={!!detail.signedBy}
              onTransition={handleTransition}
              onExport={handleExport}
            />
          }
        />
      }
      rail={
        <StudioRail
          contract={studioContract}
          onTransition={handleTransition}
        />
      }
    >
      {(section, navigate) => renderSection(section, navigate)}
    </WorkspaceLayout>
  );
}
