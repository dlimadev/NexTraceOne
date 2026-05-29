import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../i18n';
import { DashboardVariablesBar } from '../../features/governance/components/DashboardVariablesBar';
import type { DashboardVariable } from '../../features/governance/types/dashboardBuilder';

vi.mock('../../features/governance/components/TimeRangePicker', () => ({
  TimeRangePicker: ({ value, onChange }: { value: string; onChange: (v: string) => void }) => (
    <button data-testid="time-picker" onClick={() => onChange('1h')}>{value}</button>
  ),
}));

const makeVariable = (overrides: Partial<DashboardVariable> = {}): DashboardVariable => ({
  name: 'service',
  label: 'Serviço',
  type: 'custom',
  options: ['payment-api', 'auth-api'],
  value: 'payment-api',
  multi: false,
  includeAll: false,
  ...overrides,
});

function renderBar(
  variables: DashboardVariable[] = [makeVariable()],
  props: Partial<Parameters<typeof DashboardVariablesBar>[0]> = {}
) {
  const onVariableChange = vi.fn();
  const onTimeRangeChange = vi.fn();
  const onAddVariable = vi.fn();
  return {
    onVariableChange,
    onTimeRangeChange,
    onAddVariable,
    ...render(
      <I18nextProvider i18n={i18n}>
        <DashboardVariablesBar
          variables={variables}
          timeRange="6h"
          onVariableChange={onVariableChange}
          onTimeRangeChange={onTimeRangeChange}
          onAddVariable={onAddVariable}
          {...props}
        />
      </I18nextProvider>
    ),
  };
}

describe('DashboardVariablesBar', () => {
  it('renders the variable label', () => {
    renderBar();
    expect(screen.getByText('Serviço')).toBeTruthy();
  });

  it('renders variable options in the select', () => {
    renderBar();
    // The component renders multiple comboboxes (variable select + refresh interval select).
    // The variable select is the first one.
    const select = screen.getAllByRole('combobox')[0] as HTMLSelectElement;
    expect(select.value).toBe('payment-api');
    const options = Array.from(select.options).map((o) => o.value);
    expect(options).toContain('payment-api');
    expect(options).toContain('auth-api');
  });

  it('calls onVariableChange when a new option is selected', () => {
    const { onVariableChange } = renderBar();
    // The component renders multiple comboboxes (variable select + refresh interval select).
    // The variable select is the first one.
    const select = screen.getAllByRole('combobox')[0];
    fireEvent.change(select, { target: { value: 'auth-api' } });
    expect(onVariableChange).toHaveBeenCalledWith('service', 'auth-api');
  });

  it('renders the time picker', () => {
    renderBar();
    expect(screen.getByTestId('time-picker')).toBeTruthy();
  });

  it('calls onTimeRangeChange when time picker fires', () => {
    const { onTimeRangeChange } = renderBar();
    fireEvent.click(screen.getByTestId('time-picker'));
    expect(onTimeRangeChange).toHaveBeenCalledWith('1h');
  });

  it('renders the + Variable button when not readOnly', () => {
    renderBar();
    const addBtn = screen.getByRole('button', { name: /variable|variável/i });
    expect(addBtn).toBeTruthy();
  });

  it('does not render + Variable button in readOnly mode', () => {
    renderBar([makeVariable()], { isReadOnly: true });
    const addBtn = screen.queryByRole('button', { name: /variable|variável/i });
    expect(addBtn).toBeNull();
  });

  it('renders a text input for text-type variables', () => {
    const textVar = makeVariable({ type: 'text', value: 'my-value' });
    renderBar([textVar]);
    const input = screen.getByDisplayValue('my-value') as HTMLInputElement;
    expect(input.type).toBe('text');
  });

  it('returns null if readOnly and no variables', () => {
    const { container } = renderBar([], { isReadOnly: true });
    expect(container.firstChild).toBeNull();
  });
});
