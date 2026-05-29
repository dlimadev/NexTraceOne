import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ContractImportOverlay } from '../../features/catalog/components/ContractImportOverlay';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    importContract: vi.fn().mockResolvedValue({ id: 'contract-1' }),
  },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderOverlay(props = {}) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <ContractImportOverlay
        onClose={vi.fn()}
        onSuccess={vi.fn()}
        {...props}
      />
    </QueryClientProvider>
  );
}

describe('ContractImportOverlay', () => {
  beforeEach(() => {
    vi.mocked(contractsApi.importContract).mockResolvedValue({ id: 'contract-1' } as never);
  });

  it('renders step 1 with apiAssetId input when no preselectedApiAssetId', () => {
    renderOverlay();
    expect(screen.getByPlaceholderText(/asset id|uuid/i)).toBeInTheDocument();
  });

  it('when preselectedApiAssetId is given step 1 shows the pre-filled name', () => {
    renderOverlay({ preselectedApiAssetId: 'api-123', preselectedApiAssetName: 'Payment API' });
    expect(screen.getByText(/Payment API/i)).toBeInTheDocument();
  });

  it('detecting OpenAPI content sets protocol badge', async () => {
    const user = userEvent.setup();
    renderOverlay({ preselectedApiAssetId: 'api-123', preselectedApiAssetName: 'Payment API' });
    // advance step 1 (preselected)
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // switch to Editor tab
    await user.click(screen.getByRole('button', { name: /editor/i }));
    const textarea = screen.getByRole('textbox');
    await user.type(textarea, 'openapi: "3.0.0"');
    // badge should appear
    expect(await screen.findByText(/openapi/i)).toBeInTheDocument();
  });

  it('calls importContract and onSuccess on final submit', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    renderOverlay({ preselectedApiAssetId: 'api-123', preselectedApiAssetName: 'Payment API', onSuccess });
    // Step 1 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 — switch to editor, type content
    await user.click(screen.getByRole('button', { name: /editor/i }));
    await user.type(screen.getByRole('textbox'), 'openapi: "3.0.0"');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 3 — fill version
    await user.type(screen.getByPlaceholderText(/1\.0\.0/), '1.0.0');
    await user.click(screen.getByRole('button', { name: /submit|salvar|guardar/i }));
    await waitFor(() => {
      expect(contractsApi.importContract).toHaveBeenCalledWith(
        expect.objectContaining({ apiAssetId: 'api-123', version: '1.0.0' })
      );
      expect(onSuccess).toHaveBeenCalledOnce();
    });
  });
});
