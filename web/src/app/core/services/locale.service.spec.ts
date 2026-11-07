import { LOCALE_ID, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LOCAL_STORAGE } from '../storage/app-storage';
import { LocaleService } from './locale.service';

describe('LocaleService', () => {
    function create(localeId: string, platformId = 'browser'): LocaleService {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                { provide: LOCALE_ID, useValue: localeId },
                { provide: PLATFORM_ID, useValue: platformId },
                { provide: LOCAL_STORAGE, useValue: localStorage },
            ],
        });
        return TestBed.inject(LocaleService);
    }

    afterEach(() => localStorage.removeItem('preferredLanguage'));

    it('should resolve the English build locale', () => {
        expect(create('en-US').getCurrentLanguage()).toBe('en');
    });

    it('should resolve the Brazilian Portuguese build locale', () => {
        expect(create('pt-BR').getCurrentLanguage()).toBe('pt-BR');
    });

    it('should resolve the Spanish build locale', () => {
        expect(create('es-ES').getCurrentLanguage()).toBe('es');
    });

    it('should fall back to English for an unsupported locale', () => {
        expect(create('fr-FR').getCurrentLanguage()).toBe('en');
    });

    it('should not read browser language on the server', () => {
        expect(create('en', 'server').getBrowserLanguage()).toBe('');
    });

    it('should persist a supported preference through the adapter', () => {
        const service = create('en');

        service.savePreference('pt-BR');

        expect(service.getStoredPreference()).toBe('pt-BR');
    });
});
