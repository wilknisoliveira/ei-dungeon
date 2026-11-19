import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { SignupComponent } from './signup.component';

describe('SignupComponent', () => {
    let component: SignupComponent;
    let fixture: ComponentFixture<SignupComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [{ provide: SESSION_STORAGE, useValue: { getItem: () => null, setItem: () => undefined } }],
            imports: [
                SignupComponent,
                RouterTestingModule,
                HttpClientTestingModule,
                FormsModule,
                ReactiveFormsModule,
                MatFormFieldModule,
                MatInputModule,
                NoopAnimationsModule,
            ],
        });
        fixture = TestBed.createComponent(SignupComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('routes to verification with returned context and no immediate duplicate request', async () => {
        const auth = TestBed.inject(AuthService);
        spyOn(auth, 'signup').and.resolveTo({ email: 'p@example.com' });
        spyOn(auth, 'requestEmailVerification');
        const router = TestBed.inject(Router);
        spyOn(router, 'navigate').and.resolveTo(true);
        component.formGroup.setValue({ email: 'p@example.com', password: 'password123', confirmPassword: 'password123' });
        await component.onSubmit();
        expect(router.navigate).toHaveBeenCalledWith(['/email-verification']);
        expect(TestBed.inject(EmailAuthFlowService).context('verification').email).toBe('p@example.com');
        expect(TestBed.inject(EmailAuthFlowService).remaining('verification')).toBe(60);
        expect(auth.requestEmailVerification).not.toHaveBeenCalled();
    });
    it('does not submit invalid registration or expose registration conflicts', async () => {
        const auth = TestBed.inject(AuthService);
        spyOn(auth, 'signup').and.rejectWith(new HttpErrorResponse({ status: 400 }));
        await component.onSubmit();
        expect(auth.signup).not.toHaveBeenCalled();
        component.formGroup.setValue({ email: 'p@example.com', password: 'password123', confirmPassword: 'password123' });
        await component.onSubmit();
        expect(component.error).toContain('Check your registration details');
    });

    it('rejects mismatched passwords and sends only email and password', async () => {
        const auth = TestBed.inject(AuthService);
        spyOn(auth, 'signup').and.resolveTo({ email: 'p@example.com' });
        spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
        component.formGroup.setValue({ email: 'p@example.com', password: 'password123', confirmPassword: 'different' });
        await component.onSubmit();
        expect(auth.signup).not.toHaveBeenCalled();
        component.formGroup.controls['confirmPassword'].setValue('password123');
        await component.onSubmit();
        expect(auth.signup).toHaveBeenCalledWith({ email: 'p@example.com', password: 'password123' });
    });
    it('takes Google users directly home or to explicit registration guidance', async () => {
        const signin = spyOn(component.google, 'signin').and.resolveTo();
        const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
        await component.onGoogle({ challengeId: 'id', credential: 'proof' });
        expect(navigate).toHaveBeenCalledWith(['/home']);
        signin.and.rejectWith(new HttpErrorResponse({ status: 422, error: { code: 'google_registration_required' } }));
        await component.onGoogle({ challengeId: 'next', credential: 'proof' });
        expect(component.googleAction).toBe('signup');
        expect(component.error).toContain('traditional registration');
    });

});
