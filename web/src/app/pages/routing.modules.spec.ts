import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { routes } from './routing.modules';
import { EmailVerificationComponent } from './email-verification/email-verification.component';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';

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
