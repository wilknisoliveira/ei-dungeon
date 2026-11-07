import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { InjectionToken, PLATFORM_ID, inject } from '@angular/core';

declare const SITE_URL: string | undefined;

const LOCAL_HOSTS = new Set(['localhost', '127.0.0.1', '[::1]']);

export function normalizeSiteUrl(value: string): string {
    const raw = value.trim();
    if (!raw) {
        throw new Error('SITE_URL is required for prerendering.');
    }

    let url: URL;
    try {
        url = new URL(raw);
    } catch {
        throw new Error('SITE_URL must be an absolute URL.');
    }

    const isLocal = LOCAL_HOSTS.has(url.hostname);
    if (url.protocol !== 'https:' && !(isLocal && url.protocol === 'http:')) {
        throw new Error('SITE_URL must use HTTPS outside local development.');
    }

    if (
        url.username ||
        url.password ||
        url.pathname !== '/' ||
        url.search ||
        url.hash
    ) {
        throw new Error('SITE_URL must contain only the site origin.');
    }

    return url.origin;
}

export const SITE_ORIGIN = new InjectionToken<string>('SITE_ORIGIN', {
    providedIn: 'root',
    factory: () => {
        const document = inject(DOCUMENT);
        const platformId = inject(PLATFORM_ID);
        const defined =
            typeof SITE_URL === 'string' && SITE_URL ? SITE_URL : undefined;

        if (defined) {
            return normalizeSiteUrl(defined);
        }

        if (isPlatformBrowser(platformId)) {
            return normalizeSiteUrl(document.location.origin);
        }

        throw new Error('SITE_URL is required for prerendering.');
    },
});
