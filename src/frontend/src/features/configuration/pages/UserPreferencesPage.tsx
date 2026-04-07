import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Settings, Sidebar, LayoutDashboard, Save, CheckCircle, XCircle,
  Clock, Globe, List, BellOff, Rss, Bot,
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
  const [quietHoursEnabled, setQuietHoursEnabled] = useState(false);
  const [quietHoursStart, setQuietHoursStart] = useState('22:00');
  const [quietHoursEnd, setQuietHoursEnd] = useState('08:00');
  const [quietHoursTimezone, setQuietHoursTimezone] = useState('UTC');
  const [digestFrequency, setDigestFrequency] = useState('daily');
  const [digestSections, setDigestSections] = useState<string[]>(['changes', 'incidents', 'contracts', 'compliance']);

  const DIGEST_SECTION_OPTIONS = ['changes', 'incidents', 'contracts', 'compliance', 'finops', 'ai-usage'];

  // ── AI Preferences state ──────────────────────────────────────────────────
  const [aiVerbosity, setAiVerbosity] = useState('standard');
  const [aiLanguage, setAiLanguage] = useState('en');
  const [aiContextScope, setAiContextScope] = useState('team');
  const [aiKnowledgeSources, setAiKnowledgeSources] = useState<string[]>(['contracts', 'services', 'changes', 'incidents', 'runbooks']);

  const AI_KNOWLEDGE_OPTIONS = ['contracts', 'services', 'changes', 'incidents', 'runbooks', 'knowledge-articles', 'operational-notes'];
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
      const qhe = data.preferences.find(p => p.key === 'notifications.quiet_hours.enabled');
      const qhs = data.preferences.find(p => p.key === 'notifications.quiet_hours.start');
      const qhend = data.preferences.find(p => p.key === 'notifications.quiet_hours.end');
      const qhtz = data.preferences.find(p => p.key === 'notifications.quiet_hours.timezone');
      const df2 = data.preferences.find(p => p.key === 'notifications.digest.frequency');
      const dsec = data.preferences.find(p => p.key === 'notifications.digest.sections');
      if (qhe) setQuietHoursEnabled(qhe.value === 'true');
      if (qhs) setQuietHoursStart(qhs.value);
      if (qhend) setQuietHoursEnd(qhend.value);
      if (qhtz) setQuietHoursTimezone(qhtz.value);
      if (df2) setDigestFrequency(df2.value);
      if (dsec) { try { setDigestSections(JSON.parse(dsec.value)); } catch { /* keep default */ } }

      const aiVerb = data.preferences.find(p => p.key === 'user.ai.response_verbosity');
      const aiLang = data.preferences.find(p => p.key === 'user.ai.preferred_language');
      const aiScope = data.preferences.find(p => p.key === 'user.ai.auto_context_scope');
      const aiKnow = data.preferences.find(p => p.key === 'user.ai.knowledge_sources');
      if (aiVerb) setAiVerbosity(aiVerb.value);
      if (aiLang) setAiLanguage(aiLang.value);
      if (aiScope) setAiContextScope(aiScope.value);
      if (aiKnow) { try { setAiKnowledgeSources(JSON.parse(aiKnow.value)); } catch { /* keep default */ } }
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
      await savePreference('notifications.quiet_hours.enabled', String(quietHoursEnabled));
      await savePreference('notifications.quiet_hours.start', quietHoursStart);
      await savePreference('notifications.quiet_hours.end', quietHoursEnd);
      await savePreference('notifications.quiet_hours.timezone', quietHoursTimezone);
      await savePreference('notifications.digest.frequency', digestFrequency);
      await savePreference('notifications.digest.sections', JSON.stringify(digestSections));
      await savePreference('user.ai.response_verbosity', aiVerbosity);
      await savePreference('user.ai.preferred_language', aiLanguage);
      await savePreference('user.ai.auto_context_scope', aiContextScope);
      await savePreference('user.ai.knowledge_sources', JSON.stringify(aiKnowledgeSources));
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

      {/* Quiet Hours */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <BellOff size={18} />
            <span>{t('quietHours.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">{t('quietHours.subtitle')}</p>
            <div className="space-y-4">
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={quietHoursEnabled}
                  onChange={e => setQuietHoursEnabled(e.target.checked)}
                  className="w-4 h-4 accent-blue-600"
                />
                <span className="text-sm">{t('quietHours.enabled')}</span>
              </label>
              {quietHoursEnabled && (
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pl-7">
                  <div>
                    <label className="block text-xs text-muted mb-1">{t('quietHours.start')}</label>
                    <input
                      type="time"
                      value={quietHoursStart}
                      onChange={e => setQuietHoursStart(e.target.value)}
                      className="w-full px-3 py-1.5 text-sm border rounded bg-transparent"
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-muted mb-1">{t('quietHours.end')}</label>
                    <input
                      type="time"
                      value={quietHoursEnd}
                      onChange={e => setQuietHoursEnd(e.target.value)}
                      className="w-full px-3 py-1.5 text-sm border rounded bg-transparent"
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-muted mb-1">{t('quietHours.timezone')}</label>
                    <select
                      value={quietHoursTimezone}
                      onChange={e => setQuietHoursTimezone(e.target.value)}
                      className="w-full px-3 py-1.5 text-sm border rounded bg-white dark:bg-gray-900"
                    >
                      {TIMEZONES.map(tz => <option key={tz} value={tz}>{tz}</option>)}
                    </select>
                  </div>
                </div>
              )}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Digest Settings */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Rss size={18} />
            <span>{t('digestSettings.title')}</span>
          </CardHeader>
          <CardBody>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">{t('digestSettings.frequency')}</label>
                <select
                  value={digestFrequency}
                  onChange={e => setDigestFrequency(e.target.value)}
                  className="w-48 px-3 py-1.5 text-sm border rounded bg-white dark:bg-gray-900"
                >
                  <option value="daily">{t('digestSettings.frequencyOptions.daily')}</option>
                  <option value="weekly">{t('digestSettings.frequencyOptions.weekly')}</option>
                  <option value="none">{t('digestSettings.frequencyOptions.none')}</option>
                </select>
              </div>
              {digestFrequency !== 'none' && (
                <div>
                  <label className="block text-sm font-medium mb-2">{t('digestSettings.sections')}</label>
                  <div className="flex flex-wrap gap-2">
                    {DIGEST_SECTION_OPTIONS.map(sec => (
                      <button
                        key={sec}
                        onClick={() =>
                          setDigestSections(prev =>
                            prev.includes(sec) ? prev.filter(s => s !== sec) : [...prev, sec]
                          )
                        }
                        className={`text-xs px-3 py-1.5 rounded-full border transition-colors ${
                          digestSections.includes(sec)
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 font-medium'
                            : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-blue-300'
                        }`}
                      >
                        {sec}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
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

      {/* AI Preferences */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Bot size={18} />
            <span>{t('aiPreferences.title')}</span>
          </CardHeader>
          <CardBody>
            <div className="space-y-5">
              {/* Response Verbosity */}
              <div>
                <label className="block text-sm font-medium mb-1">{t('aiPreferences.verbosity')}</label>
                <select
                  value={aiVerbosity}
                  onChange={e => setAiVerbosity(e.target.value)}
                  className="w-48 px-3 py-1.5 text-sm border rounded bg-white dark:bg-gray-900"
                >
                  <option value="concise">{t('aiPreferences.verbosityOptions.concise')}</option>
                  <option value="standard">{t('aiPreferences.verbosityOptions.standard')}</option>
                  <option value="detailed">{t('aiPreferences.verbosityOptions.detailed')}</option>
                </select>
              </div>

              {/* Preferred Language */}
              <div>
                <label className="block text-sm font-medium mb-1">{t('aiPreferences.language')}</label>
                <input
                  type="text"
                  value={aiLanguage}
                  onChange={e => setAiLanguage(e.target.value)}
                  className="w-32 px-3 py-1.5 text-sm border rounded bg-transparent"
                  placeholder="en"
                />
              </div>

              {/* Auto Context Scope */}
              <div>
                <label className="block text-sm font-medium mb-1">{t('aiPreferences.contextScope')}</label>
                <select
                  value={aiContextScope}
                  onChange={e => setAiContextScope(e.target.value)}
                  className="w-48 px-3 py-1.5 text-sm border rounded bg-white dark:bg-gray-900"
                >
                  <option value="service">{t('aiPreferences.scopeOptions.service')}</option>
                  <option value="team">{t('aiPreferences.scopeOptions.team')}</option>
                  <option value="all">{t('aiPreferences.scopeOptions.all')}</option>
                </select>
              </div>

              {/* Knowledge Sources */}
              <div>
                <label className="block text-sm font-medium mb-2">{t('aiPreferences.knowledgeSources')}</label>
                <div className="flex flex-wrap gap-2">
                  {AI_KNOWLEDGE_OPTIONS.map(src => {
                    const key = src.replace(/-([a-z])/g, (_, c: string) => c.toUpperCase()) as keyof typeof t;
                    return (
                      <button
                        key={src}
                        type="button"
                        onClick={() =>
                          setAiKnowledgeSources(prev =>
                            prev.includes(src) ? prev.filter(s => s !== src) : [...prev, src]
                          )
                        }
                        className={`text-xs px-3 py-1.5 rounded-full border transition-colors ${
                          aiKnowledgeSources.includes(src)
                            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 font-medium'
                            : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-blue-300'
                        }`}
                      >
                        {t(`aiPreferences.knowledgeSourceOptions.${src === 'knowledge-articles' ? 'knowledgeArticles' : src === 'operational-notes' ? 'operationalNotes' : src}`)}
                      </button>
                    );
                  })}
                </div>
              </div>
            </div>
          </CardBody>
        </Card>
      </div>
    </PageContainer>
  );
}

export default UserPreferencesPage;
