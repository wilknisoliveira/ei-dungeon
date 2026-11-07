import { Component, DestroyRef, NgZone, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from 'src/app/service/auth/auth.service';
import { EmailAuthFlowService } from 'src/app/service/auth/email-auth-flow.service';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';

@Component({
    standalone: true,
    selector: 'app-forgot-password',
    imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule,
        LanguageSelectorComponent, ThemeToggleComponent],
    templateUrl: './forgot-password.component.html',
    styleUrls: ['./forgot-password.component.scss'],
})
export class ForgotPasswordComponent {
    private readonly auth = inject(AuthService);
    readonly flow = inject(EmailAuthFlowService);
    private readonly router = inject(Router);
    readonly emailError = $localize`:@@emailAuthEmailError:Enter a valid email address.`;
    readonly busy = signal(false);
    readonly completed = signal(false);
    readonly error = signal('');
    readonly now = signal(Date.now());
    readonly resendWait = computed(() => this.flow.remaining('recovery', this.now()));
    readonly form = inject(NonNullableFormBuilder).group({
        email: [this.flow.context('recovery').email, [Validators.required, Validators.email]],
    });

    constructor() {
        const destroy = inject(DestroyRef);
        const timer = inject(NgZone).runOutsideAngular(() => setInterval(() => this.now.set(Date.now()), 1000));
        destroy.onDestroy(() => clearInterval(timer));
        this.form.controls.email.valueChanges.pipe(takeUntilDestroyed()).subscribe(email => {
            this.flow.setEmail('recovery', email);
            this.error.set('');
        });
    }

    trimEmail(): void {
        const email = this.form.controls.email;
        if (email.value !== email.value.trim()) email.setValue(email.value.trim());
    }

    async handleSubmit(): Promise<void> {
        this.trimEmail();
        this.form.markAllAsTouched();
        if (this.form.invalid || this.busy() || this.completed() || this.flow.remaining('recovery')) return;
        this.busy.set(true);
        this.error.set('');
        try {
            await this.auth.requestPasswordRecovery(this.form.getRawValue());
            this.flow.setEmail('recovery', this.form.controls.email.value);
            this.flow.requested('recovery');
            this.completed.set(true);
            await this.router.navigate(['/reset-password']);
        } catch (error) {
            const result = this.flow.error(error, 'recovery');
            this.error.set(result.message);
            if (result.fields['email']) this.form.controls.email.setErrors({ server: result.fields['email'] });
        } finally {
            this.now.update(previous => Math.max(Date.now(), previous + 1));
            this.busy.set(false);
        }
    }
}
