import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Clock, RefreshCw, Edit2, Save, X, Zap } from 'lucide-react';
import {
  platformAdminApi,
  type NonProdScheduleEntry,
  type NonProdScheduleUpdate,
} from '../api/platformAdmin';

const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const DAY_LABELS = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

export function NonProdSchedulerPage() {
  const { t } = useTranslation('nonProdScheduler');
  const queryClient = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<NonProdScheduleUpdate>({
    enabled: true,
    activeDaysOfWeek: [1, 2, 3, 4, 5],
    activeFromHour: 8,
    activeToHour: 20,
    timezone: 'UTC',
  });
  const [overrideEnvId, setOverrideEnvId] = useState<string | null>(null);
  const [overrideReason, setOverrideReason] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['nonprod-schedules'],
    queryFn: platformAdminApi.getNonProdSchedules,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, update }: { id: string; update: NonProdScheduleUpdate }) =>
      platformAdminApi.updateNonProdSchedule(id, update),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['nonprod-schedules'] });
      setEditingId(null);
    },
  });

  const overrideMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      platformAdminApi.overrideNonProdSchedule(id, {
        keepActiveUntil: new Date(Date.now() + 8 * 3600_000).toISOString(),
        reason,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['nonprod-schedules'] });
      setOverrideEnvId(null);
      setOverrideReason('');
    },
  });

  function startEdit(schedule: NonProdScheduleEntry) {
    setEditingId(schedule.environmentId);
    setEditForm({
      enabled: schedule.enabled,
      activeDaysOfWeek: [...schedule.activeDaysOfWeek],
      activeFromHour: schedule.activeFromHour,
      activeToHour: schedule.activeToHour,
      timezone: schedule.timezone,
    });
  }

  function toggleDay(day: number) {
    setEditForm((f) => ({
      ...f,
      activeDaysOfWeek: f.activeDaysOfWeek.includes(day)
        ? f.activeDaysOfWeek.filter((d) => d !== day)
        : [...f.activeDaysOfWeek, day].sort(),
    }));
  }

  function statusBadgeClass(status: NonProdScheduleEntry['status']) {
    if (status === 'Active') return 'bg-green-100 text-green-800';
    if (status === 'OverriddenUntil') return 'bg-yellow-100 text-yellow-800';
    return 'bg-gray-100 text-gray-600';
  }

  if (isLoading) return <div className="p-6 text-sm text-gray-500">{t('loading')}</div>;
  if (isError) return <div className="p-6 text-sm text-red-500">{t('error')}</div>;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Clock size={24} className="text-blue-600" />
          <div>
            <h1 className="text-xl font-semibold text-gray-900">{t('title')}</h1>
            <p className="text-sm text-gray-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-3 py-1.5 text-sm border rounded-md hover:bg-gray-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Summary */}
      <div className="bg-green-50 border border-green-200 rounded-lg p-4 flex items-center justify-between">
        <div>
          <div className="text-sm font-medium text-green-800">{t('savingsSummaryLabel')}</div>
          <div className="text-xs text-green-600">{t('savingsSummaryDesc')}</div>
        </div>
        <div className="text-2xl font-bold text-green-700">
          {data?.totalEstimatedSavingPercent ?? 0}%
        </div>
      </div>

      {/* Schedule cards */}
      <div className="space-y-4">
        {data?.schedules.map((schedule) => (
          <div key={schedule.environmentId} className="bg-white border rounded-lg overflow-hidden">
            <div className="px-4 py-3 border-b bg-gray-50 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span className="text-sm font-medium text-gray-900">{schedule.environmentName}</span>
                <span className={`px-2 py-0.5 rounded text-xs font-medium ${statusBadgeClass(schedule.status)}`}>
                  {t(`status.${schedule.status}`)}
                </span>
                <span className="text-xs text-gray-400">
                  {t('savingLabel')}: {schedule.estimatedSavingPercent}%
                </span>
              </div>
              <div className="flex gap-2">
                {editingId !== schedule.environmentId && (
                  <>
                    <button
                      onClick={() => setOverrideEnvId(schedule.environmentId)}
                      className="flex items-center gap-1 px-2 py-1 text-xs border rounded hover:bg-yellow-50 text-yellow-700 border-yellow-300"
                    >
                      <Zap size={12} />
                      {t('override')}
                    </button>
                    <button
                      onClick={() => startEdit(schedule)}
                      className="flex items-center gap-1 px-2 py-1 text-xs border rounded hover:bg-gray-100"
                    >
                      <Edit2 size={12} />
                      {t('edit')}
                    </button>
                  </>
                )}
              </div>
            </div>

            {/* Override form */}
            {overrideEnvId === schedule.environmentId && (
              <div className="p-4 bg-yellow-50 border-b space-y-3">
                <div className="text-sm font-medium text-yellow-800">{t('overrideTitle')}</div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">{t('overrideReasonLabel')}</label>
                  <input
                    type="text"
                    value={overrideReason}
                    onChange={(e) => setOverrideReason(e.target.value)}
                    placeholder={t('overrideReasonPlaceholder')}
                    className="w-full text-sm border rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-yellow-400"
                  />
                </div>
                <div className="flex gap-2 justify-end">
                  <button
                    onClick={() => setOverrideEnvId(null)}
                    className="px-3 py-1.5 text-xs border rounded hover:bg-gray-50"
                  >
                    {t('cancel')}
                  </button>
                  <button
                    onClick={() => overrideMutation.mutate({ id: schedule.environmentId, reason: overrideReason })}
                    disabled={!overrideReason.trim() || overrideMutation.isPending}
                    className="px-3 py-1.5 text-xs bg-yellow-500 text-white rounded hover:bg-yellow-600 disabled:opacity-50"
                  >
                    {overrideMutation.isPending ? t('saving') : t('overrideConfirm')}
                  </button>
                </div>
              </div>
            )}

            {editingId === schedule.environmentId ? (
              <div className="p-4 space-y-4">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id={`enabled-${schedule.environmentId}`}
                    checked={editForm.enabled}
                    onChange={(e) => setEditForm((f) => ({ ...f, enabled: e.target.checked }))}
                  />
                  <label htmlFor={`enabled-${schedule.environmentId}`} className="text-sm text-gray-700">
                    {t('enabledLabel')}
                  </label>
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">{t('activeDaysLabel')}</label>
                  <div className="flex gap-2">
                    {DAYS.map((day, idx) => (
                      <button
                        key={day}
                        type="button"
                        onClick={() => toggleDay(idx)}
                        className={`px-2 py-1 rounded text-xs font-medium border ${
                          editForm.activeDaysOfWeek.includes(idx)
                            ? 'bg-blue-600 text-white border-blue-600'
                            : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
                        }`}
                      >
                        {t(`day.${DAY_LABELS[idx]}`)}
                      </button>
                    ))}
                  </div>
                </div>
                <div className="flex gap-4">
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">{t('fromHourLabel')}</label>
                    <input
                      type="number"
                      min={0}
                      max={23}
                      value={editForm.activeFromHour}
                      onChange={(e) => setEditForm((f) => ({ ...f, activeFromHour: Number(e.target.value) }))}
                      className="w-20 text-sm border rounded px-2 py-1"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-gray-700 mb-1">{t('toHourLabel')}</label>
                    <input
                      type="number"
                      min={0}
                      max={23}
                      value={editForm.activeToHour}
                      onChange={(e) => setEditForm((f) => ({ ...f, activeToHour: Number(e.target.value) }))}
                      className="w-20 text-sm border rounded px-2 py-1"
                    />
                  </div>
                </div>
                <div className="flex gap-2 justify-end">
                  <button
                    onClick={() => setEditingId(null)}
                    className="flex items-center gap-1 px-3 py-1.5 text-sm border rounded hover:bg-gray-50"
                  >
                    <X size={14} />
                    {t('cancel')}
                  </button>
                  <button
                    onClick={() => updateMutation.mutate({ id: schedule.environmentId, update: editForm })}
                    disabled={updateMutation.isPending}
                    className="flex items-center gap-1 px-3 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                  >
                    <Save size={14} />
                    {updateMutation.isPending ? t('saving') : t('save')}
                  </button>
                </div>
              </div>
            ) : (
              <div className="p-4 text-sm text-gray-600">
                <span className="font-medium text-gray-700">{t('scheduleLabel')}: </span>
                {schedule.activeDaysOfWeek.map((d) => DAYS[d]).join(', ')}{' '}
                {schedule.activeFromHour}:00 – {schedule.activeToHour}:00 ({schedule.timezone})
                {schedule.overrideUntil && (
                  <div className="mt-1 text-xs text-yellow-700">
                    {t('overrideActiveUntil', { date: new Date(schedule.overrideUntil).toLocaleString() })}
                    {schedule.overrideReason ? ` — ${schedule.overrideReason}` : ''}
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>

      {data?.simulatedNote && (
        <p className="text-xs text-gray-400 italic">{data.simulatedNote}</p>
      )}
    </div>
  );
}
