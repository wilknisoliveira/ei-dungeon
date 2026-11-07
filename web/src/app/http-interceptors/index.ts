import { HTTP_INTERCEPTORS, HttpInterceptorFn } from '@angular/common/http';
import { authInterceptor } from './auth-interceptor';

export const httpInterceptorProviders = [
    { provide: HTTP_INTERCEPTORS, useFactory: authInterceptor satisfies HttpInterceptorFn, multi: true },
];
