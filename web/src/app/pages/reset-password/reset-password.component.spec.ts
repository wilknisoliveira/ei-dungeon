import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';

describe('ResetPasswordComponent', () => {
    let component: ResetPasswordComponent;
    let fixture: ComponentFixture<ResetPasswordComponent>;
    let auth: jasmine.SpyObj<AuthService>;
    let router: Router;
    const valid = { email: 'p@example.com', code: '000123', newPassword: 'abcd', confirmPassword: 'abcd' };
    beforeEach(() => {
        auth = jasmine.createSpyObj('AuthService', ['requestPasswordRecovery', 'confirmPasswordRecovery', 'logout']);
        auth.requestPasswordRecovery.and.resolveTo(); auth.confirmPasswordRecovery.and.resolveTo();
        TestBed.configureTestingModule({ imports: [ResetPasswordComponent], providers: [provideRouter([]), provideNoopAnimations(),
            { provide: AuthService, useValue: auth },
            { provide: SESSION_STORAGE, useValue: { getItem: () => null, setItem: () => undefined } },
            { provide: SnackbarService, useValue: { addSuccess: jasmine.createSpy('success') } },
        ] });
        router = TestBed.inject(Router); spyOn(router, 'navigate').and.resolveTo(true);
        fixture = TestBed.createComponent(ResetPasswordComponent);
        component = fixture.componentInstance; fixture.detectChanges();
    });
    it('submits only email/code/newPassword and clears session and secrets on success', async () => {
        component.form.setValue(valid);
        await component.handleSubmit();
        expect(auth.confirmPasswordRecovery).toHaveBeenCalledOnceWith({ email: valid.email, code: '000123', newPassword: 'abcd' });
        expect(auth.logout).toHaveBeenCalledTimes(1);
        expect(router.navigate).toHaveBeenCalledWith(['/login']);
        expect(component.form.controls.newPassword.value).toBe('');
        await component.handleSubmit();
        expect(auth.confirmPasswordRecovery).toHaveBeenCalledTimes(1);
    });
    it('rejects mismatches, short/long passwords, and malformed codes without API calls', async () => {
        for (const values of [ { ...valid, confirmPassword: 'other' }, { ...valid, newPassword: 'abc', confirmPassword: 'abc' },
            { ...valid, newPassword: 'x'.repeat(51), confirmPassword: 'x'.repeat(51) }, { ...valid, code: '１２３４５６' } ]) {
            component.form.setValue(values); await component.handleSubmit();
        }
        expect(auth.confirmPasswordRecovery).not.toHaveBeenCalled();
    });
    it('supports direct entry and explicitly requests a recovery code, never verification', async () => {
        expect(component.status()).toBe('');
        expect(auth.requestPasswordRecovery).not.toHaveBeenCalled();
        component.form.controls.email.setValue('p@example.com');
        await component.handleRequest();
        expect(auth.requestPasswordRecovery).toHaveBeenCalledOnceWith({ email: 'p@example.com' });
        expect(component.status()).toContain('If a verified account exists');
        expect(component.resendWait()).toBe(60);
    });
    it('preserves input on invalid-code or uncertain network results without logging out', async () => {
        component.form.setValue(valid);
        auth.confirmPasswordRecovery.and.rejectWith(new HttpErrorResponse({ status: 400, error: { code: 'invalid_verification_code' } }));
        await component.handleSubmit();
        expect(component.error()).toContain('could not be confirmed');
        expect(component.form.getRawValue()).toEqual(valid);
        auth.confirmPasswordRecovery.and.rejectWith(new HttpErrorResponse({ status: 0 }));
        await component.handleSubmit();
        expect(component.error()).toContain('Try logging in');
        expect(auth.logout).not.toHaveBeenCalled();
    });
    it('maps server password validation and retains the local cooldown after email edit', async () => {
        component.form.setValue(valid);
        auth.confirmPasswordRecovery.and.rejectWith(new HttpErrorResponse({ status: 400, error: { errors: { NewPassword: ['internal'] } } }));
        await component.handleSubmit();
        expect(component.form.controls.newPassword.hasError('server')).toBeTrue();
        TestBed.inject(EmailAuthFlowService).requested('recovery');
        component.form.controls.email.setValue('other@example.com');
        expect(component.form.controls.code.value).toBe('');
        expect(TestBed.inject(EmailAuthFlowService).remaining('recovery')).toBe(60);
    });
    it('blocks duplicate confirmation requests while pending', async () => {
        let resolve!: () => void;
        auth.confirmPasswordRecovery.and.returnValue(new Promise<void>(r => resolve = r));
        component.form.setValue(valid);
        const first = component.handleSubmit();
        await component.handleSubmit();
        expect(auth.confirmPasswordRecovery).toHaveBeenCalledTimes(1);
        resolve(); await first;
    });

    it('toggles each password independently through a suffix icon without submitting or changing values', () => {
        component.form.setValue(valid);
        fixture.detectChanges();
        const root: HTMLElement = fixture.nativeElement;
        const password = root.querySelector<HTMLInputElement>('[formControlName="newPassword"]')!;
        const confirmation = root.querySelector<HTMLInputElement>('[formControlName="confirmPassword"]')!;
        const toggle = root.querySelector<HTMLButtonElement>('button[aria-label="Show new password"]')!;
        const repeatToggle = root.querySelector<HTMLButtonElement>('button[aria-label="Show repeated password"]')!;
        expect(password.type).toBe('password');
        expect(confirmation.type).toBe('password');
        expect(toggle.closest('mat-form-field')).toBe(password.closest('mat-form-field'));
        expect(toggle.type).toBe('button');
        expect(toggle.getAttribute('aria-pressed')).toBe('false');
        expect(toggle.querySelector('mat-icon')?.textContent?.trim()).toBe('visibility');
        toggle.click(); fixture.detectChanges();
        expect(password.type).toBe('text');
        expect(confirmation.type).toBe('password');
        expect(toggle.getAttribute('aria-pressed')).toBe('true');
        expect(toggle.querySelector('mat-icon')?.textContent?.trim()).toBe('visibility_off');
        repeatToggle.click(); fixture.detectChanges();
        expect(confirmation.type).toBe('text');
        toggle.click(); fixture.detectChanges();
        expect(password.type).toBe('password');
        expect(confirmation.type).toBe('text');
        expect(component.form.getRawValue()).toEqual(valid);
        expect(auth.confirmPasswordRecovery).not.toHaveBeenCalled();
        expect(auth.requestPasswordRecovery).not.toHaveBeenCalled();
    });

    it('prevents password visibility changes while the form is busy', () => {
        component.busy.set(true); fixture.detectChanges();
        const toggle: HTMLButtonElement = fixture.nativeElement.querySelector('button[aria-label="Show new password"]');
        toggle.click(); fixture.detectChanges();
        expect(component.hidePassword()).toBeTrue();
    });
});
