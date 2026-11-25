import { GoogleAuthService } from 'src/app/service/auth/google-auth.service';
import { GoogleCredential } from 'src/app/types/auth/google-auth';
import { GoogleSigninButtonComponent } from 'src/app/shared/google-signin-button/google-signin-button.component';
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { HttpErrorResponse } from '@angular/common/http';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { AuthService } from 'src/app/service/auth/auth.service';
import { UserLogin } from 'src/app/types/auth/user-login';
import { LoadingComponent } from 'src/app/shared/loading/loading.component';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';
@Component({
    standalone: true,
    imports: [
        GoogleSigninButtonComponent,
        CommonModule,
        FormsModule,
        RouterModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        LoadingComponent,
        LanguageSelectorComponent,
        ThemeToggleComponent,
    ],
    selector: 'app-auth',
    templateUrl: './auth.component.html',
    styleUrls: ['./auth.component.scss'],
})
export class AuthComponent {
    readonly google = inject(GoogleAuthService);
    googleAction = '';

    private readonly flow = inject(EmailAuthFlowService);
    private readonly route = inject(ActivatedRoute);
    readonly error = signal('');
    private loginDeadline = 0;
    hide = signal(true);
    userLogin: UserLogin = {
        email: '',
        password: '',
    };

    readonly loading = signal(false);

    get sparksIntent(): Record<string, string> | null {
        return this.route.snapshot.queryParamMap.get('sparks') === '1' ? { sparks: '1' } : null;
    }

    constructor(
        private authService: AuthService,
        private router: Router,
    ) {}

    async onGoogle(credential: GoogleCredential): Promise<void> {
        if (this.loading()) return;
        this.loading.set(true);
        this.googleAction = '';
        try {
            await this.google.signin(credential);
            await this.continueTo('/home');
        } catch (error) {
            this.error.set(this.google.message(error));
            const code = error instanceof HttpErrorResponse ? error.error?.code : '';
            this.googleAction = code === 'google_registration_required' ? 'signup' : code === 'google_link_required' ? 'login' : '';
        } finally {
            this.loading.set(false);
        }
    }

    async onSubmit() {
        this.userLogin.email = this.userLogin.email.trim();
        if (this.loading() || !this.userLogin.email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.userLogin.email) || !this.userLogin.password || this.loginDeadline > Date.now()) return;
        this.loading.set(true);
        this.error.set('');
        try {
            await this.authService.login(this.userLogin);
            await this.continueTo('/home');
        } catch (error) {
            if (error instanceof HttpErrorResponse && error.status === 403 && error.error?.code === 'email_verification_required') {
                this.flow.prepareVerification(this.userLogin.email);
                this.userLogin.password = '';
                await this.continueTo('/email-verification');
            } else if (error instanceof HttpErrorResponse && error.status === 401) {
                this.error.set($localize`:@@loginInvalidCredentials:Invalid email or password.`);
            } else if (error instanceof HttpErrorResponse && error.status === 429) {
                const header = error.headers.get('Retry-After') || '';
                this.loginDeadline = Date.now() + (/^\d+$/.test(header) && Number(header) > 0 && Number.isSafeInteger(Number(header)) ? Number(header) : 60) * 1000;
                this.error.set($localize`:@@emailAuthThrottled:Too many requests. Please wait before trying again.`);
            } else {
                this.error.set(this.flow.error(error, 'verification').message);
            }
        } finally {
            this.loading.set(false);
        }
    }

    private continueTo(path: string): Promise<boolean> {
        const queryParams = this.sparksIntent;
        return queryParams ? this.router.navigate([path], { queryParams }) : this.router.navigate([path]);
    }
}
