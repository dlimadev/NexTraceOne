import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { GraphQlSchemaExplorer } from '../../features/contracts/studio/components/GraphQlSchemaExplorer';
import { GraphQlSchemaDiffViewer } from '../../features/contracts/studio/components/GraphQlSchemaDiffViewer';
import { ProtobufSchemaExplorer } from '../../features/contracts/studio/components/ProtobufSchemaExplorer';
import { ProtobufSchemaDiffViewer } from '../../features/contracts/studio/components/ProtobufSchemaDiffViewer';
import type { GraphQlTypeItem } from '../../features/contracts/studio/components/GraphQlSchemaExplorer';
import type { ProtobufElementItem } from '../../features/contracts/studio/components/ProtobufSchemaExplorer';

const gqlItems: GraphQlTypeItem[] = [
  { name: 'User', kind: 'OBJECT', fieldCount: 5 },
  { name: 'getUser', kind: 'QUERY', fieldCount: 2 },
  { name: 'createUser', kind: 'MUTATION', fieldCount: 3 },
  { name: 'UserStatus', kind: 'ENUM', fieldCount: 4, isDeprecated: true },
];

const protoItems: ProtobufElementItem[] = [
  { name: 'UserMessage', kind: 'MESSAGE', fieldCount: 4 },
  { name: 'UserService', kind: 'SERVICE', fieldCount: 2 },
  { name: 'GetUser', kind: 'RPC', parent: 'UserService' },
  { name: 'status', kind: 'FIELD', parent: 'UserMessage', isDeprecated: true },
];

// ── GraphQlSchemaExplorer ──────────────────────────────────────────────────────

describe('GraphQlSchemaExplorer', () => {
  it('renders all provided items', () => {
    render(<GraphQlSchemaExplorer items={gqlItems} />);
    expect(screen.getByTestId('graphql-schema-explorer')).toBeInTheDocument();
    expect(screen.getByText('User')).toBeInTheDocument();
    expect(screen.getByText('getUser')).toBeInTheDocument();
  });

  it('filters items by search query', () => {
    render(<GraphQlSchemaExplorer items={gqlItems} />);
    const input = screen.getByRole('textbox');
    fireEvent.change(input, { target: { value: 'create' } });
    expect(screen.getByText('createUser')).toBeInTheDocument();
    expect(screen.queryByText('User')).not.toBeInTheDocument();
  });

  it('shows deprecated badge for deprecated items', () => {
    render(<GraphQlSchemaExplorer items={gqlItems} />);
    const badges = screen.getAllByText('deprecated');
    expect(badges.length).toBeGreaterThan(0);
  });

  it('calls onSelect when item is clicked', () => {
    const onSelect = vi.fn();
    render(<GraphQlSchemaExplorer items={gqlItems} onSelect={onSelect} />);
    fireEvent.click(screen.getByText('User'));
    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ name: 'User' }));
  });
});

// ── GraphQlSchemaDiffViewer ────────────────────────────────────────────────────

describe('GraphQlSchemaDiffViewer', () => {
  const before = {
    label: 'v1.0',
    lines: [
      { content: 'type User { id: ID! }', kind: 'unchanged' as const },
      { content: 'type Post { id: ID! title: String! }', kind: 'breaking' as const },
    ],
  };
  const after = {
    label: 'v2.0',
    lines: [
      { content: 'type User { id: ID! email: String }', kind: 'non-breaking' as const },
    ],
  };

  it('renders both sides with labels', () => {
    render(<GraphQlSchemaDiffViewer before={before} after={after} breakingCount={1} nonBreakingCount={1} />);
    expect(screen.getByTestId('graphql-schema-diff-viewer')).toBeInTheDocument();
    expect(screen.getByText('v1.0')).toBeInTheDocument();
    expect(screen.getByText('v2.0')).toBeInTheDocument();
  });

  it('shows breaking and non-breaking counts in badges', () => {
    render(<GraphQlSchemaDiffViewer before={before} after={after} breakingCount={2} nonBreakingCount={3} />);
    expect(screen.getByText(/graphqlDiffViewer.breakingChanges/)).toBeInTheDocument();
    expect(screen.getByText(/graphqlDiffViewer.nonBreakingChanges/)).toBeInTheDocument();
  });

  it('shows no breaking changes message when counts are 0', () => {
    render(<GraphQlSchemaDiffViewer before={before} after={after} breakingCount={0} nonBreakingCount={0} />);
    expect(screen.getByText('graphqlDiffViewer.noBreakingChanges')).toBeInTheDocument();
  });
});

