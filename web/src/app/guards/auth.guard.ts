import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../service/auth/auth.service';

export const AuthGuard: CanActivateFn = (route, state) => {
    const router: Router = inject(Router);
    const authService: AuthService = inject(AuthService);

    if (authService.isUserLoggedIn() && route.url.toString() == 'login') {
        router.navigate(['home']);
        return false;
    } else if (
        !authService.isUserLoggedIn() &&
        route.url.toString() == 'login'
    ) {
        return true;
    } else if (authService.isUserLoggedIn()) {
        return true;
    } else {
        router.navigate(['login']);
        return false;
    }
};
