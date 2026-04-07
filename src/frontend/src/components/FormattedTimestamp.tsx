import { useQuery } from '@tanstack/react-query';

interface FormattedTimestampProps {
  value: string | Date | null | undefined;
  format?: 'datetime' | 'date' | 'time' | 'relative';
}

interface PreferencesResponse {
  preferences: Array<{ key: string; value: string }>;
}

async function fetchPreferences(): Promise<PreferencesResponse> {
  const resp = await fetch('/api/v1/user-preferences');
  if (!resp.ok) throw new Error('Failed to fetch preferences');
  return resp.json();
}

function getPref(prefs: Array<{ key: string; value: string }>, key: string, fallback: string): string {
  return prefs.find(p => p.key === key)?.value ?? fallback;
}

function formatRelative(date: Date): string {
  const now = Date.now();
  const diff = now - date.getTime();
  const seconds = Math.floor(diff / 1000);
  if (seconds < 60) return `${seconds}s ago`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  const months = Math.floor(days / 30);
  if (months < 12) return `${months}mo ago`;
  return `${Math.floor(months / 12)}y ago`;
}

export function FormattedTimestamp({ value, format = 'datetime' }: FormattedTimestampProps) {
  const { data } = useQuery({
    queryKey: ['user-preferences'],
    queryFn: fetchPreferences,
    staleTime: 5 * 60 * 1000,
  });

  if (!value) return null;

  const date = value instanceof Date ? value : new Date(value);
  if (isNaN(date.getTime())) return null;

  const prefs = data?.preferences ?? [];
  const timezone = getPref(prefs, 'user.timezone', Intl.DateTimeFormat().resolvedOptions().timeZone);
  const dateFormatPref = getPref(prefs, 'user.date_format', 'yyyy-MM-dd');
  const timeFormatPref = getPref(prefs, 'user.time_format', 'HH:mm:ss');

  const use12h = timeFormatPref.includes('a');

  const dateOptions: Intl.DateTimeFormatOptions = { timeZone: timezone };
  if (dateFormatPref === 'MM/dd/yyyy') {
    dateOptions.month = '2-digit'; dateOptions.day = '2-digit'; dateOptions.year = 'numeric';
  } else if (dateFormatPref === 'dd/MM/yyyy' || dateFormatPref === 'dd.MM.yyyy') {
    dateOptions.day = '2-digit'; dateOptions.month = '2-digit'; dateOptions.year = 'numeric';
  } else {
    dateOptions.year = 'numeric'; dateOptions.month = '2-digit'; dateOptions.day = '2-digit';
  }

  const timeOptions: Intl.DateTimeFormatOptions = {
    timeZone: timezone,
    hour: '2-digit',
    minute: '2-digit',
    ...(timeFormatPref.includes('ss') ? { second: '2-digit' as const } : {}),
    hour12: use12h,
  };

  let formatted: string;
  if (format === 'relative') {
    formatted = formatRelative(date);
  } else if (format === 'date') {
    formatted = new Intl.DateTimeFormat(undefined, dateOptions).format(date);
  } else if (format === 'time') {
    formatted = new Intl.DateTimeFormat(undefined, timeOptions).format(date);
  } else {
    const d = new Intl.DateTimeFormat(undefined, dateOptions).format(date);
    const t = new Intl.DateTimeFormat(undefined, timeOptions).format(date);
    formatted = `${d} ${t}`;
  }

  return (
    <time dateTime={date.toISOString()} title={date.toISOString()}>
      {formatted}
    </time>
  );
}

export default FormattedTimestamp;
