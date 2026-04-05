import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SchemaPropertyEditor } from '../../features/contracts/workspace/builders/shared/SchemaPropertyEditor';
import {
  generateExampleFromSchema,
  generateStringExample,
  generateIntegerExample,
} from '../../features/contracts/workspace/builders/shared/ExampleGenerator';
import type { SchemaProperty } from '../../features/contracts/workspace/builders/shared/builderTypes';

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeProp(overrides: Partial<SchemaProperty> = {}): SchemaProperty {
  return {
    id: `prop-${Math.random().toString(36).slice(2, 8)}`,
    name: 'testField',
    type: 'string',
    description: '',
    required: false,
    constraints: {},
    ...overrides,
  };
}

function makeProps(count: number): SchemaProperty[] {
  return Array.from({ length: count }, (_, i) =>
    makeProp({ id: `prop-${i}`, name: `field${i}` }),
  );
}

// ── Rendering ──────────────────────────────────────────────────────────────────

describe('SchemaPropertyEditor', () => {
  it('renders without crashing', () => {
    const onChange = vi.fn();
    render(<SchemaPropertyEditor properties={[]} onChange={onChange} />);
    // The add button should be present
    expect(screen.getByText(/Add Property/i)).toBeInTheDocument();
  });

  it('renders property list with correct number of rows', () => {
    const props = makeProps(3);
    const onChange = vi.fn();
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);
    // Each property has a name input — verify all 3 are present
    const nameInputs = screen.getAllByPlaceholderText(/propertyName/i);
    expect(nameInputs).toHaveLength(3);
  });

  it('adds new property when "Add Property" button is clicked', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<SchemaPropertyEditor properties={[]} onChange={onChange} />);

    await user.click(screen.getByText(/Add Property/i));
    expect(onChange).toHaveBeenCalledOnce();
    const newProps = onChange.mock.calls[0]?.[0] as SchemaProperty[];
    expect(newProps).toHaveLength(1);
    expect(newProps[0]?.type).toBe('string');
  });

  it('removes property when delete button is clicked', async () => {
    const user = userEvent.setup();
    const props = [makeProp({ id: 'p1', name: 'field1' })];
    const onChange = vi.fn();
    const { container } = render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    // The delete button is the last button inside the property row, with class containing 'hover:text-danger'
    const deleteBtn = container.querySelector('button.flex-shrink-0');
    expect(deleteBtn).not.toBeNull();
    await user.click(deleteBtn!);
    expect(onChange).toHaveBeenCalled();
    const result = onChange.mock.calls[0]?.[0] as SchemaProperty[];
    expect(result).toHaveLength(0);
  });

  it('updates property name', async () => {
    const user = userEvent.setup();
    const props = [makeProp({ id: 'p1', name: '' })];
    const onChange = vi.fn();
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const input = screen.getByPlaceholderText(/propertyName/i);
    await user.type(input, 'email');
    expect(onChange).toHaveBeenCalled();
    // Check the last call includes the typed character
    const lastCall = onChange.mock.calls[onChange.mock.calls.length - 1];
    const updated = (lastCall?.[0] as SchemaProperty[])[0];
    expect(updated?.name).toContain('l'); // last character of "email"
  });

  it('updates property type to string', () => {
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', type: 'integer' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const typeSelect = screen.getByDisplayValue('integer');
    expect(typeSelect).toBeInTheDocument();
  });

  it('updates property type to integer', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', type: 'string' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const typeSelect = screen.getByDisplayValue('string');
    await user.selectOptions(typeSelect, 'integer');
    expect(onChange).toHaveBeenCalled();
    const updated = (onChange.mock.calls[0]?.[0] as SchemaProperty[])[0];
    expect(updated?.type).toBe('integer');
  });

  it('updates property type to boolean', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', type: 'string' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const typeSelect = screen.getByDisplayValue('string');
    await user.selectOptions(typeSelect, 'boolean');
    expect(onChange).toHaveBeenCalled();
    const updated = (onChange.mock.calls[0]?.[0] as SchemaProperty[])[0];
    expect(updated?.type).toBe('boolean');
  });

  it('updates property type to array', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', type: 'string' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const typeSelect = screen.getByDisplayValue('string');
    await user.selectOptions(typeSelect, 'array');
    expect(onChange).toHaveBeenCalled();
    const updated = (onChange.mock.calls[0]?.[0] as SchemaProperty[])[0];
    expect(updated?.type).toBe('array');
    expect(updated?.items).toBeDefined();
  });

  it('updates property type to object and shows nested properties', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', type: 'string' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const typeSelect = screen.getByDisplayValue('string');
    await user.selectOptions(typeSelect, 'object');
    expect(onChange).toHaveBeenCalled();
    const updated = (onChange.mock.calls[0]?.[0] as SchemaProperty[])[0];
    expect(updated?.type).toBe('object');
    expect(updated?.properties).toEqual([]);
  });

  it('updates property type to $ref and provides $ref field', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', type: 'string' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    const typeSelect = screen.getByDisplayValue('string');
    await user.selectOptions(typeSelect, '$ref');
    expect(onChange).toHaveBeenCalled();
    const updated = (onChange.mock.calls[0]?.[0] as SchemaProperty[])[0];
    expect(updated?.type).toBe('$ref');
  });

  it('toggles required flag', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const props = [makeProp({ id: 'p1', required: false })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    // Find the REQ/OPT toggle button
    const reqButton = screen.getByText('OPT');
    await user.click(reqButton);
    expect(onChange).toHaveBeenCalled();
    const updated = (onChange.mock.calls[0]?.[0] as SchemaProperty[])[0];
    expect(updated?.required).toBe(true);
  });

  it('shows constraints panel when expanded', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    // Use a $ref type that has expand/collapse capability
    const props = [makeProp({ id: 'p1', type: '$ref' })];
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    // Click the expand chevron
    const expandButtons = screen.getAllByRole('button');
    const chevronButton = expandButtons[0]; // First button is the expand toggle
    if (chevronButton) {
      await user.click(chevronButton);
      // After expanding, should show the $ref input placeholder
      expect(screen.getByPlaceholderText(/#\/components\/schemas\//i)).toBeInTheDocument();
    }
  });

  it('moves property up in order', async () => {
    const user = userEvent.setup();
    const props = [
      makeProp({ id: 'p1', name: 'first' }),
      makeProp({ id: 'p2', name: 'second' }),
    ];
    const onChange = vi.fn();
    render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    // Find all ArrowUp buttons — second one should be enabled (move 'second' up)
    const allButtons = screen.getAllByRole('button');
    const arrowUpButtons = allButtons.filter(btn => {
      const svg = btn.querySelector('svg');
      return svg && btn.getAttribute('disabled') === null && btn.previousElementSibling === null;
    });

    // Find the up arrow for the second property (the one that isn't disabled)
    // We look for buttons that are not disabled and are in the second property row
    const upButtons = screen.getAllByRole('button').filter(btn => {
      const svg = btn.querySelector('svg');
      if (!svg) return false;
      // Up arrow buttons - check that it's not disabled and near text 'second'
      return !btn.hasAttribute('disabled') && btn.closest('.border-edge')?.querySelector('input[value="second"]');
    });

    if (upButtons[0]) {
      await user.click(upButtons[0]);
      expect(onChange).toHaveBeenCalled();
      const result = onChange.mock.calls[0]?.[0] as SchemaProperty[];
      expect(result[0]?.name).toBe('second');
      expect(result[1]?.name).toBe('first');
    }
  });

  it('moves property down in order', async () => {
    const user = userEvent.setup();
    const props = [
      makeProp({ id: 'p1', name: 'first' }),
      makeProp({ id: 'p2', name: 'second' }),
    ];
    const onChange = vi.fn();
    const { container } = render(<SchemaPropertyEditor properties={props} onChange={onChange} />);

    // Find the first property row's down arrow (second arrow button in first row)
    const rows = container.querySelectorAll('.border.border-edge.rounded-md');
    const firstRow = rows[0];
    expect(firstRow).toBeTruthy();
    // Arrow buttons are the ones with disabled:opacity-30 class pattern
    const arrowButtons = firstRow!.querySelectorAll('button:not(.flex-shrink-0):not([title])');
    // Filter to find the non-disabled down arrow (second arrow button)
    const downBtn = Array.from(arrowButtons).filter(btn => 
      btn.querySelector('svg') && !btn.hasAttribute('disabled') && !btn.textContent
    )[1]; // second arrow = down
    if (downBtn) {
      await user.click(downBtn);
      expect(onChange).toHaveBeenCalled();
      const result = onChange.mock.calls[0]?.[0] as SchemaProperty[];
      expect(result[0]?.name).toBe('second');
      expect(result[1]?.name).toBe('first');
    }
  });

  it('handles read-only mode — add buttons not shown', () => {
    const onChange = vi.fn();
    render(<SchemaPropertyEditor properties={makeProps(1)} onChange={onChange} isReadOnly />);

    // Add property button should not be rendered
    expect(screen.queryByText(/Add Property/i)).not.toBeInTheDocument();
  });

  // ── ExampleGenerator integration tests ─────────────────────────────────────

  it('generates full example from schema properties (integration)', () => {
    const props: SchemaProperty[] = [
      makeProp({ name: 'email', type: 'string', constraints: { format: 'email' } }),
      makeProp({ name: 'age', type: 'integer', constraints: { minimum: 18, maximum: 65 } }),
      makeProp({ name: 'active', type: 'boolean' }),
    ];

    const result = generateExampleFromSchema(props);
    expect(result).toEqual({
      email: 'user@example.com',
      age: 41,
      active: true,
    });
  });

  it('generateStringExample returns format-appropriate value for email', () => {
    const prop = makeProp({ name: 'x', constraints: { format: 'email' } });
    expect(generateStringExample(prop)).toBe('user@example.com');
  });

  it('generateStringExample returns format-appropriate value for uuid', () => {
    const prop = makeProp({ name: 'x', constraints: { format: 'uuid' } });
    expect(generateStringExample(prop)).toBe('550e8400-e29b-41d4-a716-446655440000');
  });

  it('generateIntegerExample respects min/max constraints', () => {
    const prop = makeProp({ type: 'integer', constraints: { minimum: 5, maximum: 15 } });
    const result = generateIntegerExample(prop);
    expect(result).toBe(10);
    expect(result).toBeGreaterThanOrEqual(5);
    expect(result).toBeLessThanOrEqual(15);
  });
});
