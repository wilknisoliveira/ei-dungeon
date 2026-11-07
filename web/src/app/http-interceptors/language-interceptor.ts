import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LocaleService } from 'src/app/core/services/locale.service';
import { environment } from 'src/environments/environment';

const apiRoot = `${environment.api.replace(/\/+$/, '')}/api`;

/** Sends the active application's language only to the configured API. */
export const languageInterceptor: HttpInterceptorFn = (request, next) => {
    if (request.url !== apiRoot && !request.url.startsWith(`${apiRoot}/`) && !request.url.startsWith(`${apiRoot}?`)) {
        return next(request);
    }

    // The selector and this interceptor use the same source. Language changes reload
    // the localized application; browser settings or stale storage must not override it.
    const language = inject(LocaleService).getCurrentLanguage();
    return next(request.clone({ setHeaders: { 'Accept-Language': language } }));
};
