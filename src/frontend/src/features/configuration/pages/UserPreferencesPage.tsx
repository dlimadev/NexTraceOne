import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Settings, Sidebar, LayoutDashboard, Save, CheckCircle, XCircle,
  Clock, Globe, List,
} from 'lucide-react';

const TIMEZONES = [
  'UTC',
  'America/Sao_Paulo',
  'America/New_York',
  'America/Chicago',
  'America/Denver',
  'America/Los_Angeles',
  'Europe/Lisbon',
  'Europe/London',
  'Europe/Paris',
  'Europe/Berlin',
  'Asia/Tokyo',
  'Asia/Shanghai',
  'Australia/Sydney',
];
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';

/**
 * UserPreferencesPage — personalização de plataforma por utilizador.
 * Permite configurar sidebar (pinned items), home dashboard (widgets), etc.
 * Respeita limites da plataforma (maxPinnedItems, maxWidgets) definidos por parametrização.
 * Pilar: Platform Customization + Source of Truth
 */

interface PreferenceItem {
  key: string;
  value: string;
  updatedAt?: string;
}

interface PreferencesResponse {
  userId: string;
  preferences: PreferenceItem[];
  sidebarCustomizationEnabled: boolean;
  maxPinnedItems: number;
  maxWidgets: number;
  evaluatedAt: string;
}

const SIDEBAR_MODULES = [
  'catalog', 'contracts', 'changes', 'operations', 'knowledge',
  'governance', 'ai-hub', 'integrations', 'audit',
];

const AVAILABLE_WIDGETS = [
  'serviceHealth', 'recentChanges', 'activeIncidents', 'doraMetrics',
  'contractCoverage', 'errorBudget', 'teamOverview', 'complianceStatus',
  'knowledgeActivity', 'aiUsage', 'securityFindings', 'releaseCalendar',
];

async function fetchPreferences(): Promise<PreferencesResponse> {
  const resp = await fetch('/api/v1/user-preferences');
  if (!resp.ok) throw new Error('Failed to fetch preferences');
  return resp.json();
}

async function savePreference(key: string, value: string): Promise<void> {
  const resp = await fetch('/api/v1/user-preferences', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ key, value }),
  });
  if (!resp.ok) throw new Error('Failed to save preference');
}

