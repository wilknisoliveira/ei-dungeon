import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { EmailVerificationComponent } from './email-verification.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';

describe('EmailVerificationComponent', () => {
    let component: EmailVerificationComponent;
    let auth: jasmine.SpyObj<AuthService>;
    let router: Router;
    beforeEach(() => {
        auth = jasmine.createSpyObj('AuthService', ['requestEmailVerification', 'confirmEmailVerification']);
        auth.requestEmailVerification.and.resolveTo();
        auth.confirmEmailVerification.and.resolveTo();
        TestBed.configureTestingModule({ imports: [EmailVerificationComponent], providers: [provideRouter([]), provideNoopAnimations(),
            { provide: AuthService, useValue: auth },
            { provide: SESSION_STORAGE, useValue: { getItem: () => null, setItem: () => undefined } },
            { provide: SnackbarService, useValue: { addSuccess: jasmine.createSpy('success') } },
        ] });
        router = TestBed.inject(Router);
        spyOn(router, 'navigate').and.resolveTo(true);
        const fixture = TestBed.createComponent(EmailVerificationComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });
    it('opens without requesting automatically and only requests after email is valid', async () => {
        expect(auth.requestEmailVerification).not.toHaveBeenCalled();
        await component.handleRequest();
        expect(auth.requestEmailVerification).not.toHaveBeenCalled();
        component.form.controls.email.setValue('p@example.com');
        await component.handleRequest();
        expect(auth.requestEmailVerification).toHaveBeenCalledOnceWith({ email: 'p@example.com' });
        await component.handleRequest();
        expect(auth.requestEmailVerification).toHaveBeenCalledTimes(1);
        expect(component.status()).toContain('If this account needs verification');
    });
    it('keeps leading zeroes and prevents post-success reuse', async () => {
        component.form.setValue({ email: 'p@example.com', code: '000123' });
        await component.handleSubmit();
        expect(auth.confirmEmailVerification).toHaveBeenCalledOnceWith({ email: 'p@example.com', code: '000123' });
        expect(router.navigate).toHaveBeenCalledWith(['/login']);
        await component.handleSubmit();
        expect(auth.confirmEmailVerification).toHaveBeenCalledTimes(1);
    });
    it('clears the code on email edit without removing cooldown', () => {
        const flow = TestBed.inject(EmailAuthFlowService);
        flow.requested('verification');
        component.form.controls.code.setValue('000123');
        component.form.controls.email.setValue('other@example.com');
        expect(component.form.controls.code.value).toBe('');
        expect(flow.remaining('verification')).toBe(60);
    });
    it('maps validation, keeps input and handles a lost confirmation response', async () => {
        component.form.setValue({ email: 'p@example.com', code: '000123' });
        auth.confirmEmailVerification.and.rejectWith(new HttpErrorResponse({ status: 400, error: { errors: { Code: ['internal'] } } }));
        await component.handleSubmit();
        expect(component.form.controls.code.hasError('server')).toBeTrue();
        expect(component.form.controls.code.value).toBe('000123');
        component.form.controls.code.setValue('000124');
        auth.confirmEmailVerification.and.rejectWith(new HttpErrorResponse({ status: 0 }));
        await component.handleSubmit();
        expect(component.error()).toContain('could not confirm the result');
    });
    it('blocks duplicates while pending and throttles confirmation separately from resend', async () => {
        let resolve!: () => void;
        auth.confirmEmailVerification.and.returnValue(new Promise<void>(r => resolve = r));
        component.form.setValue({ email: 'p@example.com', code: '000123' });
        const first = component.handleSubmit();
        await component.handleSubmit();
        expect(auth.confirmEmailVerification).toHaveBeenCalledTimes(1);
        resolve(); await first;
    });
    it('honors empty 429 responses and keeps the form recoverable', async () => {
        component.form.setValue({ email: 'p@example.com', code: '000123' });
        auth.confirmEmailVerification.and.rejectWith(new HttpErrorResponse({ status: 429, headers: new HttpHeaders({ 'Retry-After': '80' }) }));
        await component.handleSubmit();
        expect(component.confirmWait()).toBe(80);
        expect(component.resendWait()).toBe(0);
        await component.handleSubmit();
        expect(auth.confirmEmailVerification).toHaveBeenCalledTimes(1);
    });
});
