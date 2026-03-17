import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Select } from '../../components/Select';

const options = [
  { value: 'a', label: 'Option A' },
  { value: 'b', label: 'Option B' },
  { value: 'c', label: 'Option C', disabled: true },
];

describe('Select', () => {
  it('renderiza todas as opções', () => {
    render(<Select options={options} />);
    const select = screen.getByRole('combobox');
    expect(select).toBeInTheDocument();
    expect(select.querySelectorAll('option')).toHaveLength(3);
  });

  it('renderiza label quando fornecida', () => {
    render(<Select label="Ambiente" options={options} />);
    expect(screen.getByText('Ambiente')).toBeInTheDocument();
  });

  it('renderiza placeholder como primeira opção desabilitada', () => {
    render(<Select options={options} placeholder="Selecione..." />);
    const firstOption = screen.getByRole('combobox').querySelector('option:first-child');
    expect(firstOption).toHaveTextContent('Selecione...');
    expect(firstOption).toBeDisabled();
  });

  it('renderiza mensagem de erro', () => {
    render(<Select options={options} label="Env" error="Campo obrigatório" />);
    expect(screen.getByRole('alert')).toHaveTextContent('Campo obrigatório');
  });

  it('aplica aria-invalid quando há erro', () => {
    render(<Select options={options} label="Env" error="Erro" />);
    expect(screen.getByRole('combobox')).toHaveAttribute('aria-invalid', 'true');
  });

  it('renderiza helper text', () => {
    render(<Select options={options} label="Env" helperText="Escolha um ambiente" />);
    expect(screen.getByText('Escolha um ambiente')).toBeInTheDocument();
  });

  it('não renderiza helper text quando há erro', () => {
    render(<Select options={options} label="Env" helperText="Ajuda" error="Erro" />);
    expect(screen.queryByText('Ajuda')).not.toBeInTheDocument();
  });

  it('desabilita opções individuais', () => {
    render(<Select options={options} />);
    const opts = screen.getByRole('combobox').querySelectorAll('option');
    expect(opts[2]).toBeDisabled();
  });

  it('responde a onChange', async () => {
    const onChange = vi.fn();
    render(<Select options={options} onChange={onChange} />);
    await userEvent.selectOptions(screen.getByRole('combobox'), 'b');
    expect(onChange).toHaveBeenCalled();
  });
});
