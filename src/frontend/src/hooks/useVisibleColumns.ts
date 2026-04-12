import { useQuery } from '@tanstack/react-query';

interface ColumnOption {
  key: string;
  label: string;
}

async function fetchPreferences() {
  const resp = await fetch('/api/v1/user-preferences');
  if (!resp.ok) throw new Error('Failed to fetch');
  return resp.json();
}

export function useVisibleColumns(context: string, availableColumns: ColumnOption[]): string[] {
  const prefKey = `table.columns.${context}`;
  const { data } = useQuery({ queryKey: ['user-preferences'], queryFn: fetchPreferences, staleTime: 60000 });
  const prefs: Array<{ key: string; value: string }> = data?.preferences ?? [];
  const stored = prefs.find(p => p.key === prefKey)?.value;
  if (!stored) return availableColumns.map(c => c.key);
  try {
    const parsed = JSON.parse(stored);
    return Array.isArray(parsed) ? parsed : availableColumns.map(c => c.key);
  } catch {
    return availableColumns.map(c => c.key);
  }
}
