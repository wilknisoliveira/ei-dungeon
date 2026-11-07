import { HttpContextToken } from '@angular/common/http';

export const PUBLIC_AUTH = new HttpContextToken<boolean>(() => false);

export type EmailAuthPurpose = 'verification' | 'recovery';
export interface EmailRequest { email: string; }
export interface EmailConfirmation extends EmailRequest { code: string; }
export interface RecoveryConfirmation extends EmailConfirmation { newPassword: string; }
export interface RegistrationResponse { userName: string; fullName: string; email: string; }
