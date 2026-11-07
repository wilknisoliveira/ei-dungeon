import { LanguageConfiguration } from '../models/language.model';

export const SUPPORTED_LANGUAGES: LanguageConfiguration[] = [
    {
        code: 'en',
        name: 'English',
        abbreviation: 'EN',
        locale: 'en-US',
        baseHref: '/en/',
    },
    {
        code: 'pt-BR',
        name: 'Português',
        abbreviation: 'PT',
        locale: 'pt-BR',
        baseHref: '/pt-BR/',
    },
    {
        code: 'es',
        name: 'Español',
        abbreviation: 'ES',
        locale: 'es-ES',
        baseHref: '/es/',
    },
];

export const DEFAULT_LANGUAGE = 'en';
export const LANGUAGE_STORAGE_KEY = 'preferredLanguage';
