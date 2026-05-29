import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../i18n';
import { PanelEditorOverlay } from '../../features/governance/components/PanelEditorOverlay';
import type { PanelEditorSlot } from '../../features/governance/components/PanelEditorOverlay';

// Mock heavy dependencies
vi.mock('../../features/governance/components/NqlMonacoEditor', () => ({
  NqlMonacoEditor: ({ value, onChange }: { value: string; onChange: (v: string) => void }) => (
    <textarea data-testid="nql-editor" value={value} onChange={(e) => onChange(e.target.value)} />
  ),
}));

vi.mock('../../features/governance/components/DataTransformPanel', () => ({
  DataTransformPanel: () => <div data-testid="transform-panel" />,
}));

vi.mock('../../features/governance/components/VisualQueryBuilder', () => ({
  VisualQueryBuilder: () => <div data-testid="visual-query-builder" />,
}));

vi.mock('../../features/governance/components/PanelVisualizationPicker', () => ({
  PanelVisualizationPicker: () => <div data-testid="viz-picker" />,
}));

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: [] }),
  },
}));

const makeSlot = (overrides: Partial<PanelEditorSlot> = {}): PanelEditorSlot => ({
  tempId: 'slot-1',
  type: 'stat',
  customTitle: 'Test Panel',
  nqlQuery: 'rate(http_requests_total)',
  chartType: 'timeseries',
  unit: 'none',
  yAxisMin: '',
  yAxisMax: '',
  thresholds: '[]',
  transforms: [],
  ...overrides,
});

function renderOverlay(
  props: Partial<Parameters<typeof PanelEditorOverlay>[0]> = {}
) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const onApply = vi.fn();
  const onClose = vi.fn();
  const slot = makeSlot();
  return {
    onApply,
    onClose,
    ...render(
      <QueryClientProvider client={qc}>
        <I18nextProvider i18n={i18n}>
          <PanelEditorOverlay
            slot={slot}
            variables={[]}
            onApply={onApply}
            onClose={onClose}
            {...props}
          />
        </I18nextProvider>
      </QueryClientProvider>
    ),
  };
}

describe('PanelEditorOverlay', () => {
  it('renders the panel title in the header input', () => {
    renderOverlay();
    const titleInput = screen.getByDisplayValue('Test Panel');
    expect(titleInput).toBeTruthy();
  });

  it('calls onClose when Discard button is clicked', () => {
    const { onClose } = renderOverlay();
    const discardBtn = screen.getByRole('button', { name: /discard|descartar/i });
    fireEvent.click(discardBtn);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('calls onApply with the draft slot when Apply is clicked', () => {
    const { onApply } = renderOverlay();
    const applyBtn = screen.getByRole('button', { name: /apply|aplicar/i });
    fireEvent.click(applyBtn);
    expect(onApply).toHaveBeenCalledTimes(1);
    expect(onApply).toHaveBeenCalledWith(expect.objectContaining({
      tempId: 'slot-1',
      type: 'stat',
    }));
  });

  it('updates the title when the user types in the title input', () => {
    const { onApply } = renderOverlay();
    const titleInput = screen.getByDisplayValue('Test Panel') as HTMLInputElement;
    fireEvent.change(titleInput, { target: { value: 'New Title' } });
    expect(titleInput.value).toBe('New Title');
    const applyBtn = screen.getByRole('button', { name: /apply|aplicar/i });
    fireEvent.click(applyBtn);
    expect(onApply).toHaveBeenCalledWith(expect.objectContaining({
      customTitle: 'New Title',
    }));
  });

  it('shows the Transform tab content when Transforms tab is clicked', () => {
    renderOverlay();
    const tabs = screen.getAllByRole('button');
    const transformTab = tabs.find((b) => /transform|transforma/i.test(b.textContent ?? ''));
    expect(transformTab).toBeTruthy();
    fireEvent.click(transformTab!);
    expect(screen.getByTestId('transform-panel')).toBeTruthy();
  });

  it('calls onClose when Back to dashboard button is clicked', () => {
    const { onClose } = renderOverlay();
    const backBtn = screen.getByRole('button', { name: /back|voltar/i });
    fireEvent.click(backBtn);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
