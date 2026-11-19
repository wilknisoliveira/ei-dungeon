import { AuthService } from 'src/app/service/auth/auth.service';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { SettingsComponent } from './settings.component';

describe('SettingsComponent', () => {
    let component: SettingsComponent;
    let fixture: ComponentFixture<SettingsComponent>;

    beforeEach(async () => {
        TestBed.configureTestingModule({
            imports: [
                SettingsComponent,
                RouterTestingModule,
                HttpClientTestingModule,
                ReactiveFormsModule,
                FormsModule,
                MatFormFieldModule,
                MatInputModule,
                MatSnackBarModule,
                MatIconModule,
                MatDividerModule,
                NoopAnimationsModule,
            ],
        });
        spyOn(TestBed.inject(AuthService), 'getAccount').and.resolveTo({
            id: 'user', email: 'player@example.com', role: 'CommonUser', hasPassword: true, googleLinked: false,
        });
        fixture = TestBed.createComponent(SettingsComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        await fixture.whenStable();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('shows first-password creation for Google-only accounts and requires a proof', async () => {
        component.userInfo = { id: 'user', email: 'player@example.com', role: 'CommonUser', hasPassword: false, googleLinked: true };
        component.activeSection = 'security';
        fixture.detectChanges();
        expect(fixture.nativeElement.textContent).toContain('Create password');
        expect(fixture.nativeElement.querySelector('[formControlName="currentPassword"]')).toBeNull();
        const create = spyOn(component.google, 'createPassword').and.resolveTo();
        component.createPasswordForm.setValue({ password: 'password123', confirmPassword: 'password123' });
        await component.onCreatePassword();
        expect(create).not.toHaveBeenCalled();
        component.passwordProof = { challengeId: 'id', credential: 'proof' };
        await component.onCreatePassword();
        expect(create).toHaveBeenCalledWith({ challengeId: 'id', credential: 'proof' }, 'password123');
        expect(component.passwordProof).toBeNull();
        expect(component.createPasswordForm.controls.password.value).toBeNull();
    });
    it('retains current-password changes for password-bearing accounts', async () => {
        const change = spyOn(TestBed.inject(AuthService), 'changePassword').and.resolveTo({});
        component.changePasswordForm.setValue({ currentPassword: 'current', newPassword: 'new-password' });
        await component.onChangePassword();
        expect(change).toHaveBeenCalledWith('current', 'new-password');
    });
    it('links explicitly and refreshes authoritative account capabilities', async () => {
        const proof = { challengeId: 'id', credential: 'proof' };
        const link = spyOn(component.google, 'link').and.resolveTo();
        await component.linkGoogle(proof);
        expect(link).toHaveBeenCalledWith(proof);
        expect(TestBed.inject(AuthService).getAccount).toHaveBeenCalledTimes(2);
    });

});
