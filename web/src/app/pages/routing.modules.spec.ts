import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { routes } from './routing.modules';
import { EmailVerificationComponent } from './email-verification/email-verification.component';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { Component } from '@angular/core';
import { AuthService } from 'src/app/service/auth/auth.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { AuthGuard } from 'src/app/guards/auth.guard';
import { HasRoleGuard } from 'src/app/guards/has-role.guard';

@Component({ standalone: true, template: '<p>Route destination</p>' })
class RouteDestinationComponent {}

describe('Public email routes', () => {
    beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter(routes), provideHttpClient(), provideNoopAnimations(),
        { provide: SESSION_STORAGE, useValue: { getItem: () => null, setItem: () => undefined } },
    ] }));
    it('opens verification directly without an authenticated session', async () => {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/email-verification', EmailVerificationComponent);
        expect(harness.routeNativeElement?.textContent).toContain('Verify your email');
        expect(harness.routeNativeElement?.querySelector('meta[name="robots"]')).toBeNull();
    });

    it('opens both recovery pages directly without a session', async () => {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/forgot-password', ForgotPasswordComponent);
        expect(harness.routeNativeElement?.textContent).toContain('Forgot your password?');
        await harness.navigateByUrl('/reset-password', ResetPasswordComponent);
        expect(harness.routeNativeElement?.textContent).toContain('Reset your password');
    });
});

describe('Page loading and access policy', () => {
    it('defines one lazy loader per page without direct component references', () => {
        expect(routes.map(route => route.path)).toEqual([
            'login', 'forgot-password', 'reset-password', 'email-verification',
            'signup', 'settings', 'home', 'support-tickets', 'admin', '',
        ]);
        for (const route of routes) {
            expect(route.component).toBeUndefined();
            expect(typeof route.loadComponent).toBe('function');
        }
        expect(routes.find(route => route.path === '')?.pathMatch).toBe('full');
    });

    it('guards the lazy Guild Mail page for signed-in player roles and administrators', () => {
        const support = routes.find(route => route.path === 'support-tickets');
        expect(support?.canActivate).toEqual([AuthGuard, HasRoleGuard]);
        expect(support?.data?.['roles']).toEqual(['Admin', 'CommonUser', 'PremiumUser']);
    });

    it('keeps only the private conversation ID when a signed-out visitor opens its direct link', async () => {
        const ticket = '3617ec15-fae5-4cfc-a613-ddc845970b80';
        const auth = jasmine.createSpyObj<AuthService>('AuthService', ['isUserLoggedIn']);
        auth.isUserLoggedIn.and.returnValue(false);
        TestBed.configureTestingModule({ providers: [
            provideRouter([
                { path: 'support-tickets', component: RouteDestinationComponent, canActivate: [AuthGuard] },
                { path: 'login', component: RouteDestinationComponent, canActivate: [AuthGuard] },
            ]),
            { provide: AuthService, useValue: auth },
        ] });

        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(`/support-tickets?ticket=${ticket}&other=data`);

        expect(TestBed.inject(Router).url).toBe(`/login?ticket=${ticket}`);
    });

    it('preserves existing public routes and Home/Settings role policy', () => {
        for (const path of ['', 'signup', 'email-verification', 'forgot-password', 'reset-password']) {
            expect(routes.find(route => route.path === path)?.canActivate).toBeUndefined();
        }
        expect(routes.find(route => route.path === 'login')?.canActivate).toEqual([AuthGuard]);
        for (const path of ['home', 'settings']) {
            expect(routes.find(route => route.path === path)?.canActivate).toEqual([AuthGuard, HasRoleGuard]);
            expect(routes.find(route => route.path === path)?.data?.['roles']).toEqual(['Admin', 'CommonUser']);
        }
    });

    for (const role of ['Admin', 'CommonUser', 'PremiumUser', 'anonymous']) {
        it(`allows only Admin to activate the admin page (${role})`, async () => {
            const admin = routes.find(route => route.path === 'admin')!;
            const loadAdmin = jasmine.createSpy('loadAdmin').and.callFake(async () => RouteDestinationComponent);
            const auth = jasmine.createSpyObj<AuthService>('AuthService', ['getRole', 'isUserLoggedIn']);
            auth.getRole.and.returnValue(role);
            auth.isUserLoggedIn.and.returnValue(role !== 'anonymous');
            TestBed.configureTestingModule({
                providers: [
                    provideRouter([
                        { ...admin, loadComponent: loadAdmin },
                        { path: 'home', component: RouteDestinationComponent },
                        { path: 'login', component: RouteDestinationComponent },
                    ]),
                    { provide: AuthService, useValue: auth },
                    { provide: SnackbarService, useValue: jasmine.createSpyObj('SnackbarService', ['addError']) },
                ],
            });
            expect(admin.data?.['roles']).toEqual(['Admin']);
            const harness = await RouterTestingHarness.create();
            await harness.navigateByUrl('/admin');
            await harness.fixture.whenStable();
            if (role === 'Admin') {
                expect(loadAdmin).toHaveBeenCalledTimes(1);
                expect(harness.routeNativeElement?.textContent).toContain('Route destination');
            } else {
                expect(loadAdmin).not.toHaveBeenCalled();
            }
        });
    }
});
