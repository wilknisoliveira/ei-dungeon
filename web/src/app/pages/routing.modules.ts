import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';
import { EmailVerificationComponent } from './email-verification/email-verification.component';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthComponent } from './auth/auth.component';
import { AuthGuard } from '../guards/auth.guard';
import { HomeComponent } from './home/home.component';
import { HasRoleGuard } from '../guards/has-role.guard';
import { SignupComponent } from './signup/signup.component';
import { SettingsComponent } from './settings/settings.component';
import { LandingComponent } from './landing/landing.component';

export const routes: Routes = [
    { path: 'login', component: AuthComponent, canActivate: [AuthGuard] },
    { path: 'forgot-password', component: ForgotPasswordComponent },
    { path: 'reset-password', component: ResetPasswordComponent },
    { path: 'email-verification', component: EmailVerificationComponent },
    { path: 'signup', component: SignupComponent },
    { path: 'settings', component: SettingsComponent, canActivate: [AuthGuard, HasRoleGuard],
        data: {
            roles: ['Admin', 'CommonUser'],
        },
    },
    {
        path: 'home',
        component: HomeComponent,
        canActivate: [AuthGuard, HasRoleGuard],
        data: {
            roles: ['Admin', 'CommonUser'],
        },
    },
    {
        path: '',
        component: LandingComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RoutingModule {}
