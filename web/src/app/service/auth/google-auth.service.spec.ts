import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { LOCALE_ID, PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { GoogleAuthService } from './google-auth.service';
import { AuthService } from './auth.service';
import { PUBLIC_AUTH } from 'src/app/types/auth/email-auth';
import { environment } from 'src/environments/environment';

describe('GoogleAuthService', () => {
    let service: GoogleAuthService;
    let http: HttpTestingController;
    const proof = { challengeId: 'challenge', credential: 'signed-google-token' };
    const originalClientId = environment.googleClientId;
    let previousGoogle: unknown;
    beforeEach(() => {
        environment.googleClientId = 'test.apps.googleusercontent.com';
        previousGoogle = (window as any).google;
        TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
        service = TestBed.inject(GoogleAuthService);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        environment.googleClientId = originalClientId;
        (window as any).google = previousGoogle;
        document.querySelectorAll('script[src^="https://accounts.google.com/gsi/client"]').forEach(script => script.remove());
        http.verify();
    });
    it('requests a browser-bound nonce and emits only the credential for that attempt', async () => {
        const initialize = jasmine.createSpy('initialize');
        const renderButton = jasmine.createSpy('renderButton');
        (window as any).google = { accounts: { id: { initialize, renderButton } } };
        const callback = jasmine.createSpy('callback');
        const element = document.createElement('div');
        const rendered = service.render(element, 'SignIn', true, callback, () => true);
        await Promise.resolve();
        const request = http.expectOne(environment.api + '/api/user/auth/google/challenge');
        expect(request.request.withCredentials).toBeTrue();
        expect(request.request.context.get(PUBLIC_AUTH)).toBeTrue();
        request.flush({ challengeId: 'challenge', nonce: 'server-nonce', expiresAt: 'future' });
        const view = await rendered;
        expect(initialize.calls.mostRecent().args[0].nonce).toBe('server-nonce');
        initialize.calls.mostRecent().args[0].callback({ credential: 'signed-google-token' });
        expect(callback).toHaveBeenCalledWith(proof);
        const light = renderButton.calls.argsFor(0)[0] as HTMLElement;
        const dark = renderButton.calls.argsFor(1)[0] as HTMLElement;
        expect(renderButton.calls.argsFor(0)[1].theme).toBe('outline');
        expect(renderButton.calls.argsFor(1)[1].theme).toBe('outline_dark');
        expect(light.style.visibility).toBe('hidden');
        expect(light.inert).toBeTrue();
        expect(light.getAttribute('aria-hidden')).toBe('true');
        expect(dark.style.visibility).toBe('visible');
        view.updateTheme(false);
        expect(light.style.visibility).toBe('visible');
        expect(light.inert).toBeFalse();
        expect(light.hasAttribute('aria-hidden')).toBeFalse();
        expect(dark.style.visibility).toBe('hidden');
        expect(dark.inert).toBeTrue();
        view.updateTheme(true);
        expect(dark.style.visibility).toBe('visible');
        expect(dark.inert).toBeFalse();
        expect(element.contains(light)).toBeTrue();
        expect(element.contains(dark)).toBeTrue();
        expect(renderButton).toHaveBeenCalledTimes(2);
        expect(initialize).toHaveBeenCalledTimes(1);
        http.expectNone(environment.api + '/api/user/auth/google/challenge');
        initialize.calls.mostRecent().args[0].callback({ credential: 'signed-google-token' });
        expect(callback).toHaveBeenCalledWith(proof);
    });
    it('ignores credentials returned to a destroyed or superseded page', async () => {
        const initialize = jasmine.createSpy('initialize');
        const renderButton = jasmine.createSpy('renderButton');
        (window as any).google = { accounts: { id: { initialize, renderButton } } };
        let active = true;
        const callback = jasmine.createSpy('callback');
        const element = document.createElement('div');
        const rendering = service.render(element, 'Link', false, callback, () => active);
        await Promise.resolve();
        const request = http.expectOne(environment.api + '/api/user/auth/google/challenge');
        expect(request.request.context.get(PUBLIC_AUTH)).toBeFalse();
        request.flush({ challengeId: 'challenge', nonce: 'nonce', expiresAt: 'future' });
        const view = await rendering;
        active = false;
        view.updateTheme(true);
        expect(renderButton).toHaveBeenCalledTimes(2);
        expect((renderButton.calls.argsFor(0)[0] as HTMLElement).style.visibility).toBe('visible');
        initialize.calls.mostRecent().args[0].callback({ credential: 'stale' });
        expect(callback).not.toHaveBeenCalled();
    });

    for (const [locale, expected] of [['en-US', 'en'], ['pt-BR', 'pt-BR'], ['es-ES', 'es']]) {
        it(`loads the Google library and both buttons in the page language ${locale}`, async () => {
            TestBed.resetTestingModule();
            TestBed.configureTestingModule({ imports: [HttpClientTestingModule],
                providers: [{ provide: LOCALE_ID, useValue: locale }] });
            service = TestBed.inject(GoogleAuthService);
            http = TestBed.inject(HttpTestingController);
            (window as any).google = undefined;
            let script!: HTMLScriptElement;
            const append = document.head.appendChild.bind(document.head);
            spyOn(document.head, 'appendChild').and.callFake(<T extends Node>(node: T): T => {
                if (node instanceof HTMLScriptElement && node.src.startsWith('https://accounts.google.com/gsi/client')) {
                    script = node;
                    return node;
                }
                return append(node);
            });
            const renderButton = jasmine.createSpy('renderButton');
            const rendering = service.render(document.createElement('div'), 'SignIn', false, () => undefined, () => true);
            const language = new URL(script.src).searchParams.get('hl');
            (window as any).google = { accounts: { id: { initialize: () => undefined, renderButton } } };
            script.onload!(new Event('load'));
            await Promise.resolve();
            http.expectOne(environment.api + '/api/user/auth/google/challenge')
                .flush({ challengeId: 'id', nonce: 'nonce', expiresAt: 'future' });
            await rendering;
            expect(language).toBe(expected);
            expect(renderButton).toHaveBeenCalledTimes(2);
            for (const args of renderButton.calls.allArgs()) {
                expect(args[1]).toEqual(jasmine.objectContaining({ locale: expected,
                    type: 'standard', size: 'large', text: 'continue_with' }));
            }
        });
    }
    it('stores application tokens only after successful Google sign-in', async () => {
        const auth = TestBed.inject(AuthService);
        spyOn(auth, 'setTokens');
        const pending = service.signin(proof);
        const request = http.expectOne(environment.api + '/api/user/auth/google');
        expect(request.request.body).toEqual(proof);
        expect(request.request.context.get(PUBLIC_AUTH)).toBeTrue();
        const tokens = { authenticated: true, accessToken: 'application-token', refreshToken: 'application-refresh' };
        request.flush(tokens);
        await pending;
        expect(auth.setTokens).toHaveBeenCalledWith(tokens as any);
    });
    it('keeps authenticated proof operations out of public auth context', async () => {
        const linked = service.link(proof);
        const link = http.expectOne(environment.api + '/api/user/auth/google/link');
        expect(link.request.context.get(PUBLIC_AUTH)).toBeFalse();
        expect(link.request.withCredentials).toBeTrue();
        link.flush(null);
        await linked;
        const created = service.createPassword(proof, 'password123');
        const password = http.expectOne(environment.api + '/api/user/auth/password');
        expect(password.request.body).toEqual({ ...proof, newPassword: 'password123' });
        password.flush(null);
        await created;
    });
    it('propagates HTTP failures without saving a session', async () => {
        const auth = TestBed.inject(AuthService);
        spyOn(auth, 'setTokens');
        const pending = service.signin(proof);
        const result = expectAsync(pending).toBeRejected();
        http.expectOne(environment.api + '/api/user/auth/google').flush({ code: 'google_link_required' }, { status: 409, statusText: 'Conflict' });
        await result;
        expect(auth.setTokens).not.toHaveBeenCalled();
    });
    it('maps known outcomes and does not display arbitrary provider error text', () => {
        expect(service.message(new HttpErrorResponse({ error: { code: 'google_registration_required' } }))).toContain('traditional registration');
        expect(service.message(new HttpErrorResponse({ error: { code: 'google_link_required' } }))).toContain('existing account');
        expect(service.message(new HttpErrorResponse({ error: { message: 'provider-secret' } }))).not.toContain('provider-secret');
    });
    it('rejects rendering on the server without loading a script or requesting a challenge', async () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [{ provide: PLATFORM_ID, useValue: 'server' }] });
        service = TestBed.inject(GoogleAuthService);
        http = TestBed.inject(HttpTestingController);
        await expectAsync(service.render(document.createElement('div'), 'SignIn', false, () => undefined, () => true)).toBeRejected();
        http.expectNone(environment.api + '/api/user/auth/google/challenge');
    });

    it('propagates linking and first-password failures so the caller can require a fresh proof', async () => {
        const linked = service.link(proof);
        const rejectedLink = expectAsync(linked).toBeRejected();
        http.expectOne(environment.api + '/api/user/auth/google/link').flush(null, { status: 401, statusText: 'Unauthorized' });
        await rejectedLink;
        const created = service.createPassword(proof, 'password123');
        const rejectedPassword = expectAsync(created).toBeRejected();
        http.expectOne(environment.api + '/api/user/auth/password').flush(null, { status: 409, statusText: 'Conflict' });
        await rejectedPassword;
    });

});
