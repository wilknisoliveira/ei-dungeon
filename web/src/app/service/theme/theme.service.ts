import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { OverlayContainer } from '@angular/cdk/overlay';
import { LOCAL_STORAGE } from 'src/app/core/storage/app-storage';

export const DARK_THEME = 'theme-dark';
export const LIGHT_THEME = 'theme-light';
export const THEME_STORAGE_KEY = 'app-theme';

/**
 * Owns theme application for the whole application.
 *
 * The theme classes live on the body and on the CDK overlay container, and every
 * `--mat-sys-*` token is scoped under them in `styles.scss`. Application must therefore
 * happen at startup, from `AppComponent`, and not as a side effect of a toggle rendering.
 */
@Injectable({
    providedIn: 'root',
})
export class ThemeService {
    private document = inject(DOCUMENT);
    private overlay = inject(OverlayContainer);
    private platformId = inject(PLATFORM_ID);
    private storage = inject(LOCAL_STORAGE);

    private readonly currentTheme = signal<string>(DARK_THEME);

    /** The theme currently applied to the document. */
    readonly theme = this.currentTheme.asReadonly();

    /** True when the applied theme is dark. */
    isDarkMode(): boolean {
        return this.currentTheme() === DARK_THEME;
    }

    /**
     * Applies the stored preference, defaulting to dark. Called once at startup so the
     * document is themed before any page renders.
     */
    initTheme(): void {
        this.applyTheme(this.getStoredTheme());
    }

    /** Applies a theme and, unless told otherwise, persists it as the user's preference. */
    setTheme(isDarkMode: boolean, persist: boolean = true): void {
        const theme = isDarkMode ? DARK_THEME : LIGHT_THEME;

        this.applyTheme(theme);

        if (persist) {
            this.savePreference(theme);
        }
    }

    /**
     * Forces dark without touching the stored preference. Used by pages that are dark only,
     * so a light-theme user keeps their preference once they navigate away.
     */
    forceDark(): void {
        this.applyTheme(DARK_THEME);
    }

    /** Reapplies whatever the user actually chose. Pairs with `forceDark`. */
    restoreStoredTheme(): void {
        this.applyTheme(this.getStoredTheme());
    }

    /** Reads the stored preference, falling back to dark. */
    getStoredTheme(): string {
        try {
            const stored = this.storage.getItem(THEME_STORAGE_KEY);
            if (stored === LIGHT_THEME || stored === DARK_THEME) {
                return stored;
            }
        } catch (e) {
            console.warn('Failed to read theme preference:', e);
        }

        return DARK_THEME;
    }

    private applyTheme(theme: string): void {
        const other = theme === DARK_THEME ? LIGHT_THEME : DARK_THEME;

        this.document.body.classList.add(theme);
        this.document.body.classList.remove(other);

        if (isPlatformBrowser(this.platformId)) {
            const overlayClasses = this.overlay.getContainerElement().classList;
            overlayClasses.add(theme);
            overlayClasses.remove(other);
        }

        this.currentTheme.set(theme);
    }

    private savePreference(theme: string): void {
        try {
            this.storage.setItem(THEME_STORAGE_KEY, theme);
        } catch (e) {
            console.warn('Failed to save theme preference:', e);
        }
    }
}
