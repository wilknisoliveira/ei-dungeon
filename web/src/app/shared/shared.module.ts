import { NgModule } from '@angular/core';
import { LoadingComponent } from './loading/loading.component';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';

import { SmallLoadingComponent } from './small-loading/small-loading.component';
import { ConfirmationDialogComponent } from './confirmation-dialog/confirmation-dialog.component';
import { ThemeToggleComponent } from './theme-toggle/theme-toggle.component';
import { LanguageSelectorComponent } from './components/language-selector/language-selector.component';
import { TimezoneAwareDatePipe } from './pipes/timezone-aware-date.pipe';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@NgModule({
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        FormsModule,
        ReactiveFormsModule,
        MatProgressSpinnerModule,
        MatSelectModule,
        MatInputModule,
        LoadingComponent,
        SmallLoadingComponent,
        ConfirmationDialogComponent,
        ThemeToggleComponent,
                LanguageSelectorComponent,
        TimezoneAwareDatePipe,
    ],
    exports: [
        LoadingComponent,
        SmallLoadingComponent,
        ConfirmationDialogComponent,
        ThemeToggleComponent,
                LanguageSelectorComponent,
        TimezoneAwareDatePipe,
    ],
})
export class SharedModule {}
