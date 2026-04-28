import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Settings, Sidebar, LayoutDashboard, Save, CheckCircle, XCircle,
  Sun, Moon, Globe, Bell, BellOff, Eye, Accessibility, Monitor,
  Table, Clock, Type, List, Rss, Bot,
} from 'lucide-react';

const TIMEZONE_VALUES = [
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
import { useTheme } from '../../../contexts/ThemeContext';

/**
 * UserPreferencesPage — personalização de plataforma por utilizador.
 * Permite configurar sidebar (pinned items), home dashboard (widgets),
 * aparência (theme, density, animations), notificações, acessibilidade e formato de dados.
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

const LANGUAGES = [
  { value: 'en', label: 'English' },
  { value: 'pt-BR', label: 'Português (Brasil)' },
  { value: 'pt-PT', label: 'Português (Portugal)' },
  { value: 'es', label: 'Español' },
];

const TIMEZONES = [
  { value: 'UTC', label: 'UTC' },
  { value: 'America/New_York', label: 'Eastern Time (US)' },
  { value: 'America/Chicago', label: 'Central Time (US)' },
  { value: 'America/Denver', label: 'Mountain Time (US)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (US)' },
  { value: 'America/Sao_Paulo', label: 'São Paulo (BRT)' },
  { value: 'Europe/London', label: 'London (GMT/BST)' },
  { value: 'Europe/Lisbon', label: 'Lisbon (WET)' },
  { value: 'Europe/Madrid', label: 'Madrid (CET)' },
  { value: 'Europe/Berlin', label: 'Berlin (CET)' },
  { value: 'Europe/Paris', label: 'Paris (CET)' },
  { value: 'Asia/Tokyo', label: 'Tokyo (JST)' },
  { value: 'Asia/Shanghai', label: 'Shanghai (CST)' },
  { value: 'Australia/Sydney', label: 'Sydney (AEST)' },
];

const DATE_FORMATS = [
  { value: 'relative', label: 'Relative (2 hours ago)' },
  { value: 'yyyy-MM-dd', label: '2026-04-07' },
  { value: 'dd/MM/yyyy', label: '07/04/2026' },
  { value: 'MM/dd/yyyy', label: '04/07/2026' },
  { value: 'dd-MM-yyyy', label: '07-04-2026' },
];

const DENSITY_OPTIONS = [
  { value: 'compact', icon: <Table size={14} /> },
  { value: 'comfortable', icon: <Monitor size={14} /> },
  { value: 'spacious', icon: <LayoutDashboard size={14} /> },
];

const TABLE_ROWS_OPTIONS = [10, 15, 20, 25, 50, 100];

const NOTIFICATION_CATEGORIES = [
  'Incidents', 'Changes', 'Governance', 'Security', 'Contracts', 'Operations',
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
  const { t, i18n } = useTranslation();
  const { theme, setTheme } = useTheme();
  const queryClient = useQueryClient();

  const { data, isLoading, error } = useQuery({
    queryKey: ['user-preferences'],
    queryFn: fetchPreferences,
  });

  // ── Sidebar & Dashboard State ──
  const [pinnedItems, setPinnedItems] = useState<string[]>([]);
  const [activeWidgets, setActiveWidgets] = useState<string[]>([]);

  // ── Appearance State ──
  const [selectedLanguage, setSelectedLanguage] = useState(i18n.language || 'en');
  const [selectedTimezone, setSelectedTimezone] = useState('UTC');
  const [selectedDateFormat, setSelectedDateFormat] = useState('relative');
  const [selectedDensity, setSelectedDensity] = useState('comfortable');
  const [animationsEnabled, setAnimationsEnabled] = useState(true);
  const [tableRowsPerPage, setTableRowsPerPage] = useState(25);
  const [codeFontSize, setCodeFontSize] = useState(13);

  // ── Accessibility State ──
  const [highContrastEnabled, setHighContrastEnabled] = useState(false);
  const [reducedMotionEnabled, setReducedMotionEnabled] = useState(false);
  const [keyboardShortcutsEnabled, setKeyboardShortcutsEnabled] = useState(true);

  // ── Notifications State ──
  const [emailNotifications, setEmailNotifications] = useState(true);
  const [inAppNotifications, setInAppNotifications] = useState(true);
  const [digestEnabled, setDigestEnabled] = useState(false);
  const [subscribedCategories, setSubscribedCategories] = useState<string[]>(NOTIFICATION_CATEGORIES);

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

  // Sync server preferences into local state once data arrives
  const [prefsInitialized, setPrefsInitialized] = useState(false);
  if (data?.preferences && !prefsInitialized) {
    setPrefsInitialized(true);
    const prefs = data.preferences;
    const find = (k: string) => prefs.find(p => p.key === k)?.value;
    const sidebar = find('platform.sidebar.pinned_items');
    const widgets = find('platform.home.active_widgets');
    if (sidebar) { try { setPinnedItems(JSON.parse(sidebar)); } catch { /* keep default */ } }
    if (widgets) { try { setActiveWidgets(JSON.parse(widgets)); } catch { /* keep default */ } }
    const tz = find('user.timezone');
    const df = find('user.date_format');
    const tf = find('user.time_format');
    const ipp = find('user.items_per_page');
    const de = find('default.environment');
    const dtPref = find('default.team');
    const ds = find('default.service');
    if (tz) setTimezone(tz);
    if (df) setDateFormat(df);
    if (tf) setTimeFormat(tf);
    if (ipp) setItemsPerPage(ipp);
    if (de) setDefaultEnv(de);
    if (dtPref) setDefaultTeam(dtPref);
    if (ds) setDefaultService(ds);
    const qhe = find('notifications.quiet_hours.enabled');
    const qhs = find('notifications.quiet_hours.start');
    const qhend = find('notifications.quiet_hours.end');
    const qhtz = find('notifications.quiet_hours.timezone');
    const df2 = find('notifications.digest.frequency');
    const dsec = find('notifications.digest.sections');
    if (qhe) setQuietHoursEnabled(qhe === 'true');
    if (qhs) setQuietHoursStart(qhs);
    if (qhend) setQuietHoursEnd(qhend);
    if (qhtz) setQuietHoursTimezone(qhtz);
    if (df2) setDigestFrequency(df2);
    if (dsec) { try { setDigestSections(JSON.parse(dsec)); } catch { /* keep default */ } }
    const aiVerb = find('user.ai.response_verbosity');
    const aiLang = find('user.ai.preferred_language');
    const aiScope = find('user.ai.auto_context_scope');
    const aiKnow = find('user.ai.knowledge_sources');
    if (aiVerb) setAiVerbosity(aiVerb);
    if (aiLang) setAiLanguage(aiLang);
    if (aiScope) setAiContextScope(aiScope);
    if (aiKnow) { try { setAiKnowledgeSources(JSON.parse(aiKnow)); } catch { /* keep default */ } }
  }

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
  if (error) return <PageContainer><PageErrorState message={t('userPreferences.error')} /></PageContainer>;

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

  const toggleCategory = (cat: string) => {
    if (subscribedCategories.includes(cat)) {
      setSubscribedCategories(subscribedCategories.filter(c => c !== cat));
    } else {
      setSubscribedCategories([...subscribedCategories, cat]);
    }
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('userPreferences.title')}
        subtitle={t('userPreferences.description')}
        icon={<Settings size={24} />}
      />

      <div className="space-y-6 mt-6">
        {/* Row 1: Appearance & Theme */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Appearance */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Eye size={18} />
              <span>{t('userPreferences.appearance.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Theme Toggle */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  {t('userPreferences.appearance.theme')}
                </label>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setTheme('light')}
                    className={`flex items-center gap-2 px-3 py-2 rounded-md border text-sm transition-colors ${
                      theme === 'light'
                        ? 'border-accent bg-accent/10 text-accent'
                        : 'border-edge text-muted hover:text-heading'
                    }`}
                  >
                    <Sun size={14} />
                    {t('userPreferences.appearance.light')}
                  </button>
                  <button
                    type="button"
                    onClick={() => setTheme('dark')}
                    className={`flex items-center gap-2 px-3 py-2 rounded-md border text-sm transition-colors ${
                      theme === 'dark'
                        ? 'border-accent bg-accent/10 text-accent'
                        : 'border-edge text-muted hover:text-heading'
                    }`}
                  >
                    <Moon size={14} />
                    {t('userPreferences.appearance.dark')}
                  </button>
                </div>
              </div>

              {/* Density */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  {t('userPreferences.appearance.density')}
                </label>
                <div className="flex items-center gap-2">
                  {DENSITY_OPTIONS.map(opt => (
                    <button
                      key={opt.value}
                      type="button"
                      onClick={() => setSelectedDensity(opt.value)}
                      className={`flex items-center gap-2 px-3 py-2 rounded-md border text-sm transition-colors ${
                        selectedDensity === opt.value
                          ? 'border-accent bg-accent/10 text-accent'
                          : 'border-edge text-muted hover:text-heading'
                      }`}
                    >
                      {opt.icon}
                      {t(`userPreferences.appearance.density_${opt.value}`)}
                    </button>
                  ))}
                </div>
              </div>

              {/* Animations */}
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={animationsEnabled}
                  onChange={(e) => setAnimationsEnabled(e.target.checked)}
                  className="rounded"
                />
                <span className="text-sm text-heading">
                  {t('userPreferences.appearance.animations')}
                </span>
              </label>
            </CardBody>
          </Card>

          {/* Language & Regional */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Globe size={18} />
              <span>{t('userPreferences.regional.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Language */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  {t('userPreferences.regional.language')}
                </label>
                <select
                  value={selectedLanguage}
                  onChange={(e) => setSelectedLanguage(e.target.value)}
                  className="w-full rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading"
                >
                  {LANGUAGES.map(lang => (
                    <option key={lang.value} value={lang.value}>{lang.label}</option>
                  ))}
                </select>
              </div>

              {/* Timezone */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  <Clock size={14} className="inline mr-1.5" />
                  {t('userPreferences.regional.timezone')}
                </label>
                <select
                  value={selectedTimezone}
                  onChange={(e) => setSelectedTimezone(e.target.value)}
                  className="w-full rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading"
                >
                  {TIMEZONES.map(tz => (
                    <option key={tz.value} value={tz.value}>{tz.label}</option>
                  ))}
                </select>
              </div>

              {/* Date Format */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  {t('userPreferences.regional.dateFormat')}
                </label>
                <select
                  value={selectedDateFormat}
                  onChange={(e) => setSelectedDateFormat(e.target.value)}
                  className="w-full rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading"
                >
                  {DATE_FORMATS.map(fmt => (
                    <option key={fmt.value} value={fmt.value}>{fmt.label}</option>
                  ))}
                </select>
              </div>

              {/* Table rows per page */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  <Table size={14} className="inline mr-1.5" />
                  {t('userPreferences.regional.tableRows')}
                </label>
                <select
                  value={tableRowsPerPage}
                  onChange={(e) => setTableRowsPerPage(parseInt(e.target.value, 10))}
                  className="w-full rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading"
                >
                  {TABLE_ROWS_OPTIONS.map(n => (
                    <option key={n} value={n}>{n}</option>
                  ))}
                </select>
              </div>

              {/* Code Font Size */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  <Type size={14} className="inline mr-1.5" />
                  {t('userPreferences.regional.codeFontSize')}
                </label>
                <input
                  type="number"
                  value={codeFontSize}
                  onChange={(e) => setCodeFontSize(Math.max(10, Math.min(24, parseInt(e.target.value, 10) || 13)))}
                  min={10}
                  max={24}
                  className="w-24 rounded-md border border-edge bg-input px-3 py-2 text-sm text-heading"
                />
                <span className="text-xs text-muted ml-2">px</span>
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Row 2: Sidebar & Dashboard */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
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
                        <span className="text-sm">{t(`userPreferences.sidebar.module_${mod}`, mod.replace('-', ' '))}</span>
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
                    <span className="text-sm">{t(`userPreferences.home.widget_${wid}`, wid.replace(/([A-Z])/g, ' $1').trim())}</span>
                  </label>
                ))}
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Row 3: Notifications & Accessibility */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Notification Preferences */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Bell size={18} />
              <span>{t('userPreferences.notifications.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Email Notifications */}
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={emailNotifications}
                  onChange={(e) => setEmailNotifications(e.target.checked)}
                  className="rounded"
                />
                <span className="text-sm text-heading">
                  {t('userPreferences.notifications.email')}
                </span>
              </label>

              {/* In-App Notifications */}
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={inAppNotifications}
                  onChange={(e) => setInAppNotifications(e.target.checked)}
                  className="rounded"
                />
                <span className="text-sm text-heading">
                  {t('userPreferences.notifications.inApp')}
                </span>
              </label>

              {/* Digest */}
              <div className="border-t border-edge pt-3">
                <label className="flex items-center gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={digestEnabled}
                    onChange={(e) => setDigestEnabled(e.target.checked)}
                    className="rounded"
                  />
                  <span className="text-sm text-heading">
                    {t('userPreferences.notifications.digest')}
                  </span>
                </label>
                {digestEnabled && (
                  <div className="mt-2 ml-7">
                    <label className="text-xs text-muted block mb-1">
                      {t('userPreferences.notifications.digestFrequency')}
                    </label>
                    <select
                      value={digestFrequency}
                      onChange={(e) => setDigestFrequency(e.target.value)}
                      className="rounded-md border border-edge bg-input px-3 py-1.5 text-sm text-heading"
                    >
                      <option value={6}>6h</option>
                      <option value={12}>12h</option>
                      <option value={24}>24h</option>
                      <option value={48}>48h</option>
                    </select>
                  </div>
                )}
              </div>

              {/* Categories */}
              <div className="border-t border-edge pt-3">
                <p className="text-sm font-medium text-heading mb-2">
                  {t('userPreferences.notifications.categories')}
                </p>
                <div className="grid grid-cols-2 gap-2">
                  {NOTIFICATION_CATEGORIES.map(cat => (
                    <label key={cat} className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={subscribedCategories.includes(cat)}
                        onChange={() => toggleCategory(cat)}
                        className="rounded"
                      />
                      <span className="text-sm">{t(`userPreferences.notifications.category_${cat}`, cat)}</span>
                    </label>
                  ))}
                </div>
              </div>

              {/* Mute All */}
              {!emailNotifications && !inAppNotifications && (
                <div className="flex items-center gap-2 text-xs text-warning">
                  <BellOff size={14} />
                  {t('userPreferences.notifications.allMuted')}
                </div>
              )}
            </CardBody>
          </Card>

          {/* Accessibility */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Accessibility size={18} />
              <span>{t('userPreferences.accessibility.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={highContrastEnabled}
                  onChange={(e) => setHighContrastEnabled(e.target.checked)}
                  className="rounded"
                />
                <div>
                  <span className="text-sm text-heading block">
                    {t('userPreferences.accessibility.highContrast')}
                  </span>
                  <span className="text-xs text-muted">
                    {t('userPreferences.accessibility.highContrastDesc')}
                  </span>
                </div>
              </label>

              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={reducedMotionEnabled}
                  onChange={(e) => setReducedMotionEnabled(e.target.checked)}
                  className="rounded"
                />
                <div>
                  <span className="text-sm text-heading block">
                    {t('userPreferences.accessibility.reducedMotion')}
                  </span>
                  <span className="text-xs text-muted">
                    {t('userPreferences.accessibility.reducedMotionDesc')}
                  </span>
                </div>
              </label>

              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  checked={keyboardShortcutsEnabled}
                  onChange={(e) => setKeyboardShortcutsEnabled(e.target.checked)}
                  className="rounded"
                />
                <div>
                  <span className="text-sm text-heading block">
                    {t('userPreferences.accessibility.keyboardShortcuts')}
                  </span>
                  <span className="text-xs text-muted">
                    {t('userPreferences.accessibility.keyboardShortcutsDesc')}
                  </span>
                </div>
              </label>
            </CardBody>
          </Card>
        </div>
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
                  {TIMEZONES.map(tz => <option key={tz.value} value={tz.value}>{tz.label}</option>)}
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
                      {TIMEZONES.map(tz => <option key={tz.value} value={tz.value}>{tz.label}</option>)}
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
