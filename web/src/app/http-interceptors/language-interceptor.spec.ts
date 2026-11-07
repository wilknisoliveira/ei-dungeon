import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { LocaleService } from 'src/app/core/services/locale.service';
import { LOCAL_STORAGE } from 'src/app/core/storage/app-storage';
import { AuthService } from 'src/app/service/auth/auth.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { environment } from 'src/environments/environment';
import { authInterceptor } from './auth-interceptor';
import { languageInterceptor } from './language-interceptor';

function configure(locale: string): void {
    TestBed.configureTestingModule({ providers: [
        { provide: LOCALE_ID, useValue: locale },
        provideRouter([]),
        provideHttpClient(withInterceptors([languageInterceptor, authInterceptor])),
        provideHttpClientTesting(),
        { provide: LOCAL_STORAGE, useValue: {
            // A saved preference can differ from the active, explicitly opened locale.
            getItem: (key: string) => key === 'preferredLanguage' ? 'es' : null,
            setItem: () => undefined,
            removeItem: () => undefined,
        } },
        { provide: SnackbarService, useValue: { addError: jasmine.createSpy('addError') } },
    ] });
}

describe('languageInterceptor', () => {
    for (const language of ['en', 'pt-BR', 'es']) {
        describe(language, () => {
            let auth: AuthService;
            let controller: HttpTestingController;
            beforeEach(() => {
                configure(language);
                auth = TestBed.inject(AuthService);
                controller = TestBed.inject(HttpTestingController);
            });
            afterEach(() => controller.verify());

            it('sets the active language on signup, both requests and both confirmations without changing contracts', async () => {
                const registration = { userName: 'player', fullName: 'Player Name', email: 'player@example.com', password: 'password123' };
                const email = { email: registration.email };
                const confirm = { ...email, code: '000123' };
                const recovery = { ...confirm, newPassword: 'replacement' };
                const calls = [
                    { path: '/api/user', send: () => auth.signup(registration), body: registration, status: 200 },
                    { path: '/api/user/auth/email-verification', send: () => auth.requestEmailVerification(email), body: email, status: 202 },
                    { path: '/api/user/auth/password-recovery', send: () => auth.requestPasswordRecovery(email), body: email, status: 202 },
                    { path: '/api/user/auth/email-verification/confirm', send: () => auth.confirmEmailVerification(confirm), body: confirm, status: 204 },
                    { path: '/api/user/auth/password-recovery/confirm', send: () => auth.confirmPasswordRecovery(recovery), body: recovery, status: 204 },
                ];
                for (const call of calls) {
                    const result = call.send();
                    const req = controller.expectOne(environment.api + call.path);
                    expect(req.request.headers.get('Accept-Language')).toBe(language);
                    expect(req.request.headers.get('Content-Type')).toBe('application/json');
                    expect(req.request.headers.has('Authorization')).toBeFalse();
                    expect(req.request.body).toEqual(call.body);
                    expect(req.request.params.keys()).toEqual([]);
                    req.flush(call.status === 200 ? { userName: 'player', fullName: 'Player Name', email: email.email } : null,
                        { status: call.status, statusText: 'Success' });
                    await result;
                }
            });
        });
    }

    for (const [locale, expected] of [['en-US', 'en'], ['es-ES', 'es'], ['fr-FR', 'en'], ['', 'en']]) {
        it('normalizes locale ' + (locale || '(empty)') + ' to ' + expected, async () => {
            configure(locale);
            const auth = TestBed.inject(AuthService);
            const controller = TestBed.inject(HttpTestingController);
            const result = auth.requestEmailVerification({ email: 'player@example.com' });
            const req = controller.expectOne(environment.api + '/api/user/auth/email-verification');
            expect(req.request.headers.get('Accept-Language')).toBe(expected);
            req.flush(null, { status: 202, statusText: 'Accepted' });
            await result;
            controller.verify();
        });
    }

    it('reads the language on each request, so a resend uses the latest selection', async () => {
        configure('en');
        const locale = TestBed.inject(LocaleService);
        const current = spyOn(locale, 'getCurrentLanguage').and.returnValue('pt-BR');
        const auth = TestBed.inject(AuthService);
        const controller = TestBed.inject(HttpTestingController);
        for (const language of ['pt-BR', 'es']) {
            current.and.returnValue(language);
            const result = auth.requestEmailVerification({ email: 'player@example.com' });
            const req = controller.expectOne(environment.api + '/api/user/auth/email-verification');
            expect(req.request.headers.get('Accept-Language')).toBe(language);
            req.flush(null, { status: 202, statusText: 'Accepted' });
            await result;
        }
        controller.verify();
    });

    it('does not attach the application language to assets or other origins and similar URL prefixes', () => {
        configure('pt-BR');
        const http = TestBed.inject(HttpClient);
        const controller = TestBed.inject(HttpTestingController);
        for (const url of ['/assets/logo_completed.webp', 'https://example.org/api/user', environment.api + '/api-other/user']) {
            http.get(url).subscribe();
            const req = controller.expectOne(url);
            expect(req.request.headers.has('Accept-Language')).toBeFalse();
            req.flush({});
        }
        controller.verify();
    });

    it('uses the application selection instead of a conflicting request header', () => {
        configure('pt-BR');
        const http = TestBed.inject(HttpClient);
        const controller = TestBed.inject(HttpTestingController);
        http.get(environment.api + '/api/user', { headers: { 'Accept-Language': 'es' } }).subscribe();
        const req = controller.expectOne(environment.api + '/api/user');
        expect(req.request.headers.get('Accept-Language')).toBe('pt-BR');
        req.flush({});
        controller.verify();
    });
});
