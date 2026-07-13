import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ServiceContractDrawer, type ContractDrawerState } from '../../features/catalog/components/ServiceContractDrawer';

vi.mock('../../features/contracts/components/ContractViewPanel', () => ({
  ContractViewPanel: ({ contractVersionId }: { contractVersionId: string }) => <div data-testid="view">{contractVersionId}</div>,
}));
vi.mock('../../features/contracts/create/ContractCreateForm', () => ({
  ContractCreateForm: ({ onCreated }: { onCreated: (id: string) => void }) => (
    <button onClick={() => onCreated('draft-9')}>mock-create</button>
  ),
}));
vi.mock('../../features/contracts/studio/components/ContractDraftEditor', () => ({
  ContractDraftEditor: ({ draftId }: { draftId: string }) => <div data-testid="editor">{draftId}</div>,
}));

describe('ServiceContractDrawer', () => {
  it('renders the view panel in view mode', () => {
    renderWithProviders(
      <ServiceContractDrawer state={{ mode: 'view', contractVersionId: 'cv-1' }} onClose={() => {}} onModeChange={() => {}} serviceId="svc-1" />,
    );
    expect(screen.getByTestId('view')).toHaveTextContent('cv-1');
  });

  it('transitions create -> edit in-place when a draft is created', () => {
    const onModeChange = vi.fn();
    renderWithProviders(
      <ServiceContractDrawer state={{ mode: 'create' }} onClose={() => {}} onModeChange={onModeChange} serviceId="svc-1" />,
    );
    screen.getByText('mock-create').click();
    expect(onModeChange).toHaveBeenCalledWith({ mode: 'edit', draftId: 'draft-9' });
  });

  it('renders nothing when closed', () => {
    const { container } = renderWithProviders(
      <ServiceContractDrawer state={{ mode: 'closed' }} onClose={() => {}} onModeChange={() => {}} serviceId="svc-1" />,
    );
    expect(container.querySelector('[role="dialog"]')).toBeNull();
  });
});
