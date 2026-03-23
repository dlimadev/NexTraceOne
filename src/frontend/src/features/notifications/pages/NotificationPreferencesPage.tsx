import { useMemo, useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Lock, CheckCircle, AlertTriangle } from 'lucide-react';
import { PageContainer } from '../../../components/shell/PageContainer';
import { PageHeader } from '../../../components/PageHeader';
import { ErrorState } from '../../../components/ErrorState';
import { Button } from '../../../components/Button';
import { Toggle } from '../../../components/Toggle';
import { Tooltip } from '../../../components/Tooltip';
import { InlineMessage } from '../../../components/InlineMessage';
import {
  useNotificationPreferences,
  useUpdatePreference,
} from '../hooks/useNotificationPreferences';
import { getCategoryKey } from '../hooks/useNotificationHelpers';
import type { NotificationPreferenceDto } from '../types';

const CHANNELS = ['InApp', 'Email', 'Teams'] as const;
type Channel = (typeof CHANNELS)[number];

function channelI18nKey(channel: Channel): string {
  const map: Record<Channel, string> = {
    InApp: 'notifications.preferences.inApp',
    Email: 'notifications.preferences.email',
    Teams: 'notifications.preferences.teams',
  };
  return map[channel];
}

interface PreferenceRow {
  category: string;
  channels: Record<Channel, { enabled: boolean; isMandatory: boolean }>;
}

function buildRows(preferences: NotificationPreferenceDto[]): PreferenceRow[] {
  const map = new Map<string, PreferenceRow>();
  const order: string[] = [];

  for (const pref of preferences) {
    if (!map.has(pref.category)) {
      order.push(pref.category);
      map.set(pref.category, {
        category: pref.category,
        channels: {
          InApp: { enabled: false, isMandatory: false },
          Email: { enabled: false, isMandatory: false },
          Teams: { enabled: false, isMandatory: false },
        },
      });
    }
    const row = map.get(pref.category)!;
    if (pref.channel in row.channels) {
      row.channels[pref.channel as Channel] = {
        enabled: pref.enabled,
        isMandatory: pref.isMandatory,
      };
    }
  }

  return order.map((c) => map.get(c)!);
}

type FeedbackState = { type: 'success' | 'error'; key: string } | null;

export function NotificationPreferencesPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const { data, isLoading, isError, refetch } = useNotificationPreferences();
  const updatePreference = useUpdatePreference();

  const [feedback, setFeedback] = useState<FeedbackState>(null);

  const rows = useMemo(
    () => buildRows(data?.preferences ?? []),
    [data?.preferences],
  );

  const handleToggle = useCallback(
    (category: string, channel: Channel, enabled: boolean) => {
      setFeedback(null);
      updatePreference.mutate(
        { category, channel, enabled },
        {
          onSuccess: () => {
            setFeedback({ type: 'success', key: `${category}-${channel}` });
          },
          onError: () => {
            setFeedback({ type: 'error', key: `${category}-${channel}` });
          },
        },
      );
    },
    [updatePreference],
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('notifications.preferences.title')}
        subtitle={t('notifications.preferences.subtitle')}
        actions={
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate('/notifications')}
          >
            <ArrowLeft className="h-4 w-4 mr-1.5" />
            {t('notifications.title')}
          </Button>
        }
      />

      {/* Description */}
      <p className="mt-4 text-sm text-muted max-w-2xl">
        {t('notifications.preferences.description')}
      </p>

      {/* Feedback */}
      {feedback?.type === 'success' && (
        <div className="mt-4">
          <InlineMessage severity="success" icon={<CheckCircle className="h-4 w-4" />}>
            {t('notifications.preferences.saved')}
          </InlineMessage>
        </div>
      )}
      {feedback?.type === 'error' && (
        <div className="mt-4">
          <InlineMessage severity="danger" icon={<AlertTriangle className="h-4 w-4" />}>
            {t('notifications.preferences.saveFailed')}
          </InlineMessage>
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="mt-10 flex items-center justify-center py-16">
          <div className="flex items-center gap-3 text-muted text-sm">
            <div className="h-5 w-5 animate-spin rounded-full border-2 border-edge border-t-cyan" />
            {t('notifications.preferences.loading')}
          </div>
        </div>
      )}

      {/* Error */}
      {isError && (
        <div className="mt-6">
          <ErrorState
            title={t('notifications.preferences.errorLoading')}
            action={
              <Button variant="secondary" size="sm" onClick={() => refetch()}>
                {t('common.retry', 'Retry')}
              </Button>
            }
          />
        </div>
      )}

      {/* Preferences matrix */}
      {!isLoading && !isError && rows.length > 0 && (
        <div className="mt-6 overflow-x-auto">
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-edge">
                <th className="py-3 pr-4 text-left text-xs font-medium uppercase tracking-wider text-muted">
                  {t('notifications.preferences.category')}
                </th>
                {CHANNELS.map((ch) => (
                  <th
                    key={ch}
                    className="px-4 py-3 text-center text-xs font-medium uppercase tracking-wider text-muted"
                  >
                    {t(channelI18nKey(ch))}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr
                  key={row.category}
                  className="border-b border-edge/50 hover:bg-hover/30 transition-colors"
                >
                  <td className="py-4 pr-4">
                    <span className="text-sm font-medium text-heading">
                      {t(getCategoryKey(row.category))}
                    </span>
                  </td>
                  {CHANNELS.map((ch) => {
                    const cell = row.channels[ch];
                    return (
                      <td key={ch} className="px-4 py-4">
                        <div className="flex items-center justify-center gap-2">
                          {cell.isMandatory ? (
                            <Tooltip
                              content={t('notifications.preferences.mandatoryTooltip')}
                              position="top"
                            >
                              <div className="flex items-center gap-2">
                                <Toggle
                                  checked={cell.enabled}
                                  onChange={() => {}}
                                  disabled
                                  size="sm"
                                />
                                <Lock className="h-3.5 w-3.5 text-muted" />
                              </div>
                            </Tooltip>
                          ) : (
                            <Toggle
                              checked={cell.enabled}
                              onChange={(val) =>
                                handleToggle(row.category, ch, val)
                              }
                              disabled={updatePreference.isPending}
                              size="sm"
                            />
                          )}
                        </div>
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </PageContainer>
  );
}
