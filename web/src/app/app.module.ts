import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './http-interceptors/auth-interceptor';
import { languageInterceptor } from './http-interceptors/language-interceptor';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import {
    provideClientHydration,
    withI18nSupport,
} from '@angular/platform-browser';

@NgModule({
    imports: [
        BrowserModule,
        AppRoutingModule,
        BrowserAnimationsModule,
        MatSnackBarModule,
        AppComponent,
    ],
    providers: [
        provideHttpClient(withInterceptors([languageInterceptor, authInterceptor])),
        provideClientHydration(withI18nSupport()),
    ],
    bootstrap: [AppComponent],
})
export class AppModule {}
