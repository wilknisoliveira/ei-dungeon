import { Component, DestroyRef, NgZone, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';

@Component({
    standalone: true,
    selector: 'app-email-verification',
    imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule,
        LanguageSelectorComponent, ThemeToggleComponent],
    templateUrl: './email-verification.component.html',
    styleUrls: ['./email-verification.component.scss'],
})
export class EmailVerificationComponent {
    private readonly auth = inject(AuthService);
    readonly flow = inject(EmailAuthFlowService);
    private readonly router = inject(Router);
    private readonly snackbar = inject(SnackbarService);
    private readonly fb = inject(NonNullableFormBuilder);
    readonly emailError = $localize`:@@emailAuthEmailError:Enter a valid email address.`;
    readonly codeError = $localize`:@@emailAuthCodeError:Enter exactly six digits.`;
    readonly busy = signal(false);
    readonly completed = signal(false);
    readonly error = signal('');
    readonly status = signal(this.flow.context('verification').requested ? this.flow.acknowledgement('verification') : '');
    readonly now = signal(Date.now());
    readonly resendWait = computed(() => this.flow.remaining('verification', this.now()));
    readonly confirmWait = computed(() => this.flow.remaining('verification', this.now(), false));
    readonly form = this.fb.group({
        email: [this.flow.context('verification').email, [Validators.required, Validators.email]],
        code: ['', [Validators.required, Validators.pattern(/^[0-9]{6}$/)]],
    });

    constructor() {
        const destroy = inject(DestroyRef);
        const timer = inject(NgZone).runOutsideAngular(() => setInterval(() => this.now.set(Date.now()), 1000));
        destroy.onDestroy(() => clearInterval(timer));
        this.form.controls.email.valueChanges.pipe(takeUntilDestroyed()).subscribe(email => {
            if (email.trim() !== this.flow.context('verification').email) this.form.controls.code.reset();
            this.flow.setEmail('verification', email);
            this.status.set('');
            this.error.set('');
        });
    }

    trimEmail(): void {
        const email = this.form.controls.email;
        if (email.value !== email.value.trim()) email.setValue(email.value.trim());
    }

    async handleRequest(): Promise<void> {
        this.trimEmail();
        const email = this.form.controls.email;
        email.markAsTouched();
        if (email.invalid || this.busy() || this.completed() || this.flow.remaining('verification')) return;
        this.busy.set(true);
        this.error.set('');
        this.status.set('');
        try {
            await this.auth.requestEmailVerification({ email: email.value });
            this.flow.setEmail('verification', email.value);
            this.flow.requested('verification');
            this.status.set(this.flow.acknowledgement('verification'));
        } catch (error) {
            this.handleError(error, false);
        } finally {
            this.now.update(previous => Math.max(Date.now(), previous + 1));
            this.busy.set(false);
        }
    }

    async handleSubmit(): Promise<void> {
        this.trimEmail();
        this.form.markAllAsTouched();
        if (this.form.invalid || this.busy() || this.completed() || this.flow.remaining('verification', Date.now(), false)) return;
        this.busy.set(true);
        this.error.set('');
        try {
            await this.auth.confirmEmailVerification(this.form.getRawValue());
            this.completed.set(true);
            this.form.controls.code.reset();
            this.flow.complete('verification');
            this.snackbar.addSuccess($localize`:@@emailVerifiedSuccess:Email verified. You can now log in.`);
            await this.router.navigate(['/login']);
        } catch (error) {
            this.handleError(error, true);
        } finally {
            this.now.update(previous => Math.max(Date.now(), previous + 1));
            this.busy.set(false);
        }
    }

    private handleError(error: unknown, confirmation: boolean): void {
        const result = this.flow.error(error, 'verification', confirmation);
        this.error.set(result.message);
        for (const key of ['email', 'code'] as const) {
            if (result.fields[key]) this.form.controls[key].setErrors({ server: result.fields[key] });
        }
    }
}
