import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { HttpErrorResponse } from '@angular/common/http';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { AuthService } from 'src/app/service/auth/auth.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { UserLogin } from 'src/app/types/auth/user-login';
import { LoadingComponent } from 'src/app/shared/loading/loading.component';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';
@Component({
    standalone: true,
    imports: [
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
    private readonly flow = inject(EmailAuthFlowService);
    readonly error = signal('');
    private loginDeadline = 0;
    hide = signal(true);
    userLogin: UserLogin = {
        userName: '',
        password: '',
    };

    readonly loading = signal(false);

    constructor(
        private authService: AuthService,
        private router: Router,
        private snackBar: SnackbarService,
    ) {}

    async onSubmit() {
        if (this.loading() || !this.userLogin.userName || !this.userLogin.password || this.loginDeadline > Date.now()) return;
        this.loading.set(true);
        this.error.set('');
        try {
            await this.authService.login(this.userLogin);
            await this.router.navigate(['/home']);
        } catch (error) {
            if (error instanceof HttpErrorResponse && error.status === 403 && error.error?.code === 'email_verification_required') {
                this.flow.prepareVerification(this.userLogin.userName);
                this.userLogin.password = '';
                await this.router.navigate(['/email-verification']);
            } else if (error instanceof HttpErrorResponse && error.status === 401) {
                this.error.set($localize`:@@loginInvalidCredentials:Invalid username or password.`);
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
}
