import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LOCAL_STORAGE, SESSION_STORAGE } from './app-storage';

describe('app storage providers', () => {
    afterEach(() => {
        localStorage.removeItem('local-test');
        sessionStorage.removeItem('session-test');
        TestBed.resetTestingModule();
    });

    it('should expose browser local storage', () => {
        TestBed.configureTestingModule({});
        const storage = TestBed.inject(LOCAL_STORAGE);

        storage.setItem('local-test', 'value');

        expect(localStorage.getItem('local-test')).toBe('value');
    });

    it('should expose browser session storage', () => {
        TestBed.configureTestingModule({});
        const storage = TestBed.inject(SESSION_STORAGE);

        storage.setItem('session-test', 'value');

        expect(sessionStorage.getItem('session-test')).toBe('value');
    });

    it('should use no-op storage on the server', () => {
        TestBed.configureTestingModule({
            providers: [{ provide: PLATFORM_ID, useValue: 'server' }],
        });
        const storage = TestBed.inject(LOCAL_STORAGE);

        storage.setItem('local-test', 'value');

        expect(storage.getItem('local-test')).toBeNull();
        expect(localStorage.getItem('local-test')).toBeNull();
    });
});
