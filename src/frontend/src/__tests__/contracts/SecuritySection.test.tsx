/**
 * Regressão para a migração dos controlos de formulário do workspace de
 * contratos para o design system (SecuritySection / DefinitionSection partilham
 * os mesmos helpers Field/FieldArea/SelectField).
 *
 * Objetivo: garantir que a troca de `<input>/<textarea>/<select>` crus pelos
 * componentes DS `TextField/TextArea/Select` preservou o contrato controlado —
 * escrever num campo atualiza o estado e Save propaga o valor editado.
 */
import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { SecuritySection } from '../../features/contracts/workspace/sections/SecuritySection';

describe('SecuritySection — controlos DS', () => {
  it('escrever num campo de texto e Save propaga o valor editado', () => {
    const onSave = vi.fn();
    renderWithProviders(
      <SecuritySection specContent="" protocol="OpenApi" onSave={onSave} />,
    );

    const textboxes = screen.getAllByRole('textbox');
    expect(textboxes.length).toBeGreaterThan(0);

    const first = textboxes[0] as HTMLInputElement | HTMLTextAreaElement;
    fireEvent.change(first, { target: { value: 'admin:read' } });
    expect(first.value).toBe('admin:read');

    fireEvent.click(screen.getByRole('button', { name: /save/i }));

    expect(onSave).toHaveBeenCalledTimes(1);
    expect(Object.values(onSave.mock.calls[0][0])).toContain('admin:read');
  });
});
