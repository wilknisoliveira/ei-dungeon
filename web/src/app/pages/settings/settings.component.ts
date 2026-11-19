import { CurrentAccount, GoogleCredential } from 'src/app/types/auth/google-auth';
import { GoogleAuthService } from 'src/app/service/auth/google-auth.service';
import { GoogleSigninButtonComponent } from 'src/app/shared/google-signin-button/google-signin-button.component';
import { ChangeDetectorRef, Component, OnInit, effect, inject, signal } from '@angular/core';
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
        GoogleSigninButtonComponent,
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
    private readonly changeDetector = inject(ChangeDetectorRef);
    userInfo: CurrentAccount | null = null;
    readonly google = inject(GoogleAuthService);
    passwordProof: GoogleCredential | null = null;
    readonly createPasswordForm = inject(FormBuilder).group({
        password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(50)]],
        confirmPassword: ['', [Validators.required]],
    }, { validators: group => group.get('password')?.value === group.get('confirmPassword')?.value ? null : { passwordMismatch: true } });
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
        void this.loadAccount();
    }

    async loadAccount(): Promise<void> {
        try {
            this.userInfo = await this.authService.getAccount();
        } catch {
            this.snackBar.addError($localize`:@@accountLoadFailed:Could not load account settings. Please reload the page.`);
        } finally {
            this.changeDetector.markForCheck();
        }
    }

    async linkGoogle(credential: GoogleCredential): Promise<void> {
        if (this.loading) return;
        this.loading = true;
        try {
            await this.google.link(credential);
            await this.loadAccount();
            this.snackBar.addSuccess($localize`:@@googleLinkedSuccess:Google Account linked successfully.`);
        } catch (error) {
            this.snackBar.addError(this.google.message(error));
        } finally {
            this.loading = false;
            this.changeDetector.markForCheck();
        }
    }

    async onCreatePassword(): Promise<void> {
        this.createPasswordForm.markAllAsTouched();
        if (this.createPasswordForm.invalid || !this.passwordProof || this.loading) return;
        this.loading = true;
        try {
            await this.google.createPassword(this.passwordProof, this.createPasswordForm.controls.password.value!);
            this.snackBar.addSuccess($localize`:@@passwordCreated:Password created. You can now also sign in with email and password.`);
            await this.loadAccount();
        } catch (error) {
            this.snackBar.addError(this.google.message(error));
        } finally {
            this.passwordProof = null;
            this.createPasswordForm.reset();
            this.loading = false;
            this.changeDetector.markForCheck();
        }
    }

    selectSection(section: string): void {
        this.activeSection = section;
        if (this.isHandset()) {
            this.drawerOpened.set(false);
        }
    }

    async onChangePassword(): Promise<void> {
        if (this.changePasswordForm.invalid || !this.userInfo?.hasPassword || this.loading) return;
        this.loading = true;
        try {
            const { currentPassword, newPassword } =
                this.changePasswordForm.value;
            await this.authService.changePassword(currentPassword, newPassword);
            this.snackBar.addSuccess($localize`:@@passwordChangedSuccess:Password changed successfully.`);
            this.changePasswordForm.reset();
        } catch (error: any) {
            if (error.status === 400) {
                this.snackBar.addError($localize`:@@currentPasswordIncorrect:Current password is incorrect.`);
            } else {
                this.snackBar.addError($localize`:@@settingsFailure:Something went wrong. Please try again.`);
            }
        } finally {
            this.loading = false;
            this.changeDetector.markForCheck();
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
            this.changeDetector.markForCheck();
        }
    }
}
