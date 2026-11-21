import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { finalize } from 'rxjs';
import { AdminService } from 'src/app/service/admin/admin.service';
import { SnackbarService } from 'src/app/service/snackbar/snackbar.service';
import { AdminUser } from 'src/app/types/admin/admin';

export interface AdminDeleteDialogData {
    user: AdminUser;
}

@Component({
    selector: 'app-admin-delete-dialog',
    host: { lang: 'en' },
    standalone: true,
    imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
    templateUrl: './admin-delete-dialog.component.html',
    styleUrls: ['./admin-delete-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDeleteDialogComponent {
    readonly data = inject<AdminDeleteDialogData>(MAT_DIALOG_DATA);
    private readonly dialogRef = inject(MatDialogRef<AdminDeleteDialogComponent, boolean>);
    private readonly service = inject(AdminService);
    private readonly snackbar = inject(SnackbarService);
    private readonly destroyRef = inject(DestroyRef);
    readonly confirmation = new FormControl('', { nonNullable: true });
    readonly pending = signal(false);
    readonly error = signal('');

    canDelete(): boolean {
        return !this.pending() && this.confirmation.value.trim() === this.data.user.email.trim();
    }

    deleteAccount(): void {
        if (!this.canDelete()) return;
        this.pending.set(true);
        this.error.set('');
        this.dialogRef.disableClose = true;
        this.service.deleteUser(this.data.user.id).pipe(
            takeUntilDestroyed(this.destroyRef),
            finalize(() => {
                this.pending.set(false);
                this.dialogRef.disableClose = false;
            }),
        ).subscribe({
            next: () => this.dialogRef.close(true),
            error: () => {
                const message = 'Could not delete the account. Please try again.';
                this.error.set(message);
                this.snackbar.addError(message);
            },
        });
    }

    cancel(): void {
        if (!this.pending()) this.dialogRef.close(false);
    }
}