// ── ProtobufSchemaExplorer ────────────────────────────────────────────────────

describe('ProtobufSchemaExplorer', () => {
  it('renders all provided items', () => {
    render(<ProtobufSchemaExplorer items={protoItems} />);
    expect(screen.getByTestId('protobuf-schema-explorer')).toBeInTheDocument();
    // UserMessage appears in both the item button and as group key — use getAllByText
    expect(screen.getAllByText('UserMessage').length).toBeGreaterThan(0);
    expect(screen.getAllByText('UserService').length).toBeGreaterThan(0);
  });

  it('filters items by search', () => {
    render(<ProtobufSchemaExplorer items={protoItems} />);
    const input = screen.getByRole('textbox');
    fireEvent.change(input, { target: { value: 'Service' } });
    expect(screen.getAllByText('UserService').length).toBeGreaterThan(0);
    expect(screen.queryByText('UserMessage')).not.toBeInTheDocument();
  });

  it('shows deprecated badge for deprecated fields', () => {
    render(<ProtobufSchemaExplorer items={protoItems} />);
    expect(screen.getByText('protobufDiffViewer.deprecatedField')).toBeInTheDocument();
  });

  it('calls onSelect when an item is clicked', () => {
    const onSelect = vi.fn();
    render(<ProtobufSchemaExplorer items={protoItems} onSelect={onSelect} />);
    // Click the first UserMessage button (font-mono element)
    const buttons = screen.getAllByRole('button');
    const userMessageBtn = buttons.find((b) => b.textContent?.includes('UserMessage'));
    expect(userMessageBtn).toBeDefined();
    fireEvent.click(userMessageBtn!);
    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ name: 'UserMessage' }));
  });
});

// ── ProtobufSchemaDiffViewer ──────────────────────────────────────────────────

describe('ProtobufSchemaDiffViewer', () => {
  const before = {
    label: 'v1.proto',
    lines: [
      { content: 'message User { string name = 1; }', kind: 'unchanged' as const, changeType: 'message' as const },
      { content: 'string id = 2;', kind: 'breaking' as const, changeType: 'field' as const },
    ],
  };
  const after = {
    label: 'v2.proto',
    lines: [
      { content: 'message User { string name = 1; string email = 3; }', kind: 'non-breaking' as const, changeType: 'message' as const },
    ],
  };

  it('renders both sides with labels', () => {
    render(<ProtobufSchemaDiffViewer before={before} after={after} breakingCount={1} nonBreakingCount={1} />);
    expect(screen.getByTestId('protobuf-schema-diff-viewer')).toBeInTheDocument();
    expect(screen.getByText('v1.proto')).toBeInTheDocument();
    expect(screen.getByText('v2.proto')).toBeInTheDocument();
  });

  it('shows breaking and non-breaking badge labels', () => {
    render(<ProtobufSchemaDiffViewer before={before} after={after} breakingCount={1} nonBreakingCount={2} />);
    expect(screen.getByText(/protobufDiffViewer.breakingChanges/)).toBeInTheDocument();
    expect(screen.getByText(/protobufDiffViewer.nonBreakingChanges/)).toBeInTheDocument();
  });

  it('shows no breaking changes message when counts are 0', () => {
    render(<ProtobufSchemaDiffViewer before={before} after={after} breakingCount={0} nonBreakingCount={0} />);
    expect(screen.getByText('protobufDiffViewer.noBreakingChanges')).toBeInTheDocument();
  });

  it('renders change type labels for lines with changeType', () => {
    render(<ProtobufSchemaDiffViewer before={before} after={after} breakingCount={1} nonBreakingCount={1} />);
    expect(screen.getAllByText('[MSG]').length).toBeGreaterThan(0);
    expect(screen.getAllByText('[FLD]').length).toBeGreaterThan(0);
  });
});
