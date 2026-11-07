import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { SESSION_STORAGE } from 'src/app/core/storage/app-storage';
import { EmailAuthPurpose } from 'src/app/types/auth/email-auth';

interface FlowContext { email: string; userName?: string; deadline: number; confirmationDeadline?: number; requested: boolean; }
interface FlowState { verification: FlowContext; recovery: FlowContext; requestDeadline: number; }
export interface EmailAuthError { message: string; fields: Record<string, string>; }

@Injectable({ providedIn: 'root' })
export class EmailAuthFlowService {
    private readonly storage = inject(SESSION_STORAGE);
    private readonly key = 'email-auth-flow';
    private state: FlowState = this.restore();

    context(purpose: EmailAuthPurpose): Readonly<FlowContext> { return this.state[purpose]; }

    setEmail(purpose: EmailAuthPurpose, email: string): void {
        const context = this.state[purpose];
        if (context.email !== email.trim()) {
            context.email = email.trim();
            context.requested = false;
            delete context.userName;
        }
        this.save();
    }

    registered(userName: string, email: string): void {
        this.setEmail('verification', email);
        this.state.verification.userName = userName;
        this.requested('verification');
    }

    prepareVerification(userName: string): void {
        if (this.state.verification.userName !== userName) this.setEmail('verification', '');
    }

    requested(purpose: EmailAuthPurpose): void {
        this.state[purpose].requested = true;
        this.wait(purpose, 60, true);
    }

    remaining(purpose: EmailAuthPurpose, now = Date.now(), request = true): number {
        return Math.max(0, Math.ceil(((request ? Math.max(this.state[purpose].deadline, this.state.requestDeadline) : this.state[purpose].confirmationDeadline || 0) - now) / 1000));
    }

    wait(purpose: EmailAuthPurpose, seconds: number, request: boolean): void {
        const deadline = Date.now() + seconds * 1000;
        if (request) {
            this.state[purpose].deadline = Math.max(this.state[purpose].deadline, deadline);
            this.state.requestDeadline = Math.max(this.state.requestDeadline, deadline);
        } else {
            // Both confirmation endpoints share the server's IP limit.
            for (const item of [this.state.verification, this.state.recovery]) {
                item.confirmationDeadline = Math.max(item.confirmationDeadline || 0, deadline);
            }
        }
        this.save();
    }

    complete(purpose: EmailAuthPurpose): void {
        this.state[purpose] = { email: '', deadline: 0, requested: false };
        this.save();
    }

    acknowledgement(purpose: EmailAuthPurpose): string {
        return purpose === 'verification'
            ? $localize`:@@verificationAcknowledgement:If this account needs verification, a code will be sent. Check your inbox and spam folder.`
            : $localize`:@@recoveryAcknowledgement:If a verified account exists for this email, a recovery code will be sent. Check your inbox and spam folder.`;
    }

    error(error: unknown, purpose: EmailAuthPurpose, confirmation = false): EmailAuthError {
        const fields: Record<string, string> = {};
        if (error instanceof HttpErrorResponse) {
            if (error.status === 429) {
                const header = error.headers.get('Retry-After')?.trim() ?? '';
                const seconds = /^\d+$/.test(header) && Number.isSafeInteger(Number(header)) && Number(header) > 0
                    ? Number(header) : 60;
                this.wait(purpose, seconds, !confirmation);
                return { fields, message: $localize`:@@emailAuthThrottled:Too many requests. Please wait before trying again.` };
            }
            if (error.status === 0) return { fields, message: confirmation
                ? $localize`:@@emailAuthUncertain:We could not confirm the result. Try logging in, or request another code if needed.`
                : $localize`:@@emailAuthNetwork:Connection lost. Check your connection before requesting another code.` };
            if (error.error?.code === 'invalid_verification_code') return { fields,
                message: $localize`:@@emailAuthInvalidCode:This code could not be confirmed. Check your email and code, or request another code.` };
            const errors: unknown = error.error?.errors;
            if (errors && typeof errors === 'object') {
                for (const key of Object.keys(errors)) {
                    switch (key.toLowerCase()) {
                        case 'email': fields['email'] = $localize`:@@emailAuthEmailError:Enter a valid email address.`; break;
                        case 'code': fields['code'] = $localize`:@@emailAuthCodeError:Enter exactly six digits.`; break;
                        case 'newpassword': fields['newPassword'] = $localize`:@@emailAuthPasswordError:Use a password with 4–50 characters.`; break;
                    }
                }
            }
        }
        return { fields, message: Object.keys(fields).length
            ? $localize`:@@emailAuthCheckFields:Check the highlighted fields and try again.`
            : $localize`:@@emailAuthFailure:Something went wrong. Please try again.` };
    }

    private restore(): FlowState {
        const empty = (): FlowContext => ({ email: '', deadline: 0, requested: false });
        const state: FlowState = { verification: empty(), recovery: empty(), requestDeadline: 0 };
        try {
            const data = JSON.parse(this.storage.getItem(this.key) || '{}');
            for (const purpose of ['verification', 'recovery'] as const) {
                const item = data?.[purpose];
                if (item && typeof item.email === 'string' && Number.isFinite(item.deadline) && item.deadline >= 0) {
                    state[purpose] = { email: item.email, deadline: item.deadline, requested: item.requested === true,
                        confirmationDeadline: Number.isFinite(item.confirmationDeadline) ? item.confirmationDeadline : 0,
                        ...(typeof item.userName === 'string' ? { userName: item.userName } : {}) };
                }
            }
            if (Number.isFinite(data?.requestDeadline) && data.requestDeadline >= 0) state.requestDeadline = data.requestDeadline;
        } catch { /* Storage is a convenience; the form still works without it. */ }
        return state;
    }

    private save(): void {
        try { this.storage.setItem(this.key, JSON.stringify(this.state)); } catch { /* Keep in-memory progress. */ }
    }
}
