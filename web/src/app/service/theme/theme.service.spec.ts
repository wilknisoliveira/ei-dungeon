import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/common';
import { OverlayContainer } from '@angular/cdk/overlay';
import { PLATFORM_ID } from '@angular/core';

import {
    DARK_THEME,
    LIGHT_THEME,
    THEME_STORAGE_KEY,
    ThemeService,
} from './theme.service';

describe('ThemeService', () => {
    let service: ThemeService;
    let document: Document;
    let overlayElement: HTMLElement;

    beforeEach(() => {
        TestBed.configureTestingModule({});

        service = TestBed.inject(ThemeService);
        document = TestBed.inject(DOCUMENT);
        overlayElement = TestBed.inject(OverlayContainer).getContainerElement();

        localStorage.removeItem(THEME_STORAGE_KEY);
        document.body.classList.remove(DARK_THEME, LIGHT_THEME);
    });

    afterEach(() => {
        localStorage.removeItem(THEME_STORAGE_KEY);
        document.body.classList.remove(DARK_THEME, LIGHT_THEME);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should apply the dark theme when nothing is stored', () => {
        service.initTheme();

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
        expect(document.body.classList.contains(LIGHT_THEME)).toBeFalse();
        expect(service.isDarkMode()).toBeTrue();
    });

    it('should honour a stored light preference', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);

        service.initTheme();

        expect(document.body.classList.contains(LIGHT_THEME)).toBeTrue();
        expect(document.body.classList.contains(DARK_THEME)).toBeFalse();
        expect(service.isDarkMode()).toBeFalse();
    });

    it('should fall back to dark when the stored value is not a known theme', () => {
        localStorage.setItem(THEME_STORAGE_KEY, 'theme-purple');

        service.initTheme();

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
    });

    it('should persist the theme by default', () => {
        service.setTheme(false);

        expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe(LIGHT_THEME);
    });

    it('should not persist when persist is false', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);

        service.setTheme(true, false);

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
        expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe(LIGHT_THEME);
    });

    it('should force dark without overwriting a stored light preference', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);

        service.forceDark();

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
        expect(document.body.classList.contains(LIGHT_THEME)).toBeFalse();
        expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe(LIGHT_THEME);
    });

    it('should restore the stored theme after forcing dark', () => {
        localStorage.setItem(THEME_STORAGE_KEY, LIGHT_THEME);

        service.forceDark();
        service.restoreStoredTheme();

        expect(document.body.classList.contains(LIGHT_THEME)).toBeTrue();
        expect(document.body.classList.contains(DARK_THEME)).toBeFalse();
    });

    it('should keep the overlay container class in sync with the body', () => {
        service.setTheme(false);

        expect(overlayElement.classList.contains(LIGHT_THEME)).toBeTrue();
        expect(overlayElement.classList.contains(DARK_THEME)).toBeFalse();

        service.setTheme(true);

        expect(overlayElement.classList.contains(DARK_THEME)).toBeTrue();
        expect(overlayElement.classList.contains(LIGHT_THEME)).toBeFalse();
    });

    it('should default to dark when localStorage throws', () => {
        spyOn(localStorage, 'getItem').and.throwError('denied');

        service.initTheme();

        expect(document.body.classList.contains(DARK_THEME)).toBeTrue();
    });

    it('should apply the deterministic dark theme on the server', () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [{ provide: PLATFORM_ID, useValue: 'server' }],
        });
        const serverService = TestBed.inject(ThemeService);
        const serverDocument = TestBed.inject(DOCUMENT);

        serverService.initTheme();

        expect(serverDocument.body.classList.contains(DARK_THEME)).toBeTrue();
    });
});
