import i18n, { type ResourceLanguage } from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import ptBR from './locales/pt-BR.json';
import ptPT from './locales/pt-PT.json';
import es from './locales/es.json';

/**
 * Configuração do i18next para internacionalização do frontend.
 *
 * Idiomas suportados:
 * - en (inglês) — fallback padrão
 * - pt-BR (português do Brasil)
 * - pt-PT (português de Portugal)
 * - es (espanhol)
 *
 * Detecção automática: utiliza navigator.language para selecionar o idioma
 * mais adequado. Variantes de português (pt-BR vs pt-PT) são diferenciadas.
 * Espanhol é detectado para qualquer variante (es-*).
 *
 * Namespaces organizados por domínio: auth, common, sidebar, users, etc.
 *
 * Segurança: escapeValue está habilitado (padrão seguro) para prevenir XSS
 * via interpolação de valores dinâmicos nas traduções.
 * React já escapa JSX nativamente, mas manter escapeValue=true protege
 * contra cenários onde t() é usado fora de JSX (ex.: atributos, títulos).
 */

/**
 * Detecta o idioma preferido do utilizador com base no navigator.language.
 * Diferencia pt-BR de pt-PT e detecta espanhol para qualquer variante.
 */
function detectLanguage(): string {
  const lang = navigator.language;

  if (lang === 'pt-PT' || lang.startsWith('pt-PT')) return 'pt-PT';
  if (lang.startsWith('pt')) return 'pt-BR';
  if (lang.startsWith('es')) return 'es';

  return 'en';
}

/**
 * Constrói os recursos de um idioma.
 *
 * O ficheiro de tradução é flat (um único objeto por idioma). A app usa-o de
 * duas formas: a maioria das páginas via namespace por omissão `translation`
 * com chaves pontilhadas (`t('catalog.x')`), mas ~30 páginas usam
 * `useTranslation('<secção>')` esperando a secção de topo como namespace
 * (`t('x')`). Para suportar ambas sem duplicar traduções, exponho cada objeto
 * de topo TAMBÉM como namespace, além do namespace `translation` completo.
 */
function buildResources(flat: Record<string, unknown>): ResourceLanguage {
  const namespaces: ResourceLanguage = { translation: flat as ResourceLanguage['translation'] };
  for (const [key, value] of Object.entries(flat)) {
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      namespaces[key] = value as ResourceLanguage[string];
    }
  }
  return namespaces;
}

i18n.use(initReactI18next).init({
  resources: {
    en: buildResources(en),
    'pt-BR': buildResources(ptBR),
    'pt-PT': buildResources(ptPT),
    es: buildResources(es),
  },
  lng: detectLanguage(),
  fallbackLng: 'en',
  interpolation: {
    escapeValue: true,
  },
});

export default i18n;
