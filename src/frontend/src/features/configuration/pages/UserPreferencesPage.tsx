import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Settings, Sidebar, LayoutDashboard, Save, CheckCircle2, XCircle,
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
import { Button } from '../../../components/Button';
import { Select } from '../../../components/Select';
import { TextField } from '../../../components/TextField';
import { Checkbox } from '../../../components/Checkbox';
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

  // ── Estado Sidebar & Dashboard ──
  const [pinnedItems, setPinnedItems] = useState<string[]>([]);
  const [activeWidgets, setActiveWidgets] = useState<string[]>([]);

  // ── Estado Aparência ──
  const [selectedLanguage, setSelectedLanguage] = useState(i18n.language || 'en');
  const [selectedTimezone, setSelectedTimezone] = useState('UTC');
  const [selectedDateFormat, setSelectedDateFormat] = useState('relative');
  const [selectedDensity, setSelectedDensity] = useState('comfortable');
  const [animationsEnabled, setAnimationsEnabled] = useState(true);
  const [tableRowsPerPage, setTableRowsPerPage] = useState(25);
  const [codeFontSize, setCodeFontSize] = useState(13);

  // ── Estado Acessibilidade ──
  const [highContrastEnabled, setHighContrastEnabled] = useState(false);
  const [reducedMotionEnabled, setReducedMotionEnabled] = useState(false);
  const [keyboardShortcutsEnabled, setKeyboardShortcutsEnabled] = useState(true);

  // ── Estado Notificações ──
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

  // ── Estado Preferências de IA ─────────────────────────────────────────────
  const [aiVerbosity, setAiVerbosity] = useState('standard');
  const [aiLanguage, setAiLanguage] = useState('en');
  const [aiContextScope, setAiContextScope] = useState('team');
  const [aiKnowledgeSources, setAiKnowledgeSources] = useState<string[]>(['contracts', 'services', 'changes', 'incidents', 'runbooks']);

  const AI_KNOWLEDGE_OPTIONS = ['contracts', 'services', 'changes', 'incidents', 'runbooks', 'knowledge-articles', 'operational-notes'];
  const [defaultEnv, setDefaultEnv] = useState('');
  const [defaultTeam, setDefaultTeam] = useState('');
  const [defaultService, setDefaultService] = useState('');

  // Sincroniza preferências do servidor no estado local após carregamento
  const [prefsInitialized, setPrefsInitialized] = useState(false);
  if (data?.preferences && !prefsInitialized) {
    setPrefsInitialized(true);
    const prefs = data.preferences;
    const find = (k: string) => prefs.find(p => p.key === k)?.value;
    const sidebar = find('platform.sidebar.pinned_items');
    const widgets = find('platform.home.active_widgets');
    if (sidebar) { try { setPinnedItems(JSON.parse(sidebar)); } catch { /* manter padrão */ } }
    if (widgets) { try { setActiveWidgets(JSON.parse(widgets)); } catch { /* manter padrão */ } }
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
    if (dsec) { try { setDigestSections(JSON.parse(dsec)); } catch { /* manter padrão */ } }
    const aiVerb = find('user.ai.response_verbosity');
    const aiLang = find('user.ai.preferred_language');
    const aiScope = find('user.ai.auto_context_scope');
    const aiKnow = find('user.ai.knowledge_sources');
    if (aiVerb) setAiVerbosity(aiVerb);
    if (aiLang) setAiLanguage(aiLang);
    if (aiScope) setAiContextScope(aiScope);
    if (aiKnow) { try { setAiKnowledgeSources(JSON.parse(aiKnow)); } catch { /* manter padrão */ } }
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
        {/* Linha 1: Aparência e Tema */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Aparência */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Eye size={18} />
              <span>{t('userPreferences.appearance.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Toggle de Tema */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  {t('userPreferences.appearance.theme')}
                </label>
                <div className="flex items-center gap-2">
                  {/* Botões de tema usando DS Button com estado ativo via variant */}
                  <Button
                    type="button"
                    size="sm"
                    variant={theme === 'light' ? 'subtle' : 'ghost'}
                    onClick={() => setTheme('light')}
                    icon={<Sun size={14} />}
                  >
                    {t('userPreferences.appearance.light')}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant={theme === 'dark' ? 'subtle' : 'ghost'}
                    onClick={() => setTheme('dark')}
                    icon={<Moon size={14} />}
                  >
                    {t('userPreferences.appearance.dark')}
                  </Button>
                </div>
              </div>

              {/* Densidade */}
              <div>
                <label className="text-sm font-medium text-heading block mb-2">
                  {t('userPreferences.appearance.density')}
                </label>
                <div className="flex items-center gap-2">
                  {DENSITY_OPTIONS.map(opt => (
                    <Button
                      key={opt.value}
                      type="button"
                      size="sm"
                      variant={selectedDensity === opt.value ? 'subtle' : 'ghost'}
                      onClick={() => setSelectedDensity(opt.value)}
                      icon={opt.icon}
                    >
                      {t(`userPreferences.appearance.density_${opt.value}`)}
                    </Button>
                  ))}
                </div>
              </div>

              {/* Animações — Checkbox DS */}
              <Checkbox
                checked={animationsEnabled}
                onChange={(e) => setAnimationsEnabled(e.target.checked)}
                label={t('userPreferences.appearance.animations')}
              />
            </CardBody>
          </Card>

          {/* Idioma e Regional */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Globe size={18} />
              <span>{t('userPreferences.regional.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Select DS de idioma */}
              <Select
                label={t('userPreferences.regional.language')}
                options={LANGUAGES}
                value={selectedLanguage}
                onChange={(e) => setSelectedLanguage(e.target.value)}
                size="sm"
              />

              {/* Select DS de fuso horário */}
              <Select
                label={t('userPreferences.regional.timezone')}
                options={TIMEZONES}
                value={selectedTimezone}
                onChange={(e) => setSelectedTimezone(e.target.value)}
                size="sm"
              />

              {/* Select DS de formato de data */}
              <Select
                label={t('userPreferences.regional.dateFormat')}
                options={DATE_FORMATS}
                value={selectedDateFormat}
                onChange={(e) => setSelectedDateFormat(e.target.value)}
                size="sm"
              />

              {/* Select DS de linhas por página */}
              <Select
                label={t('userPreferences.regional.tableRows')}
                options={TABLE_ROWS_OPTIONS.map(n => ({ value: String(n), label: String(n) }))}
                value={String(tableRowsPerPage)}
                onChange={(e) => setTableRowsPerPage(parseInt(e.target.value, 10))}
                size="sm"
              />

              {/* TextField DS para tamanho de fonte de código */}
              <div className="flex items-end gap-2">
                <TextField
                  label={t('userPreferences.regional.codeFontSize')}
                  type="number"
                  value={codeFontSize}
                  onChange={(e) => setCodeFontSize(Math.max(10, Math.min(24, parseInt(e.target.value, 10) || 13)))}
                  min={10}
                  max={24}
                  size="sm"
                  className="w-24"
                />
                <span className="text-xs text-muted mb-2">px</span>
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Linha 2: Sidebar e Dashboard */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Customização da Sidebar */}
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
                      /* Checkbox DS para módulos fixados na sidebar */
                      <Checkbox
                        key={mod}
                        checked={pinnedItems.includes(mod)}
                        onChange={() => togglePinned(mod)}
                        disabled={!pinnedItems.includes(mod) && pinnedItems.length >= maxPinned}
                        label={t(`userPreferences.sidebar.module_${mod}`, mod.replace('-', ' '))}
                      />
                    ))}
                  </div>
                </>
              )}
            </CardBody>
          </Card>

          {/* Widgets do Dashboard Home */}
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
                  /* Checkbox DS para widgets do dashboard */
                  <Checkbox
                    key={wid}
                    checked={activeWidgets.includes(wid)}
                    onChange={() => toggleWidget(wid)}
                    disabled={!activeWidgets.includes(wid) && activeWidgets.length >= maxW}
                    label={t(`userPreferences.home.widget_${wid}`, wid.replace(/([A-Z])/g, ' $1').trim())}
                  />
                ))}
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Linha 3: Notificações e Acessibilidade */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Preferências de Notificação */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Bell size={18} />
              <span>{t('userPreferences.notifications.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Notificações por email — Checkbox DS */}
              <Checkbox
                checked={emailNotifications}
                onChange={(e) => setEmailNotifications(e.target.checked)}
                label={t('userPreferences.notifications.email')}
              />

              {/* Notificações in-app — Checkbox DS */}
              <Checkbox
                checked={inAppNotifications}
                onChange={(e) => setInAppNotifications(e.target.checked)}
                label={t('userPreferences.notifications.inApp')}
              />

              {/* Digest */}
              <div className="border-t border-edge pt-3">
                <Checkbox
                  checked={digestEnabled}
                  onChange={(e) => setDigestEnabled(e.target.checked)}
                  label={t('userPreferences.notifications.digest')}
                />
                {digestEnabled && (
                  <div className="mt-2 ml-8">
                    {/* Select DS de frequência de digest */}
                    <Select
                      label={t('userPreferences.notifications.digestFrequency')}
                      options={[
                        { value: '6', label: '6h' },
                        { value: '12', label: '12h' },
                        { value: '24', label: '24h' },
                        { value: '48', label: '48h' },
                      ]}
                      value={String(digestFrequency)}
                      onChange={(e) => setDigestFrequency(e.target.value)}
                      size="sm"
                    />
                  </div>
                )}
              </div>

              {/* Categorias */}
              <div className="border-t border-edge pt-3">
                <p className="text-sm font-medium text-heading mb-2">
                  {t('userPreferences.notifications.categories')}
                </p>
                <div className="grid grid-cols-2 gap-2">
                  {NOTIFICATION_CATEGORIES.map(cat => (
                    /* Checkbox DS para categorias de notificação */
                    <Checkbox
                      key={cat}
                      checked={subscribedCategories.includes(cat)}
                      onChange={() => toggleCategory(cat)}
                      label={t(`userPreferences.notifications.category_${cat}`, cat)}
                    />
                  ))}
                </div>
              </div>

              {/* Aviso de mudo total */}
              {!emailNotifications && !inAppNotifications && (
                <div className="flex items-center gap-2 text-xs text-warning">
                  <BellOff size={14} />
                  {t('userPreferences.notifications.allMuted')}
                </div>
              )}
            </CardBody>
          </Card>

          {/* Acessibilidade */}
          <Card>
            <CardHeader className="flex items-center gap-2">
              <Accessibility size={18} />
              <span>{t('userPreferences.accessibility.title')}</span>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Checkbox DS com descrição para alto contraste */}
              <Checkbox
                checked={highContrastEnabled}
                onChange={(e) => setHighContrastEnabled(e.target.checked)}
                label={t('userPreferences.accessibility.highContrast')}
                description={t('userPreferences.accessibility.highContrastDesc')}
              />

              {/* Checkbox DS com descrição para movimento reduzido */}
              <Checkbox
                checked={reducedMotionEnabled}
                onChange={(e) => setReducedMotionEnabled(e.target.checked)}
                label={t('userPreferences.accessibility.reducedMotion')}
                description={t('userPreferences.accessibility.reducedMotionDesc')}
              />

              {/* Checkbox DS com descrição para atalhos de teclado */}
              <Checkbox
                checked={keyboardShortcutsEnabled}
                onChange={(e) => setKeyboardShortcutsEnabled(e.target.checked)}
                label={t('userPreferences.accessibility.keyboardShortcuts')}
                description={t('userPreferences.accessibility.keyboardShortcutsDesc')}
              />
            </CardBody>
          </Card>
        </div>
      </div>

      {/* Fuso Horário e Formato de Data */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Clock size={18} />
            <span>{t('userPreferences.timezone.title')}</span>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* Select DS de timezone */}
              <Select
                label={t('userPreferences.timezone.timezone')}
                options={TIMEZONES}
                value={timezone}
                onChange={e => setTimezone(e.target.value)}
                size="sm"
              />
              {/* Select DS de formato de data */}
              <Select
                label={t('userPreferences.timezone.dateFormat')}
                options={[
                  { value: 'yyyy-MM-dd', label: 'yyyy-MM-dd (ISO)' },
                  { value: 'MM/dd/yyyy', label: 'MM/dd/yyyy (US)' },
                  { value: 'dd/MM/yyyy', label: 'dd/MM/yyyy (EU)' },
                  { value: 'dd.MM.yyyy', label: 'dd.MM.yyyy' },
                ]}
                value={dateFormat}
                onChange={e => setDateFormat(e.target.value)}
                size="sm"
              />
              {/* Select DS de formato de hora */}
              <Select
                label={t('userPreferences.timezone.timeFormat')}
                options={[
                  { value: 'HH:mm:ss', label: 'HH:mm:ss (24h)' },
                  { value: 'hh:mm:ss a', label: 'hh:mm:ss a (12h)' },
                  { value: 'HH:mm', label: 'HH:mm (Short)' },
                ]}
                value={timeFormat}
                onChange={e => setTimeFormat(e.target.value)}
                size="sm"
              />
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Escopo Padrão */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Globe size={18} />
            <span>{t('userPreferences.defaultScope.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">{t('userPreferences.defaultScope.description')}</p>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* TextField DS para ambiente padrão */}
              <TextField
                label={t('userPreferences.defaultScope.environment')}
                type="text"
                value={defaultEnv}
                onChange={e => setDefaultEnv(e.target.value)}
                placeholder="e.g. production"
                size="sm"
              />
              {/* TextField DS para equipe padrão */}
              <TextField
                label={t('userPreferences.defaultScope.team')}
                type="text"
                value={defaultTeam}
                onChange={e => setDefaultTeam(e.target.value)}
                placeholder="e.g. platform"
                size="sm"
              />
              {/* TextField DS para serviço padrão */}
              <TextField
                label={t('userPreferences.defaultScope.service')}
                type="text"
                value={defaultService}
                onChange={e => setDefaultService(e.target.value)}
                placeholder="e.g. api-gateway"
                size="sm"
              />
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Itens por Página */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <List size={18} />
            <span>{t('userPreferences.itemsPerPage.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-3">{t('userPreferences.itemsPerPage.description')}</p>
            {/* Select DS para itens por página */}
            <Select
              options={[
                { value: '10', label: '10' },
                { value: '25', label: '25' },
                { value: '50', label: '50' },
                { value: '100', label: '100' },
              ]}
              value={itemsPerPage}
              onChange={e => setItemsPerPage(e.target.value)}
              size="sm"
              className="w-40"
            />
          </CardBody>
        </Card>
      </div>

      {/* Horas de Silêncio */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <BellOff size={18} />
            <span>{t('quietHours.title')}</span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">{t('quietHours.subtitle')}</p>
            <div className="space-y-4">
              {/* Checkbox DS para habilitar horas de silêncio */}
              <Checkbox
                checked={quietHoursEnabled}
                onChange={e => setQuietHoursEnabled(e.target.checked)}
                label={t('quietHours.enabled')}
              />
              {quietHoursEnabled && (
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pl-7">
                  {/* TextField DS tipo time para início */}
                  <TextField
                    label={t('quietHours.start')}
                    type="time"
                    value={quietHoursStart}
                    onChange={e => setQuietHoursStart(e.target.value)}
                    size="sm"
                  />
                  {/* TextField DS tipo time para fim */}
                  <TextField
                    label={t('quietHours.end')}
                    type="time"
                    value={quietHoursEnd}
                    onChange={e => setQuietHoursEnd(e.target.value)}
                    size="sm"
                  />
                  {/* Select DS de fuso horário para horas de silêncio */}
                  <Select
                    label={t('quietHours.timezone')}
                    options={TIMEZONES}
                    value={quietHoursTimezone}
                    onChange={e => setQuietHoursTimezone(e.target.value)}
                    size="sm"
                  />
                </div>
              )}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Configurações de Digest */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Rss size={18} />
            <span>{t('digestSettings.title')}</span>
          </CardHeader>
          <CardBody>
            <div className="space-y-4">
              {/* Select DS de frequência */}
              <Select
                label={t('digestSettings.frequency')}
                options={[
                  { value: 'daily', label: t('digestSettings.frequencyOptions.daily') },
                  { value: 'weekly', label: t('digestSettings.frequencyOptions.weekly') },
                  { value: 'none', label: t('digestSettings.frequencyOptions.none') },
                ]}
                value={digestFrequency}
                onChange={e => setDigestFrequency(e.target.value)}
                size="sm"
                className="w-48"
              />
              {digestFrequency !== 'none' && (
                <div>
                  <p className="text-sm font-medium text-heading mb-2">{t('digestSettings.sections')}</p>
                  <div className="flex flex-wrap gap-2">
                    {DIGEST_SECTION_OPTIONS.map(sec => (
                      /* Botão-chip de seleção de secções do digest — DS Button */
                      <Button
                        key={sec}
                        type="button"
                        size="xs"
                        variant={digestSections.includes(sec) ? 'subtle' : 'outline'}
                        onClick={() =>
                          setDigestSections(prev =>
                            prev.includes(sec) ? prev.filter(s => s !== sec) : [...prev, sec]
                          )
                        }
                      >
                        {sec}
                      </Button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Botão Salvar */}
      <div className="mt-6 flex items-center gap-3">
        <Button
          variant="primary"
          onClick={() => saveMutation.mutate()}
          disabled={saveStatus === 'saving'}
          icon={<Save size={16} />}
        >
          {saveStatus === 'saving' ? t('common.saving', 'Saving...') : t('userPreferences.save')}
        </Button>
        {saveStatus === 'saved' && (
          <span className="flex items-center gap-1 text-success text-sm">
            <CheckCircle2 size={14} /> {t('userPreferences.saved')}
          </span>
        )}
        {saveStatus === 'error' && (
          <span className="flex items-center gap-1 text-critical text-sm">
            <XCircle size={14} /> {t('userPreferences.error')}
          </span>
        )}
      </div>

      {/* Preferências de IA */}
      <div className="mt-6">
        <Card>
          <CardHeader className="flex items-center gap-2">
            <Bot size={18} />
            <span>{t('aiPreferences.title')}</span>
          </CardHeader>
          <CardBody>
            <div className="space-y-5">
              {/* Verbosidade da Resposta — Select DS */}
              <Select
                label={t('aiPreferences.verbosity')}
                options={[
                  { value: 'concise', label: t('aiPreferences.verbosityOptions.concise') },
                  { value: 'standard', label: t('aiPreferences.verbosityOptions.standard') },
                  { value: 'detailed', label: t('aiPreferences.verbosityOptions.detailed') },
                ]}
                value={aiVerbosity}
                onChange={e => setAiVerbosity(e.target.value)}
                size="sm"
                className="w-48"
              />

              {/* Idioma Preferido — TextField DS */}
              <TextField
                label={t('aiPreferences.language')}
                type="text"
                value={aiLanguage}
                onChange={e => setAiLanguage(e.target.value)}
                placeholder="en"
                size="sm"
                className="w-32"
              />

              {/* Escopo de Contexto Automático — Select DS */}
              <Select
                label={t('aiPreferences.contextScope')}
                options={[
                  { value: 'service', label: t('aiPreferences.scopeOptions.service') },
                  { value: 'team', label: t('aiPreferences.scopeOptions.team') },
                  { value: 'all', label: t('aiPreferences.scopeOptions.all') },
                ]}
                value={aiContextScope}
                onChange={e => setAiContextScope(e.target.value)}
                size="sm"
                className="w-48"
              />

              {/* Fontes de Conhecimento */}
              <div>
                <p className="text-sm font-medium text-heading mb-2">{t('aiPreferences.knowledgeSources')}</p>
                <div className="flex flex-wrap gap-2">
                  {AI_KNOWLEDGE_OPTIONS.map(src => (
                    /* Botão-chip de seleção de fontes — DS Button */
                    <Button
                      key={src}
                      type="button"
                      size="xs"
                      variant={aiKnowledgeSources.includes(src) ? 'subtle' : 'outline'}
                      onClick={() =>
                        setAiKnowledgeSources(prev =>
                          prev.includes(src) ? prev.filter(s => s !== src) : [...prev, src]
                        )
                      }
                    >
                      {t(`aiPreferences.knowledgeSourceOptions.${src === 'knowledge-articles' ? 'knowledgeArticles' : src === 'operational-notes' ? 'operationalNotes' : src}`)}
                    </Button>
                  ))}
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
