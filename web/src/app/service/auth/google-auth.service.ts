import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpContext, HttpErrorResponse } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { AuthService } from 'src/app/service/auth/auth.service';
import { PUBLIC_AUTH } from 'src/app/types/auth/email-auth';
import { GoogleAuthPurpose, GoogleChallenge, GoogleCredential } from 'src/app/types/auth/google-auth';
import { TokenObject } from 'src/app/types/auth/token-object';
import { environment } from 'src/environments/environment';
import { LocaleService } from 'src/app/core/services/locale.service';

interface GoogleAccounts {
    initialize(options: { client_id: string; nonce: string; auto_select: boolean; callback: (response: { credential: string }) => void }): void;
    renderButton(element: HTMLElement, options: { type: string; theme: string; text: string; size: string; locale: string }): void;
}

export interface GoogleButtonView {
    updateTheme(dark: boolean): void;
}

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
    private readonly http = inject(HttpClient);
    private readonly auth = inject(AuthService);
    private readonly document = inject(DOCUMENT);
    private readonly platform = inject(PLATFORM_ID);
    private readonly locale = inject(LocaleService).getCurrentLanguage();
    private library?: Promise<GoogleAccounts>;
    readonly configured = !!environment.googleClientId;

    async render(element: HTMLElement, purpose: GoogleAuthPurpose, dark: boolean,
        callback: (credential: GoogleCredential) => void, active: () => boolean): Promise<GoogleButtonView> {
        const accounts = await this.load();
        const challenge = await lastValueFrom(this.http.post<GoogleChallenge>(
            environment.api + '/api/user/auth/google/challenge', { purpose }, this.options(purpose === 'SignIn')));
        const stack = this.document.createElement('div');
        stack.style.display = 'grid';
        const variants = [false, true].map(dark => {
            const host = this.document.createElement('div');
            host.style.gridArea = '1 / 1';
            host.style.display = 'grid';
            host.style.justifyItems = 'center';
            stack.appendChild(host);
            return { dark, host };
        });
        let renderedTheme: boolean | undefined;
        const view: GoogleButtonView = {
            updateTheme: dark => {
                if (!active() || renderedTheme === dark) return;
                for (const variant of variants) {
                    const visible = variant.dark === dark;
                    variant.host.style.visibility = visible ? 'visible' : 'hidden';
                    variant.host.inert = !visible;
                    if (visible) variant.host.removeAttribute('aria-hidden');
                    else variant.host.setAttribute('aria-hidden', 'true');
                }
                renderedTheme = dark;
            },
        };
        if (!active()) return view;
        accounts.initialize({
            client_id: environment.googleClientId, nonce: challenge.nonce, auto_select: false,
            callback: response => {
                if (active()) callback({ challengeId: challenge.challengeId, credential: response.credential });
            },
        });
        view.updateTheme(dark);
        element.replaceChildren(stack);
        for (const variant of variants) {
            accounts.renderButton(variant.host, { type: 'standard', theme: variant.dark ? 'outline_dark' : 'outline',
                text: 'continue_with', size: 'large', locale: this.locale });
        }
        return view;
    }

    async signin(credential: GoogleCredential): Promise<void> {
        const tokens = await lastValueFrom(this.http.post<TokenObject>(
            environment.api + '/api/user/auth/google', credential, this.options(true)));
        this.auth.setTokens(tokens);
    }

    async link(credential: GoogleCredential): Promise<void> {
        await lastValueFrom(this.http.post<void>(environment.api + '/api/user/auth/google/link', credential, this.options(false)));
    }

    async createPassword(credential: GoogleCredential, newPassword: string): Promise<void> {
        await lastValueFrom(this.http.post<void>(environment.api + '/api/user/auth/password',
            { ...credential, newPassword }, this.options(false)));
    }

    message(error: unknown): string {
        const code = error instanceof HttpErrorResponse ? error.error?.code : '';
        switch (code) {
            case 'google_registration_required':
                return $localize`:@@googleRegistrationRequired:To use this email address, complete traditional registration and verify your email. You can then link Google in Settings.`;
            case 'google_link_required':
                return $localize`:@@googleLinkRequired:Sign in to your existing account, then link Google in Settings.`;
            case 'google_email_mismatch':
                return $localize`:@@googleEmailMismatch:Select the Google Account with the same email as your EI Dungeon account.`;
            case 'google_link_conflict':
                return $localize`:@@googleLinkConflict:This Google Account could not be linked. Check your connected account and try again.`;
            case 'password_already_exists':
                return $localize`:@@passwordAlreadyExists:A password is already configured. Reload Settings to change it.`;
            default:
                return $localize`:@@googleAuthFailed:Google sign-in could not be completed. Please try again or use email and password.`;
        }
    }

    private options(anonymous: boolean) {
        return { withCredentials: true, context: new HttpContext().set(PUBLIC_AUTH, anonymous) };
    }

    private load(): Promise<GoogleAccounts> {
        if (!this.configured || !isPlatformBrowser(this.platform)) return Promise.reject(new Error('Google unavailable'));
        const window = this.document.defaultView as (Window & { google?: { accounts: { id: GoogleAccounts } } }) | null;
        if (window?.google?.accounts.id) return Promise.resolve(window.google.accounts.id);
        if (!this.library) {
            this.library = new Promise<GoogleAccounts>((resolve, reject) => {
                const script = this.document.createElement('script');
                script.src = `https://accounts.google.com/gsi/client?hl=${encodeURIComponent(this.locale)}`;
                script.async = true;
                const timeout = setTimeout(() => { script.remove(); this.library = undefined; reject(new Error('Google timeout')); }, 15000);
                script.onload = () => {
                    clearTimeout(timeout);
                    const accounts = window?.google?.accounts.id;
                    if (accounts) resolve(accounts);
                    else { this.library = undefined; script.remove(); reject(new Error('Google unavailable')); }
                };
                script.onerror = () => {
                    clearTimeout(timeout); this.library = undefined; script.remove(); reject(new Error('Google unavailable'));
                };
                this.document.head.appendChild(script);
            });
        }
        return this.library;
    }
}

