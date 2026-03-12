import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import ptBR from './locales/pt-BR.json';

/**
 * Configuração do i18next para internacionalização do frontend.
 *
 * Idiomas suportados: en (inglês) e pt-BR (português do Brasil).
 * Idioma padrão: en — detectável pelo navegador via fallback.
 * Namespaces organizados por domínio: auth, common, sidebar, users, etc.
 */
i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    'pt-BR': { translation: ptBR },
  },
  lng: navigator.language.startsWith('pt') ? 'pt-BR' : 'en',
  fallbackLng: 'en',
  interpolation: {
    escapeValue: false,
  },
});

export default i18n;
