import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DataTable, type DataTableColumn } from '../../components/DataTable';

interface TestRow {
  id: string;
  name: string;
  status: string;
}

const columns: DataTableColumn<TestRow>[] = [
  { id: 'name', header: 'Name', accessor: (r) => r.name, sortValue: (r) => r.name },
  { id: 'status', header: 'Status', accessor: (r) => r.status },
];

const data: TestRow[] = [
  { id: '1', name: 'Service A', status: 'Active' },
  { id: '2', name: 'Service B', status: 'Inactive' },
  { id: '3', name: 'Service C', status: 'Active' },
];

describe('DataTable', () => {
  it('renders all rows', () => {
    render(<DataTable columns={columns} data={data} rowKey={(r) => r.id} />);
    expect(screen.getByText('Service A')).toBeInTheDocument();
    expect(screen.getByText('Service B')).toBeInTheDocument();
    expect(screen.getByText('Service C')).toBeInTheDocument();
  });

  it('renders column headers', () => {
    render(<DataTable columns={columns} data={data} rowKey={(r) => r.id} />);
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
  });

  it('renders empty state when no data', () => {
    render(
      <DataTable
        columns={columns}
        data={[]}
        rowKey={(r) => r.id}
        emptyTitle="No services"
      />,
    );
    expect(screen.getByText('No services')).toBeInTheDocument();
  });

  it('renders skeleton rows when loading', () => {
    const { container } = render(
      <DataTable columns={columns} data={[]} rowKey={(r) => r.id} loading skeletonRows={3} />,
    );
    const skeletonRows = container.querySelectorAll('.skeleton');
    expect(skeletonRows.length).toBeGreaterThan(0);
  });

  it('calls onRowClick when row is clicked', async () => {
    const onRowClick = vi.fn();
    render(
      <DataTable columns={columns} data={data} rowKey={(r) => r.id} onRowClick={onRowClick} />,
    );
    await userEvent.click(screen.getByText('Service A'));
    expect(onRowClick).toHaveBeenCalledWith(data[0]);
  });

  it('sorts data when column header is clicked', async () => {
    const onSortChange = vi.fn();
    render(
      <DataTable
        columns={columns}
        data={data}
        rowKey={(r) => r.id}
        sort={null}
        onSortChange={onSortChange}
      />,
    );
    await userEvent.click(screen.getByText('Name'));
    expect(onSortChange).toHaveBeenCalledWith({ columnId: 'name', direction: 'asc' });
  });

  it('renders checkboxes when selectable', () => {
    render(
      <DataTable
        columns={columns}
        data={data}
        rowKey={(r) => r.id}
        selectable
        selectedKeys={new Set()}
        onSelectionChange={() => {}}
      />,
    );
    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes.length).toBe(4); // 1 header + 3 rows
  });

  it('has grid role', () => {
    render(<DataTable columns={columns} data={data} rowKey={(r) => r.id} />);
    expect(screen.getByRole('grid')).toBeInTheDocument();
  });
});
