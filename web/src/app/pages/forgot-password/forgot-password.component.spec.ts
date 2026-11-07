import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';

describe('ForgotPasswordComponent', () => {
    let component: ForgotPasswordComponent;
    let auth: jasmine.SpyObj<AuthService>;
    let router: Router;
    beforeEach(() => {
        auth = jasmine.createSpyObj('AuthService', ['requestPasswordRecovery']);
        auth.requestPasswordRecovery.and.resolveTo();
        TestBed.configureTestingModule({ imports: [ForgotPasswordComponent], providers: [provideRouter([]), provideNoopAnimations(),
            { provide: AuthService, useValue: auth },
            { provide: SESSION_STORAGE, useValue: { getItem: () => null, setItem: () => undefined } },
        ] });
        router = TestBed.inject(Router); spyOn(router, 'navigate').and.resolveTo(true);
        const fixture = TestBed.createComponent(ForgotPasswordComponent);
        component = fixture.componentInstance; fixture.detectChanges();
    });
    it('requests only a valid trimmed email and moves to reset with context and wait', async () => {
        await component.handleSubmit();
        expect(auth.requestPasswordRecovery).not.toHaveBeenCalled();
        component.form.controls.email.setValue(' p@example.com ');
        await component.handleSubmit();
        expect(auth.requestPasswordRecovery).toHaveBeenCalledOnceWith({ email: 'p@example.com' });
        expect(router.navigate).toHaveBeenCalledWith(['/reset-password']);
        expect(TestBed.inject(EmailAuthFlowService).context('recovery').requested).toBeTrue();
        expect(TestBed.inject(EmailAuthFlowService).remaining('recovery')).toBe(60);
    });
    it('preserves email on server failure and never claims delivery', async () => {
        component.form.controls.email.setValue('p@example.com');
        auth.requestPasswordRecovery.and.rejectWith(new HttpErrorResponse({ status: 500 }));
        await component.handleSubmit();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(component.form.controls.email.value).toBe('p@example.com');
        expect(component.error()).toContain('Something went wrong');
    });
    it('blocks requests during cooldown, including after a throttle', async () => {
        component.form.controls.email.setValue('p@example.com');
        auth.requestPasswordRecovery.and.rejectWith(new HttpErrorResponse({ status: 429 }));
        await component.handleSubmit();
        await component.handleSubmit();
        expect(auth.requestPasswordRecovery).toHaveBeenCalledTimes(1);
        expect(component.resendWait()).toBe(60);
    });
});
