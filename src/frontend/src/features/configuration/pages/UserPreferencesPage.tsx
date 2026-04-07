import { useTranslation } from 'react-i18next';
import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Settings, Sidebar, LayoutDashboard, Save, CheckCircle, XCircle,
  Sun, Moon, Globe, Bell, BellOff, Eye, Accessibility, Monitor,
  Table, Clock, Type,
} from 'lucide-react';
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
  const [digestFrequency, setDigestFrequency] = useState(24);
  const [subscribedCategories, setSubscribedCategories] = useState<string[]>(NOTIFICATION_CATEGORIES);

  const [saveStatus, setSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  useEffect(() => {
    if (data?.preferences) {
      const getPref = (key: string) => data.preferences.find(p => p.key === key)?.value;

      // Sidebar & Dashboard
      const sidebar = getPref('platform.sidebar.pinned_items');
      const widgets = getPref('platform.home.active_widgets');
      if (sidebar) try { setPinnedItems(JSON.parse(sidebar)); } catch { /* keep default */ }
      if (widgets) try { setActiveWidgets(JSON.parse(widgets)); } catch { /* keep default */ }

      // Appearance
      const lang = getPref('platform.ui.language');
      const tz = getPref('platform.ui.timezone');
      const dateFmt = getPref('platform.ui.date_display_format');
      const density = getPref('platform.ui.density');
      const anim = getPref('platform.ui.animations.enabled');
      const rows = getPref('platform.ui.table_rows_per_page');
      const fontSize = getPref('platform.ui.code_font_size');
      if (lang) setSelectedLanguage(lang);
      if (tz) setSelectedTimezone(tz);
      if (dateFmt) setSelectedDateFormat(dateFmt);
      if (density) setSelectedDensity(density);
      if (anim) setAnimationsEnabled(anim === 'true');
      if (rows) setTableRowsPerPage(parseInt(rows, 10) || 25);
      if (fontSize) setCodeFontSize(parseInt(fontSize, 10) || 13);

      // Accessibility
      const hc = getPref('platform.ui.high_contrast.enabled');
      const rm = getPref('platform.ui.reduced_motion.enabled');
      const ks = getPref('platform.ui.keyboard_shortcuts.enabled');
      if (hc) setHighContrastEnabled(hc === 'true');
      if (rm) setReducedMotionEnabled(rm === 'true');
      if (ks) setKeyboardShortcutsEnabled(ks === 'true');

      // Notifications
      const email = getPref('notifications.user.email_enabled');
      const inapp = getPref('notifications.user.inapp_enabled');
      const digest = getPref('notifications.user.digest_enabled');
      const freq = getPref('notifications.user.digest_frequency_hours');
      const cats = getPref('notifications.user.categories_subscribed');
      if (email) setEmailNotifications(email === 'true');
      if (inapp) setInAppNotifications(inapp === 'true');
      if (digest) setDigestEnabled(digest === 'true');
      if (freq) setDigestFrequency(parseInt(freq, 10) || 24);
      if (cats) try { setSubscribedCategories(JSON.parse(cats)); } catch { /* keep default */ }
    }
  }, [data]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const prefs: [string, string][] = [
        // Sidebar & Dashboard
        ['platform.sidebar.pinned_items', JSON.stringify(pinnedItems)],
        ['platform.home.active_widgets', JSON.stringify(activeWidgets)],
        // Appearance
        ['platform.ui.language', selectedLanguage],
        ['platform.ui.timezone', selectedTimezone],
        ['platform.ui.date_display_format', selectedDateFormat],
        ['platform.ui.density', selectedDensity],
        ['platform.ui.animations.enabled', String(animationsEnabled)],
        ['platform.ui.table_rows_per_page', String(tableRowsPerPage)],
        ['platform.ui.code_font_size', String(codeFontSize)],
        // Accessibility
        ['platform.ui.high_contrast.enabled', String(highContrastEnabled)],
        ['platform.ui.reduced_motion.enabled', String(reducedMotionEnabled)],
        ['platform.ui.keyboard_shortcuts.enabled', String(keyboardShortcutsEnabled)],
        // Notifications
        ['notifications.user.email_enabled', String(emailNotifications)],
        ['notifications.user.inapp_enabled', String(inAppNotifications)],
        ['notifications.user.digest_enabled', String(digestEnabled)],
        ['notifications.user.digest_frequency_hours', String(digestFrequency)],
        ['notifications.user.categories_subscribed', JSON.stringify(subscribedCategories)],
      ];
      await Promise.all(prefs.map(([key, value]) => savePreference(key, value)));
      // Apply language change
      if (selectedLanguage !== i18n.language) {
        await i18n.changeLanguage(selectedLanguage);
      }
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
                      onChange={(e) => setDigestFrequency(parseInt(e.target.value, 10))}
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
