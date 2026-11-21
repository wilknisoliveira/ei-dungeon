import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../guards/auth.guard';
import { HasRoleGuard } from '../guards/has-role.guard';

export const routes: Routes = [
    {
        path: 'login',
        loadComponent: () => import('./auth/auth.component').then(module => module.AuthComponent),
        canActivate: [AuthGuard],
    },
    {
        path: 'forgot-password',
        loadComponent: () => import('./forgot-password/forgot-password.component').then(module => module.ForgotPasswordComponent),
    },
    {
        path: 'reset-password',
        loadComponent: () => import('./reset-password/reset-password.component').then(module => module.ResetPasswordComponent),
    },
    {
        path: 'email-verification',
        loadComponent: () => import('./email-verification/email-verification.component').then(module => module.EmailVerificationComponent),
    },
    {
        path: 'signup',
        loadComponent: () => import('./signup/signup.component').then(module => module.SignupComponent),
    },
    {
        path: 'settings',
        loadComponent: () => import('./settings/settings.component').then(module => module.SettingsComponent),
        canActivate: [AuthGuard, HasRoleGuard],
        data: {
            roles: ['Admin', 'CommonUser'],
        },
    },
    {
        path: 'home',
        loadComponent: () => import('./home/home.component').then(module => module.HomeComponent),
        canActivate: [AuthGuard, HasRoleGuard],
        data: {
            roles: ['Admin', 'CommonUser'],
        },
    },
    {
        path: 'admin',
        loadComponent: () => import('./admin/admin.component').then(module => module.AdminComponent),
        canActivate: [AuthGuard, HasRoleGuard],
        data: {
            roles: ['Admin'],
        },
    },
    {
        path: '',
        loadComponent: () => import('./landing/landing.component').then(module => module.LandingComponent),
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class RoutingModule {}
