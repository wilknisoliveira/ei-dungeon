import { Injectable, LOCALE_ID, PLATFORM_ID, inject } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
    SUPPORTED_LANGUAGES,
    DEFAULT_LANGUAGE,
    LANGUAGE_STORAGE_KEY,
} from '../config/supported-languages';
import { LanguageConfiguration } from '../models/language.model';
import { LOCAL_STORAGE } from '../storage/app-storage';

@Injectable({
    providedIn: 'root',
})
export class LocaleService {
    private document = inject(DOCUMENT);
    private localeId = inject(LOCALE_ID);
    private platformId = inject(PLATFORM_ID);
    private storage = inject(LOCAL_STORAGE);

    getSupportedLanguages(): LanguageConfiguration[] {
        return SUPPORTED_LANGUAGES;
    }

    getCurrentLanguage(): string {
        return this.mapRegionalVariant(this.localeId) || DEFAULT_LANGUAGE;
    }

    resolveLanguage(browserLang: string, stored: string): string {
        if (stored && this.isValidLanguage(stored)) {
            return stored;
        }

        if (browserLang) {
            const resolved = this.mapRegionalVariant(browserLang);
            if (resolved && this.isValidLanguage(resolved)) {
                return resolved;
            }
        }

        return DEFAULT_LANGUAGE;
    }

    getBrowserLanguage(): string {
        if (!isPlatformBrowser(this.platformId)) {
            return '';
        }

        const languages = navigator.languages || [navigator.language];

        for (const lang of languages) {
            const resolved = this.mapRegionalVariant(lang);
            if (resolved && this.isValidLanguage(resolved)) {
                return resolved;
            }
        }

        return '';
    }

    savePreference(lang: string): void {
        try {
            if (this.isValidLanguage(lang)) {
                this.storage.setItem(LANGUAGE_STORAGE_KEY, lang);
            }
        } catch (e) {
            console.warn('Failed to save language preference:', e);
        }
    }

    getStoredPreference(): string {
        try {
            const stored = this.storage.getItem(LANGUAGE_STORAGE_KEY);
            if (stored && this.isValidLanguage(stored)) {
                return stored;
            }
        } catch (e) {
            console.warn('Failed to read language preference:', e);
        }
        return '';
    }

    private isValidLanguage(code: string): boolean {
        return SUPPORTED_LANGUAGES.some((lang) => lang.code === code);
    }

    private mapRegionalVariant(langCode: string): string {
        const baseCode = langCode.split('-')[0].toLowerCase();

        const exactMatch = SUPPORTED_LANGUAGES.find(
            (lang) => lang.code.toLowerCase() === langCode.toLowerCase()
        );
        if (exactMatch) {
            return exactMatch.code;
        }

        const baseMatch = SUPPORTED_LANGUAGES.find(
            (lang) => lang.code.split('-')[0].toLowerCase() === baseCode
        );
        if (baseMatch) {
            return baseMatch.code;
        }

        return '';
    }
}
