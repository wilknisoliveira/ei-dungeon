import { afterNextRender, afterRenderEffect, Component, DestroyRef, ElementRef, NgZone, ViewChild, inject, input, output, signal } from '@angular/core';
import { GoogleAuthService, GoogleButtonView } from 'src/app/service/auth/google-auth.service';
import { ThemeService } from 'src/app/service/theme/theme.service';
import { GoogleAuthPurpose, GoogleCredential } from 'src/app/types/auth/google-auth';

@Component({
    selector: 'app-google-signin-button',
    standalone: true,
    templateUrl: './google-signin-button.component.html',
    styleUrls: ['./google-signin-button.component.scss'],
})
export class GoogleSigninButtonComponent {
    readonly purpose = input<GoogleAuthPurpose>('SignIn');
    readonly disabled = input(false);
    readonly showSeparator = input(false);
    readonly credential = output<GoogleCredential>();
    readonly google = inject(GoogleAuthService);
    readonly error = signal('');
    @ViewChild('buttonHost') private buttonHost?: ElementRef<HTMLElement>;
    private readonly theme = inject(ThemeService);
    private readonly zone = inject(NgZone);
    private alive = true;
    private generation = 0;
    private refreshTimer?: ReturnType<typeof setTimeout>;
    private readonly buttonView = signal<GoogleButtonView | null>(null);

    constructor() {
        afterNextRender(() => { if (this.google.configured) void this.render(); });
        afterRenderEffect(() => {
            const dark = this.theme.isDarkMode();
            this.buttonView()?.updateTheme(dark);
        });
        inject(DestroyRef).onDestroy(() => { this.alive = false; clearTimeout(this.refreshTimer); });
    }

    async render(): Promise<void> {
        if (!this.buttonHost) return;
        clearTimeout(this.refreshTimer);
        this.buttonView.set(null);
        this.error.set('');
        const generation = ++this.generation;
        const active = () => this.alive && generation === this.generation;
        try {
            const view = await this.google.render(this.buttonHost.nativeElement, this.purpose(), this.theme.isDarkMode(),
                credential => this.zone.run(() => {
                    if (!this.disabled()) this.credential.emit(credential);
                }), active);
            if (active()) {
                this.buttonView.set(view);
                this.refreshTimer = this.zone.runOutsideAngular(() => setTimeout(() => { void this.render(); }, 4 * 60 * 1000));
            }
        } catch (error) {
            if (active()) this.error.set(this.google.message(error));
        }
    }
}

