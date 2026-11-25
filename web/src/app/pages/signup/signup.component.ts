import { GoogleAuthService } from 'src/app/service/auth/google-auth.service';
import { GoogleCredential } from 'src/app/types/auth/google-auth';
import { GoogleSigninButtonComponent } from 'src/app/shared/google-signin-button/google-signin-button.component';
import { ChangeDetectorRef, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from 'src/app/service/auth/auth.service';
import { LoadingComponent } from 'src/app/shared/loading/loading.component';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';
@Component({
    standalone: true,
    imports: [
        GoogleSigninButtonComponent,
        CommonModule,
        ReactiveFormsModule,
        RouterModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        LoadingComponent,
        LanguageSelectorComponent,
        ThemeToggleComponent,
    ],
    selector: 'app-signup',
    templateUrl: './signup.component.html',
    styleUrls: ['./signup.component.scss'],
})
export class SignupComponent {
    private readonly changeDetector = inject(ChangeDetectorRef);
    readonly google = inject(GoogleAuthService);
    googleAction = '';

    private readonly flow = inject(EmailAuthFlowService);
    private readonly route = inject(ActivatedRoute);
    error = '';
    hide = signal(true);
    formGroup: FormGroup;
    loading: boolean = false;

    get sparksIntent(): Record<string, string> | null {
        return this.route.snapshot.queryParamMap.get('sparks') === '1' ? { sparks: '1' } : null;
    }

    constructor(
        private fb: FormBuilder,
        private authService: AuthService,
        private router: Router,
    ) {
        this.formGroup = this.fb.group({
            email: ['', [Validators.required, Validators.email]],
            password: [
                '',
                [
                    Validators.required,
                    Validators.minLength(8),
                    Validators.maxLength(50),
                ],
            ],
            confirmPassword: ['', [Validators.required]],
        }, { validators: group => group.get('password')?.value === group.get('confirmPassword')?.value ? null : { passwordMismatch: true } });
    }

    async onGoogle(credential: GoogleCredential): Promise<void> {
        if (this.loading) return;
        this.loading = true;
        this.googleAction = '';
        try {
            await this.google.signin(credential);
            await this.continueTo('/home');
        } catch (error) {
            this.error = this.google.message(error);
            const code = error instanceof HttpErrorResponse ? error.error?.code : '';
            this.googleAction = code === 'google_registration_required' ? 'signup' : code === 'google_link_required' ? 'login' : '';
        } finally {
            this.loading = false;
            this.changeDetector.markForCheck();
        }
    }

    async onSubmit() {
        if (this.loading || this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }
        this.loading = true;
        this.error = '';
        try {
            const result = await this.authService.signup({ email: this.formGroup.value.email.trim(), password: this.formGroup.value.password });
            this.flow.registered(result.email);
            await this.continueTo('/email-verification');
        } catch (error) {
            this.error = error instanceof HttpErrorResponse && error.status === 400
                ? $localize`:@@registrationFailure:Could not create your account. Check your registration details and try again.`
                : this.flow.error(error, 'verification').message;
        } finally {
            this.loading = false;
            this.changeDetector.markForCheck();
        }
    }

    private continueTo(path: string): Promise<boolean> {
        const queryParams = this.sparksIntent;
        return queryParams ? this.router.navigate([path], { queryParams }) : this.router.navigate([path]);
    }
}
