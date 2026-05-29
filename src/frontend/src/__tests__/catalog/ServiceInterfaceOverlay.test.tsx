// src/frontend/src/__tests__/catalog/ServiceInterfaceOverlay.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ServiceInterfaceOverlay } from '../../features/catalog/components/ServiceInterfaceOverlay';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    createServiceInterface: vi.fn().mockResolvedValue({ id: 'iface-1', name: 'test' }),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

function renderOverlay(onClose = vi.fn(), onSuccess = vi.fn()) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <ServiceInterfaceOverlay
        serviceId="svc-123"
        serviceName="Payment Service"
        onClose={onClose}
        onSuccess={onSuccess}
      />
    </QueryClientProvider>
  );
}

describe('ServiceInterfaceOverlay', () => {
  beforeEach(() => {
    vi.mocked(serviceCatalogApi.createServiceInterface).mockResolvedValue({ id: 'iface-1', name: 'test' } as never);
  });

  it('renders step 1 with type picker on mount', () => {
    renderOverlay();
    expect(screen.getAllByRole('option').length).toBeGreaterThan(3);
  });

  it('blocks advance when name is empty', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(screen.getByText(/name.*required|nome.*obrigatório/i)).toBeInTheDocument();
  });

  it('selecting KafkaProducer on step 2 shows topic field', async () => {
    const user = userEvent.setup();
    renderOverlay();
    // Click KafkaProducer card (find by text content containing "kafka")
    const kafkaCard = screen.getAllByRole('option').find(
      (el) => el.textContent?.toLowerCase().includes('kafka')
    );
    expect(kafkaCard).toBeTruthy();
    await user.click(kafkaCard!);
    // Fill name
    const nameInput = screen.getByPlaceholderText(/interface name|nome da interface/i);
    await user.type(nameInput, 'my-topic');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 shows topic field
    expect(await screen.findByPlaceholderText(/topic|tópico/i)).toBeInTheDocument();
  });

  it('calls createServiceInterface and onSuccess on submit', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    renderOverlay(vi.fn(), onSuccess);
    // Fill name
    const nameInput = screen.getByPlaceholderText(/interface name|nome da interface/i);
    await user.type(nameInput, 'my-api');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 3 — submit
    await user.click(screen.getByRole('button', { name: /submit|salvar|guardar/i }));
    await waitFor(() => {
      expect(serviceCatalogApi.createServiceInterface).toHaveBeenCalledWith(
        expect.objectContaining({ serviceAssetId: 'svc-123', name: 'my-api' })
      );
      expect(onSuccess).toHaveBeenCalledOnce();
    });
  });
});
