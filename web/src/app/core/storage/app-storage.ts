import { isPlatformBrowser } from '@angular/common';
import { InjectionToken, PLATFORM_ID, inject } from '@angular/core';

export interface AppStorage {
    getItem(key: string): string | null;
    setItem(key: string, value: string): void;
    removeItem(key: string): void;
}

const NOOP_STORAGE: AppStorage = {
    getItem: () => null,
    setItem: () => undefined,
    removeItem: () => undefined,
};

function browserStorage(kind: 'localStorage' | 'sessionStorage'): AppStorage {
    const platformId = inject(PLATFORM_ID);
    if (!isPlatformBrowser(platformId)) {
        return NOOP_STORAGE;
    }

    try {
        const storage = window[kind];
        storage.getItem('__storage_probe__');
        return storage;
    } catch {
        return NOOP_STORAGE;
    }
}

export const LOCAL_STORAGE = new InjectionToken<AppStorage>('LOCAL_STORAGE', {
    providedIn: 'root',
    factory: () => browserStorage('localStorage'),
});

export const SESSION_STORAGE = new InjectionToken<AppStorage>(
    'SESSION_STORAGE',
    {
        providedIn: 'root',
        factory: () => browserStorage('sessionStorage'),
    },
);
