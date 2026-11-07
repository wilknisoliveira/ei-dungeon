import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideZonelessChangeDetection } from '@angular/core';

import { AuthComponent } from './auth.component';

describe('AuthComponent', () => {
    let component: AuthComponent;
    let fixture: ComponentFixture<AuthComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideZonelessChangeDetection(), { provide: SESSION_STORAGE, useValue: { getItem: () => null, setItem: () => undefined } }],
            imports: [
                AuthComponent,
                RouterTestingModule,
                HttpClientTestingModule,
                NoopAnimationsModule,
            ],
        });
        fixture = TestBed.createComponent(AuthComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('routes only the specific unverified response to verification without inferring email', async () => {
        const auth = TestBed.inject(AuthService);
        spyOn(auth, 'login').and.rejectWith(new HttpErrorResponse({ status: 403, error: { code: 'email_verification_required' } }));
        const router = TestBed.inject(Router);
        spyOn(router, 'navigate').and.resolveTo(true);
        component.userLogin = { userName: 'player', password: 'password' };
        await component.onSubmit();
        expect(router.navigate).toHaveBeenCalledWith(['/email-verification']);
        expect(TestBed.inject(EmailAuthFlowService).context('verification').email).toBe('');
        expect(component.userLogin.password).toBe('');
    });
    it('keeps incorrect credentials on login with localized error', async () => {
        spyOn(TestBed.inject(AuthService), 'login').and.rejectWith(new HttpErrorResponse({ status: 401 }));
        const router = TestBed.inject(Router);
        spyOn(router, 'navigate').and.resolveTo(true);
        component.userLogin = { userName: 'player', password: 'wrong' };
        await component.onSubmit();
        expect(router.navigate).not.toHaveBeenCalled();
        expect(component.error()).toContain('Invalid username or password');
    });

    it('hides the spinner and renders the error after an asynchronous 401, allowing another attempt', async () => {
        let rejectLogin!: (error: HttpErrorResponse) => void;
        const login = spyOn(TestBed.inject(AuthService), 'login').and.returnValue(
            new Promise((_, reject) => { rejectLogin = reject; }),
        );
        spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
        const onSubmit = spyOn(component, 'onSubmit').and.callThrough();
        fixture.autoDetectChanges();
        component.userLogin = { userName: 'player', password: 'wrong' };
        await fixture.whenStable();

        const submit = () => fixture.nativeElement.querySelector('form')
            .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        submit();
        await fixture.whenStable();
        expect(fixture.nativeElement.querySelector('mat-spinner')).not.toBeNull();
        expect(fixture.nativeElement.querySelector('form')).toBeNull();

        rejectLogin(new HttpErrorResponse({ status: 401 }));
        await onSubmit.calls.mostRecent().returnValue;
        await fixture.whenStable();

        expect(fixture.nativeElement.querySelector('mat-spinner')).toBeNull();
        expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent)
            .toContain('Invalid username or password');
        expect(fixture.nativeElement.querySelector('form')).not.toBeNull();

        login.and.rejectWith(new HttpErrorResponse({ status: 401 }));
        submit();
        await onSubmit.calls.mostRecent().returnValue;
        await fixture.whenStable();
        expect(login).toHaveBeenCalledTimes(2);
    });
});
