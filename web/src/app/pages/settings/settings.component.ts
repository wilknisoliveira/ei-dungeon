import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from 'src/app/service/auth/auth.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { ConfirmationDialogComponent } from 'src/app/shared/confirmation-dialog/confirmation-dialog.component';
import { IsHandsetService } from 'src/app/shared/responsive/is-handset.service';
import { LanguageSelectorComponent } from 'src/app/shared/components/language-selector/language-selector.component';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';

@Component({
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        RouterModule,
        MatDialogModule,
        MatSidenavModule,
        MatListModule,
        MatIconModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        LanguageSelectorComponent,
        ThemeToggleComponent,
    ],
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    styleUrls: ['./settings.component.scss'],
})
export class SettingsComponent implements OnInit {
    userInfo: { id: string; username: string; role: string; fullName: string; email: string } | null = null;
    changePasswordForm: FormGroup;
    loading = false;
    activeSection = 'account';

    /** True below 768px: the drawer becomes an overlay, closed by default. */
    isHandset = inject(IsHandsetService).isHandset;

    /** Drives the drawer opening. Open on desktop, toggled on handsets. */
    drawerOpened = signal(true);

    constructor(
        private authService: AuthService,
        private snackBar: SnackbarService,
        private fb: FormBuilder,
        private dialog: MatDialog,
        private router: Router,
    ) {
        effect(() => {
            if (this.isHandset()) {
                this.drawerOpened.set(false);
            } else {
                this.drawerOpened.set(true);
            }
        });

        this.changePasswordForm = this.fb.group({
            currentPassword: [
                '',
                [
                    Validators.required,
                    Validators.minLength(4),
                    Validators.maxLength(50),
                ],
            ],
            newPassword: [
                '',
                [
                    Validators.required,
                    Validators.minLength(4),
                    Validators.maxLength(50),
                ],
            ],
        });
    }

    ngOnInit(): void {
        this.userInfo = this.authService.getUserInfo();
    }

    selectSection(section: string): void {
        this.activeSection = section;
        if (this.isHandset()) {
            this.drawerOpened.set(false);
        }
    }

    async onChangePassword(): Promise<void> {
        if (this.changePasswordForm.invalid) return;
        this.loading = true;
        try {
            const { currentPassword, newPassword } =
                this.changePasswordForm.value;
            await this.authService.changePassword(currentPassword, newPassword);
            this.snackBar.addSuccess('Password changed successfully.');
            this.changePasswordForm.reset();
        } catch (error: any) {
            if (error.status === 400) {
                this.snackBar.addError('Current password is incorrect.');
            } else {
                this.snackBar.addError('Something went wrong.');
            }
        } finally {
            this.loading = false;
        }
    }

    openDeleteAccountDialog(): void {
        const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
            width: '400px',
            data: $localize`Are you sure you want to delete your account? This action is irreversible and all your data will be permanently lost.`,
        });

        dialogRef.afterClosed().subscribe((result: boolean) => {
            if (result) {
                this.deleteAccount();
            }
        });
    }

    async deleteAccount(): Promise<void> {
        if (!this.userInfo?.id) return;
        this.loading = true;
        try {
            await new Promise<void>((resolve, reject) => {
                this.authService.deleteUser(this.userInfo!.id).subscribe({
                    next: () => resolve(),
                    error: (error) => reject(error),
                });
            });
            this.authService.logout();
            this.router.navigate(['signup']);
        } catch {
            this.snackBar.addError('Something went wrong while attempting to delete the account.');
            this.loading = false;
        }
    }
}
