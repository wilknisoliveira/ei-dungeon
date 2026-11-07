import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from 'src/app/service/auth/auth.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { LOCAL_STORAGE } from 'src/app/core/storage/app-storage';
import { environment } from 'src/environments/environment';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
    let auth: AuthService;
    let http: HttpClient;
    let controller: HttpTestingController;
    let values: Map<string, string>;
    let router: Router;
    beforeEach(() => {
        values = new Map([['tokenInfo', JSON.stringify({ accessToken: 'old-token-string', refreshToken: 'refresh' })]]);
        TestBed.configureTestingModule({ providers: [
            provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(),
            { provide: LOCAL_STORAGE, useValue: {
                getItem: (key: string) => values.get(key) ?? null,
                setItem: (key: string, value: string) => values.set(key, value),
                removeItem: (key: string) => values.delete(key),
            } },
            { provide: SnackbarService, useValue: { addError: jasmine.createSpy('addError') } },
        ] });
        auth = TestBed.inject(AuthService);
        http = TestBed.inject(HttpClient);
        controller = TestBed.inject(HttpTestingController);
        router = TestBed.inject(Router);
        spyOn(router, 'navigate').and.resolveTo(true);
    });
    afterEach(() => controller.verify());
    it('preserves public login failures without Authorization or refresh', async () => {
        const result = auth.login({ userName: 'player', password: 'password' }).catch(e => e);
        const req = controller.expectOne(environment.api + '/api/user/auth/signin');
        expect(req.request.headers.has('Authorization')).toBeFalse();
        req.flush({ code: 'email_verification_required' }, { status: 403, statusText: 'Forbidden' });
        expect((await result).error.code).toBe('email_verification_required');
        expect(router.navigate).not.toHaveBeenCalled();
        const second = auth.login({ userName: 'player', password: 'wrong' }).catch(e => e);
        controller.expectOne(environment.api + '/api/user/auth/signin').flush(null, { status: 401, statusText: 'Unauthorized' });
        expect((await second).status).toBe(401);
        expect(values.has('tokenInfo')).toBeTrue();
    });
    it('shares failed refresh and terminates both concurrent requests', async () => {
        const first = new Promise(resolve => http.get('/private/one').subscribe({ error: resolve }));
        const second = new Promise(resolve => http.get('/private/two').subscribe({ error: resolve }));
        const one = controller.expectOne('/private/one');
        expect(one.request.headers.get('Authorization')).toBe('Bearer old-token-string');
        one.flush(null, { status: 401, statusText: 'Unauthorized' });
        controller.expectOne('/private/two').flush(null, { status: 401, statusText: 'Unauthorized' });
        const refresh = controller.expectOne(environment.api + '/api/user/auth/refresh');
        expect(refresh.request.headers.has('Authorization')).toBeFalse();
        refresh.flush(null, { status: 401, statusText: 'Unauthorized' });
        await Promise.all([first, second]);
        expect(values.has('tokenInfo')).toBeFalse();
        expect(router.navigate).toHaveBeenCalledTimes(1);
    });
    it('retries a protected request once with the refreshed token', async () => {
        const result = new Promise(resolve => http.get('/private').subscribe(resolve));
        controller.expectOne('/private').flush(null, { status: 401, statusText: 'Unauthorized' });
        controller.expectOne(environment.api + '/api/user/auth/refresh').flush({ accessToken: 'new-token', refreshToken: 'new-refresh' });
        await new Promise(resolve => setTimeout(resolve, 0));
        const retry = controller.expectOne('/private');
        expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
        retry.flush({ ok: true });
        expect(await result).toEqual({ ok: true });
    });
});
