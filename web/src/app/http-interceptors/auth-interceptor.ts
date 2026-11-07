import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from 'src/app/service/auth/auth.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { LOCAL_STORAGE } from 'src/app/core/storage/app-storage';
import { PUBLIC_AUTH } from 'src/app/types/auth/email-auth';
import { TokenObject } from 'src/app/types/auth/token-object';

// Share rejection as well as success so waiting requests always terminate.
const refreshes = new WeakMap<AuthService, Promise<TokenObject>>();

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    if (req.context.get(PUBLIC_AUTH)) {
        return next(req.clone({ headers: req.headers.delete('Authorization') }));
    }
    const auth = inject(AuthService);
    const router = inject(Router);
    const snackbar = inject(SnackbarService);
    const storage = inject(LOCAL_STORAGE);
    const isRefresh = /\/api\/user\/auth\/refresh(?:\?|$)/i.test(req.url);
    const token = auth.getAuthToken();
    const request = token && !isRefresh
        ? req.clone({ setHeaders: { Authorization: 'Bearer ' + token } }) : req;

    return next(request).pipe(catchError((error: HttpErrorResponse) => {
        if (error.status !== 401 || isRefresh) return throwError(() => error);
        let pending = refreshes.get(auth);
        if (!pending) {
            pending = (async () => {
                try {
                    const stored = JSON.parse(storage.getItem('tokenInfo') || '{}');
                    const accessToken = auth.getAuthToken();
                    if (!accessToken || !stored.refreshToken) throw error;
                    return await auth.refreshToken(accessToken, stored.refreshToken);
                } catch (failure) {
                    auth.logout();
                    snackbar.addError($localize`:@@sessionExpired:Session expired. Please log in again.`);
                    void router.navigate(['/login']);
                    throw failure;
                } finally {
                    queueMicrotask(() => refreshes.delete(auth));
                }
            })();
            refreshes.set(auth, pending);
        }
        return from(pending).pipe(switchMap(result => next(request.clone({
            setHeaders: { Authorization: 'Bearer ' + result.accessToken },
        }))));
    }));
};