export function UserPreferencesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['user-preferences'],
    queryFn: fetchPreferences,
  });

  const [pinnedItems, setPinnedItems] = useState<string[]>([]);
  const [activeWidgets, setActiveWidgets] = useState<string[]>([]);
  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [timezone, setTimezone] = useState('UTC');
  const [dateFormat, setDateFormat] = useState('yyyy-MM-dd');
  const [timeFormat, setTimeFormat] = useState('HH:mm:ss');
  const [itemsPerPage, setItemsPerPage] = useState('25');
  const [defaultEnv, setDefaultEnv] = useState('');
  const [defaultTeam, setDefaultTeam] = useState('');
  const [defaultService, setDefaultService] = useState('');

  useEffect(() => {
    if (data?.preferences) {
      const sidebar = data.preferences.find(p => p.key === 'platform.sidebar.pinned_items');
      const widgets = data.preferences.find(p => p.key === 'platform.home.active_widgets');
      if (sidebar) {
        try { setPinnedItems(JSON.parse(sidebar.value)); } catch { /* keep default */ }
      }
      if (widgets) {
        try { setActiveWidgets(JSON.parse(widgets.value)); } catch { /* keep default */ }
      }
      const tz = data.preferences.find(p => p.key === 'user.timezone');
      const df = data.preferences.find(p => p.key === 'user.date_format');
      const tf = data.preferences.find(p => p.key === 'user.time_format');
      const ipp = data.preferences.find(p => p.key === 'user.items_per_page');
      const de = data.preferences.find(p => p.key === 'default.environment');
      const dtPref = data.preferences.find(p => p.key === 'default.team');
      const ds = data.preferences.find(p => p.key === 'default.service');
      if (tz) setTimezone(tz.value);
      if (df) setDateFormat(df.value);
      if (tf) setTimeFormat(tf.value);
      if (ipp) setItemsPerPage(ipp.value);
      if (de) setDefaultEnv(de.value);
      if (dtPref) setDefaultTeam(dtPref.value);
      if (ds) setDefaultService(ds.value);
    }
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      await savePreference('platform.sidebar.pinned_items', JSON.stringify(pinnedItems));
      await savePreference('platform.home.active_widgets', JSON.stringify(activeWidgets));
      await savePreference('user.timezone', timezone);
      await savePreference('user.date_format', dateFormat);
      await savePreference('user.time_format', timeFormat);
      await savePreference('user.items_per_page', itemsPerPage);
      if (defaultEnv) await savePreference('default.environment', defaultEnv);
      if (defaultTeam) await savePreference('default.team', defaultTeam);
      if (defaultService) await savePreference('default.service', defaultService);
    },
    onSuccess: () => {
      setSaveStatus('saved');
      queryClient.invalidateQueries({ queryKey: ['user-preferences'] });
      setTimeout(() => setSaveStatus('idle'), 3000);
    },
    onError: () => setSaveStatus('error'),
    onMutate: () => setSaveStatus('saving'),
  });

  if (isLoading) return <PageLoadingState />;
  if (error) return <PageErrorState message={t('userPreferences.error')} />;

  const maxPinned = data?.maxPinnedItems ?? 10;
  const maxW = data?.maxWidgets ?? 12;
  const sidebarEnabled = data?.sidebarCustomizationEnabled ?? false;

  const togglePinned = (mod: string) => {
    if (pinnedItems.includes(mod)) {
      setPinnedItems(pinnedItems.filter(p => p !== mod));
    } else if (pinnedItems.length < maxPinned) {
      setPinnedItems([...pinnedItems, mod]);
    }
  };

  const toggleWidget = (wid: string) => {
    if (activeWidgets.includes(wid)) {
      setActiveWidgets(activeWidgets.filter(w => w !== wid));
    } else if (activeWidgets.length < maxW) {
      setActiveWidgets([...activeWidgets, wid]);
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('userPreferences.title')}
        subtitle={t('userPreferences.description')}
        icon={<Settings size={24} />}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
        {/* Sidebar Customization */}
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Sidebar size={18} />
            <span>{t('userPreferences.sidebar.title')}</span>
          </CardHeader>
          <CardBody>
            {!sidebarEnabled ? (
              <p className="text-sm text-muted">{t('userPreferences.sidebar.disabled', 'Sidebar customization is disabled by platform settings')}</p>
            ) : (
              <>
                <p className="text-sm text-muted mb-3">
                  {t('userPreferences.sidebar.pinnedItems')} ({pinnedItems.length}/{maxPinned})
                </p>
                <div className="space-y-2">
                  {SIDEBAR_MODULES.map(mod => (
                    <label key={mod} className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={pinnedItems.includes(mod)}
                        onChange={() => togglePinned(mod)}
                        disabled={!pinnedItems.includes(mod) && pinnedItems.length >= maxPinned}
                        className="rounded"
                      />
                      <span className="text-sm capitalize">{mod.replace('-', ' ')}</span>
                    </label>
                  ))}
                </div>
              </>
            )}
          </CardBody>
        </Card>

        {/* Home Dashboard Widgets */}
        <Card>
          <CardHeader className="flex items-center gap-2">
            <LayoutDashboard size={18} />
            <span>{t('userPreferences.home.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-3">
              {t('userPreferences.home.widgets')} ({activeWidgets.length}/{maxW})
            </p>
            <div className="space-y-2">
              {AVAILABLE_WIDGETS.map(wid => (
                <label key={wid} className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={activeWidgets.includes(wid)}
                    onChange={() => toggleWidget(wid)}
                    disabled={!activeWidgets.includes(wid) && activeWidgets.length >= maxW}
                    className="rounded"
                  />
                  <span className="text-sm">{wid.replace(/([A-Z])/g, ' $1').trim()}</span>
                </label>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Timezone & Date Format */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Clock size={18} />
            <span>{t('userPreferences.timezone.title')}</span>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">{t('userPreferences.timezone.timezone')}</label>
                <select value={timezone} onChange={e => setTimezone(e.target.value)} className="w-full px-3 py-1.5 text-sm border rounded">
                  {TIMEZONES.map(tz => <option key={tz} value={tz}>{tz}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('userPreferences.timezone.dateFormat')}</label>
                <select value={dateFormat} onChange={e => setDateFormat(e.target.value)} className="w-full px-3 py-1.5 text-sm border rounded">
                  <option value="yyyy-MM-dd">yyyy-MM-dd (ISO)</option>
                  <option value="MM/dd/yyyy">MM/dd/yyyy (US)</option>
                  <option value="dd/MM/yyyy">dd/MM/yyyy (EU)</option>
                  <option value="dd.MM.yyyy">dd.MM.yyyy</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('userPreferences.timezone.timeFormat')}</label>
                <select value={timeFormat} onChange={e => setTimeFormat(e.target.value)} className="w-full px-3 py-1.5 text-sm border rounded">
                  <option value="HH:mm:ss">HH:mm:ss (24h)</option>
                  <option value="hh:mm:ss a">hh:mm:ss a (12h)</option>
                  <option value="HH:mm">HH:mm (Short)</option>
                </select>
              </div>
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Default Scope */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Globe size={18} />
            <span>{t('userPreferences.defaultScope.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">{t('userPreferences.defaultScope.description')}</p>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">{t('userPreferences.defaultScope.environment')}</label>
                <input type="text" value={defaultEnv} onChange={e => setDefaultEnv(e.target.value)} className="w-full px-3 py-1.5 text-sm border rounded" placeholder="e.g. production" />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('userPreferences.defaultScope.team')}</label>
                <input type="text" value={defaultTeam} onChange={e => setDefaultTeam(e.target.value)} className="w-full px-3 py-1.5 text-sm border rounded" placeholder="e.g. platform" />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('userPreferences.defaultScope.service')}</label>
                <input type="text" value={defaultService} onChange={e => setDefaultService(e.target.value)} className="w-full px-3 py-1.5 text-sm border rounded" placeholder="e.g. api-gateway" />
              </div>
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Items Per Page */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <List size={18} />
            <span>{t('userPreferences.itemsPerPage.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-3">{t('userPreferences.itemsPerPage.description')}</p>
            <select value={itemsPerPage} onChange={e => setItemsPerPage(e.target.value)} className="w-40 px-3 py-1.5 text-sm border rounded">
              <option value="10">10</option>
              <option value="25">25</option>
              <option value="50">50</option>
              <option value="100">100</option>
            </select>
          </CardBody>
        </Card>
      </div>

      {/* Save button */}
      <div className="mt-6 flex items-center gap-3">
        <button
          className="btn btn-primary flex items-center gap-2"
          onClick={() => saveMutation.mutate()}
          disabled={saveStatus === 'saving'}
        >
          <Save size={16} />
          {saveStatus === 'saving' ? t('common.saving', 'Saving...') : t('userPreferences.save')}
        </button>
        {saveStatus === 'saved' && (
          <span className="flex items-center gap-1 text-success text-sm">
            <CheckCircle size={14} /> {t('userPreferences.saved')}
          </span>
        )}
        {saveStatus === 'error' && (
          <span className="flex items-center gap-1 text-critical text-sm">
            <XCircle size={14} /> {t('userPreferences.error')}
          </span>
        )}
      </div>
    </PageContainer>
  );
}

export default UserPreferencesPage;
